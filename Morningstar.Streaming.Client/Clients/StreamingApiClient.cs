using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services.AvroBinaryDeserializer;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Morningstar.Streaming.Client.Clients
{
    public class StreamingApiClient : IStreamingApiClient
    {
        private const int FlushIntervalMillis = 60_000;
        private static readonly TimeSpan heartbeatAcknowledgementTimeout = TimeSpan.FromSeconds(5);
        private readonly IApiHelper apiHelper;
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<StreamingApiClient> logger;
        private readonly IAvroBinaryDeserializer avroBinaryDeserializer;
        private readonly TimeSpan heartbeatTimeout = TimeSpan.FromMinutes(1);
        private readonly TimeSpan heartbeatCheckInterval = TimeSpan.FromSeconds(5);
        private const string ExpectedDisconnectType = "Expected";
        private const string UnexpectedDisconnectType = "Unexpected";

        private enum DisconnectKind
        {
            Unexpected,
            Expected
        }

        private readonly record struct IncomingMessage(WebSocketMessageType MessageType, byte[] Payload, long ReceivedAtMillis);

        private readonly record struct TelemetryItem(WebSocketMessageType MessageType, string jsonMessage, long ReceivedAtMillis);

        private readonly record struct ReceiveLoopResult(bool ShouldReconnect, DisconnectKind Kind);

        public StreamingApiClient(
            IApiHelper apiHelper,
            ILogger<StreamingApiClient> logger,
            ITokenProvider tokenProvider,
            IAvroBinaryDeserializer avroBinaryDeserializer)
        {
            this.apiHelper = apiHelper;
            this.tokenProvider = tokenProvider;
            this.logger = logger;
            this.avroBinaryDeserializer = avroBinaryDeserializer;
        }

        /// <summary>
        /// Generic method to create a Level 1 stream with any request type and endpoint.
        /// Consolidates the common logic for creating streams regardless of request type or endpoint.
        /// </summary>
        public async Task<StreamResponse> CreateL1StreamAsync<TRequest>(TRequest streamRequest, string endpointUrl) where TRequest : class
        {
            try
            {
                var headers = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Authorization", await tokenProvider.CreateBearerTokenAsync()),
                    new KeyValuePair<string, string>("Accept", "application/json")
                };

                return await apiHelper.ProcessRequestAsync<StreamResponse>(
                    endpointUrl,
                    HttpMethod.Post,
                    headers,
                    streamRequest
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error when attempting to request L1 Stream.");
                throw;
            }
        }

        /// <summary>
        /// Generic method to create a Level 2 stream with any request type and endpoint.
        /// Consolidates the common logic for creating streams regardless of request type or endpoint.
        /// </summary>
        public async Task<StreamResponse> CreateL2StreamAsync<TRequest>(TRequest streamRequest, string endpointUrl) where TRequest : class
        {
            try
            {
                var headers = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Authorization", await tokenProvider.CreateBearerTokenAsync()),
                    new KeyValuePair<string, string>("Accept", "application/json")
                };

                return await apiHelper.ProcessRequestAsync<StreamResponse>(
                    endpointUrl,
                    HttpMethod.Post,
                    headers,
                    streamRequest
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error when attempting to request L2 Stream.");
                throw;
            }
        }

        /// <summary>
        /// Subscribes to a WebSocket stream and signals when the connection is established.
        /// </summary>
        /// <param name="subscriptionId">Unique identifier for the subscription, used for logging and telemetry</param>
        /// <param name="webSocketUrl">The WebSocket URL to connect to</param>
        /// <param name="purpose">Optional purpose or description for the connection</param>
        /// <param name="onMessageAsync">Callback function to process incoming messages</param>
        /// <param name="connected">TaskCompletionSource that completes when connected</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public Task SubscribeAsync(
            Guid subscriptionId,
            string webSocketUrl,
            string? purpose,
            Func<string, Task> onMessageAsync,
            TaskCompletionSource<bool> connected,
            CancellationToken cancellationToken = default)
        {
            return SubscribeAsync(subscriptionId, webSocketUrl, purpose, onMessageAsync, connected, cancellationToken, null, null, null);
        }

        public async Task SubscribeAsync(
            Guid subscriptionId,
            string webSocketUrl,
            string? purpose,
            Func<string, Task> onMessageAsync,
            TaskCompletionSource<bool> connected,
            CancellationToken cancellationToken,
            ICounterLogger? counterLogger,
            ILatencyLogger? latencyLogger,
            ISequenceLogger? sequenceLogger)
        {
            await ConnectWithRetryAsync(
                subscriptionId,
                webSocketUrl,
                purpose,
                onMessageAsync,
                connected,
                counterLogger,
                latencyLogger,
                sequenceLogger,
                sequenceDetector: null,
                onDisconnectNoticeReceived: null,
                gracefulCloseToken: default,
                onDisconnected: null,
                onReconnected: null,
                cancellationToken);
        }

        public async Task SubscribeAsync(
            Guid subscriptionId,
            string webSocketUrl,
            string? purpose,
            Func<string, Task> onMessageAsync,
            TaskCompletionSource<bool> connected,
            CancellationToken cancellationToken,
            ICounterLogger? counterLogger,
            ILatencyLogger? latencyLogger,
            ISequenceLogger? sequenceLogger,
            SequenceGapDetector? sequenceDetector,
            Action<int?> onDisconnectNoticeReceived,
            CancellationToken gracefulCloseToken,
            Action<string> onDisconnected,
            Action<string> onReconnected)
        {
            await ConnectWithRetryAsync(
                subscriptionId,
                webSocketUrl,
                purpose,
                onMessageAsync,
                connected,
                counterLogger,
                latencyLogger,
                sequenceLogger,
                sequenceDetector,
                onDisconnectNoticeReceived,
                gracefulCloseToken,
                onDisconnected,
                onReconnected,
                cancellationToken);
        }

        private async Task ConnectWithRetryAsync(
            Guid subscriptionId,
            string webSocketUrl,
            string? purpose,
            Func<string, Task> onMessageAsync,
            TaskCompletionSource<bool> connected,
            ICounterLogger? counterLogger,
            ILatencyLogger? latencyLogger,
            ISequenceLogger? sequenceLogger,
            SequenceGapDetector? sequenceDetector,
            Action<int?>? onDisconnectNoticeReceived,
            CancellationToken gracefulCloseToken,
            Action<string>? onDisconnected,
            Action<string>? onReconnected,
            CancellationToken cancellationToken)
        {
            const int maxAttempts = 5;
            int attempt = 0;
            DisconnectKind? reconnectMetricKind = null;

            // Linked so a graceful retirement requested mid-backoff/mid-connect is honored immediately,
            // not only once the active receive loop's own linked token picks it up.
            using var connectCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, gracefulCloseToken);
            var connectCancellationToken = connectCancellationSource.Token;

            while (!cancellationToken.IsCancellationRequested && !gracefulCloseToken.IsCancellationRequested)
            {
                attempt++;

                try
                {
                    using var ws = await ConnectWebSocketAsync(webSocketUrl, purpose, connectCancellationToken);

                    logger.LogInformation("WebSocket connected on attempt {Attempt}.", attempt);

                    if (reconnectMetricKind.HasValue)
                    {
                        onReconnected?.Invoke(ToDisconnectType(reconnectMetricKind.Value));
                        reconnectMetricKind = null;
                    }

                    // Signal connection established
                    connected.TrySetResult(true);

                    // Reset attempt counter after successful connection
                    attempt = 0;

                    var receiveLoopResult = await StartReceiveLoopAsync(subscriptionId, ws, onMessageAsync, cancellationToken, counterLogger, latencyLogger, sequenceLogger, sequenceDetector, onDisconnectNoticeReceived, gracefulCloseToken);

                    if (!ShouldReconnect(receiveLoopResult, cancellationToken))
                    {
                        if (!cancellationToken.IsCancellationRequested && gracefulCloseToken.IsCancellationRequested)
                        {
                            // Retired for an arbitration handover, not a subscription stop - still a real
                            // disconnect worth recording, it just won't be reconnected here.
                            onDisconnected?.Invoke(ToDisconnectType(receiveLoopResult.Kind));
                        }

                        return;
                    }

                    reconnectMetricKind = receiveLoopResult.Kind;
                    onDisconnected?.Invoke(ToDisconnectType(reconnectMetricKind.Value));

                    // Connection ended gracefully - reset counter and retry
                    logger.LogInformation("WebSocket disconnected. Attempting to reconnect...");
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    HandleCancellation(connected, ex, "Cancellation requested. Stopping WebSocket connection.");
                    return;
                }
                catch (Exception ex)
                {
                    if (!await HandleConnectionFailureAsync(
                        ex,
                        attempt,
                        maxAttempts,
                        connected,
                        connectCancellationToken))
                    {
                        return;
                    }
                }
            }
        }

        private static bool ShouldReconnect(ReceiveLoopResult receiveLoopResult, CancellationToken cancellationToken)
        {
            return receiveLoopResult.ShouldReconnect && !cancellationToken.IsCancellationRequested;
        }

        private async Task<bool> HandleConnectionFailureAsync(
            Exception exception,
            int attempt,
            int maxAttempts,
            TaskCompletionSource<bool> connected,
            CancellationToken cancellationToken)
        {
            logger.LogWarning(exception, "WebSocket failed (attempt {Attempt} of {MaxAttempts}). Reconnecting...", attempt, maxAttempts);

            if (attempt >= maxAttempts)
            {
                logger.LogError("Maximum retry attempts ({MaxAttempts}) reached. Stopping WebSocket connection.", maxAttempts);
                if (!connected.Task.IsCompleted)
                {
                    connected.TrySetException(exception);
                }

                throw exception;
            }

            return await DelayReconnectAsync(attempt, connected, cancellationToken);
        }

        private async Task<bool> DelayReconnectAsync(
            int attempt,
            TaskCompletionSource<bool> connected,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                HandleCancellation(connected, null, "Cancellation requested during reconnect delay. Stopping WebSocket connection.");
                return false;
            }

            var delaySeconds = Math.Min(Math.Pow(2, attempt), 30);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                return true;
            }
            catch (OperationCanceledException ex)
            {
                HandleCancellation(connected, ex, "Cancellation requested during reconnect delay. Stopping WebSocket connection.");
                return false;
            }
        }

        private void HandleCancellation(
            TaskCompletionSource<bool> connected,
            Exception? exception,
            string message)
        {
            logger.LogInformation(exception, message);
            if (!connected.Task.IsCompleted)
            {
                connected.TrySetCanceled();
            }
        }

        private async Task<ClientWebSocket> ConnectWebSocketAsync(string url, string? purpose, CancellationToken cancellationToken)
        {
            var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("Authorization", await tokenProvider.CreateBearerTokenAsync());
            if (!string.IsNullOrEmpty(purpose))
            {
                ws.Options.SetRequestHeader("Purpose", purpose);
            }

            logger.LogInformation("Connecting WebSocket to {Url} with purpose {Purpose}", url, purpose);
            await ws.ConnectAsync(new Uri(url), cancellationToken);

            return ws;
        }

        private async Task<ReceiveLoopResult> StartReceiveLoopAsync(
            Guid subscriptionId,
            ClientWebSocket ws,
            Func<string, Task> onMessageAsync,
            CancellationToken cancellationToken,
            ICounterLogger? counterLogger,
            ILatencyLogger? latencyLogger,
            ISequenceLogger? sequenceLogger,
            SequenceGapDetector? sequenceDetector,
            Action<int?>? onDisconnectNoticeReceived,
            CancellationToken gracefulCloseToken)
        {
            var buffer = new byte[4096];
            var lastHeartbeat = DateTime.UtcNow;
            var pendingDisconnectKind = DisconnectKind.Unexpected;
            using var shutdownCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, gracefulCloseToken);
            var shutdownCancellationToken = shutdownCancellationTokenSource.Token;

            var messageChannel = Channel.CreateUnbounded<IncomingMessage>(new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = true
            });

            var telemetryChannel = Channel.CreateUnbounded<TelemetryItem>(new UnboundedChannelOptions()
            {
                SingleReader = true,
                SingleWriter = true
            });

            var processorTask = ProcessMessageChannelAsync(
                subscriptionId,
                messageChannel.Reader,
                ws,
                onMessageAsync,
                () => lastHeartbeat = DateTime.UtcNow,
                shutdownCancellationTokenSource,
                detectedDisconnectKind => pendingDisconnectKind = detectedDisconnectKind,
                telemetryChannel.Writer,
                onDisconnectNoticeReceived,
                cancellationToken);

            var telemetryTask = TelemetryLoopAsync(
                subscriptionId,
                telemetryChannel.Reader,
                counterLogger,
                latencyLogger,
                sequenceLogger,
                sequenceDetector,
                shutdownCancellationToken);

            var heartbeatTask = StartHeartbeatMonitorAsync(ws, () => lastHeartbeat, shutdownCancellationTokenSource, shutdownCancellationToken);
            var receivedAtMillis = 0L;
            try
            {
                while (ws.State == WebSocketState.Open && !shutdownCancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;

                    try
                    {
                        result = await ws.ReceiveAsync(buffer, shutdownCancellationToken);
                        receivedAtMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    }
                    catch (OperationCanceledException) when (shutdownCancellationToken.IsCancellationRequested)
                    {
                        return new ReceiveLoopResult(false, pendingDisconnectKind);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Receive failed, disconnecting.");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        logger.LogWarning("Server closed WebSocket.");
                        break;
                    }

                    var payload = buffer.AsSpan(0, result.Count).ToArray();

                    if ((result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary) &&
                        !messageChannel.Writer.TryWrite(new IncomingMessage(result.MessageType, payload, receivedAtMillis)))
                    {
                        logger.LogWarning("Incoming message channel was completed before message enqueue for subscription {SubscriptionId}.", subscriptionId);
                    }
                }
            }
            finally
            {
                messageChannel.Writer.TryComplete();
                await IgnoreCancellationAsync(processorTask);
                await IgnoreCancellationAsync(telemetryTask);

                var retiringGracefully = gracefulCloseToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
                await shutdownCancellationTokenSource.CancelAsync();

                if (retiringGracefully)
                {
                    await CloseWebSocketGracefullyAsync(ws);
                }
                else
                {
                    AbortWebSocket(ws);
                }

                await IgnoreCancellationAsync(heartbeatTask);
            }

            var shouldReconnect = !cancellationToken.IsCancellationRequested && !gracefulCloseToken.IsCancellationRequested;
            return new ReceiveLoopResult(shouldReconnect, pendingDisconnectKind);
        }

        /// <summary>
        /// Closes a connection that is being deliberately retired (e.g. superseded by a replacement
        /// connection) using a normal WebSocket close handshake instead of an abrupt abort.
        /// </summary>
        private async Task CloseWebSocketGracefullyAsync(ClientWebSocket ws)
        {
            if (ws.State != WebSocketState.Open)
            {
                AbortWebSocket(ws);
                return;
            }

            try
            {
                using var closeTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Retired in favor of replacement connection", closeTimeoutCts.Token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Graceful WebSocket close did not complete in time; aborting instead.");
            }
            finally
            {
                AbortWebSocket(ws);
            }
        }

        private async Task ProcessMessageChannelAsync(
            Guid subscriptionId,
            ChannelReader<IncomingMessage> reader,
            ClientWebSocket ws,
            Func<string, Task> onMessageAsync,
            Action updateLastHeartbeat,
            CancellationTokenSource shutdownCancellationTokenSource,
            Action<DisconnectKind> setDisconnectKind,
            ChannelWriter<TelemetryItem> telemetryWriter,
            Action<int?>? onDisconnectNoticeReceived,
            CancellationToken cancellationToken)
        {
            var disconnectNoticeSent = false;

            try
            {
                await foreach (var message in reader.ReadAllAsync(cancellationToken))
                {
                    var jsonMessage = await DeserializeIncomingMessageAsync(message);

                    if (jsonMessage == null)
                    {
                        logger.LogWarning("Deserialized message is null. Skipping.");
                        continue;
                    }

                    if (jsonMessage.Contains(EventTypes.HeartBeat, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!await HandleHeartbeatAsync(
                            subscriptionId,
                            ws,
                            updateLastHeartbeat,
                            shutdownCancellationTokenSource,
                            cancellationToken))
                        {
                            break;
                        }

                        continue;
                    }

                    if (TryGetDisconnectKind(jsonMessage, out var disconnectKind))
                    {
                        setDisconnectKind(disconnectKind);

                        if (!disconnectNoticeSent && ShouldArbitrate(jsonMessage, out var noticeMinutes))
                        {
                            disconnectNoticeSent = true;
                            onDisconnectNoticeReceived?.Invoke(noticeMinutes);
                        }
                    }

                    telemetryWriter.TryWrite(new TelemetryItem(message.MessageType, jsonMessage, message.ReceivedAtMillis));

                    await onMessageAsync(jsonMessage);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Swallow cancellation
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Message processor failed.");
            }
            finally
            {
                telemetryWriter.TryComplete();
            }
        }

        private async Task<string?> DeserializeIncomingMessageAsync(IncomingMessage message)
        {
            if (message.MessageType == WebSocketMessageType.Text)
            {
                return Encoding.UTF8.GetString(message.Payload);
            }

            if (message.MessageType != WebSocketMessageType.Binary)
            {
                return null;
            }

            try
            {
                return await avroBinaryDeserializer.DeserializeAsync<string>(message.Payload);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process binary Avro message. Skipping.");
                return null;
            }
        }

        private async Task<bool> HandleHeartbeatAsync(
            Guid subscriptionId,
            ClientWebSocket ws,
            Action updateLastHeartbeat,
            CancellationTokenSource shutdownCancellationTokenSource,
            CancellationToken cancellationToken)
        {
            updateLastHeartbeat();

            return await TrySendHeartbeatAckAsync(
                subscriptionId,
                ws,
                shutdownCancellationTokenSource,
                cancellationToken);
        }

        private static void AbortWebSocket(ClientWebSocket ws)
        {
            if (ws.State == WebSocketState.Closed || ws.State == WebSocketState.Aborted)
            {
                return;
            }

            try
            {
                ws.Abort();
            }
            catch (ObjectDisposedException)
            {
                // Socket was already disposed during shutdown.
            }
        }

        private async Task IgnoreCancellationAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Swallow cancellation
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Background task stopped while receive loop was shutting down.");
            }
        }

        private async Task TelemetryLoopAsync(
            Guid subscriptionId,
            ChannelReader<TelemetryItem> reader,
            ICounterLogger? counterLogger,
            ILatencyLogger? latencyLogger,
            ISequenceLogger? sequenceLogger,
            SequenceGapDetector? sequenceDetector,
            CancellationToken cancellationToken)
        {
            // Reuse the caller-supplied detector (shared across a logical subscription's physical
            // connections, including arbitration handovers) when given one; only own - and later
            // clear - a detector we created ourselves for just this one connection attempt.
            var ownsDetector = sequenceDetector is null && sequenceLogger != null;
            sequenceDetector ??= sequenceLogger != null ? new SequenceGapDetector(subscriptionId, sequenceLogger) : null;

            void Flush()
            {
                latencyLogger?.Flush();
                counterLogger?.Flush();
                sequenceLogger?.Flush();
            }
            try
            {
                var lastFlushTick = Environment.TickCount64;

                while (await reader.WaitToReadAsync(cancellationToken))
                {
                    while (reader.TryRead(out var item))
                    {

                        //process telemetry

                        counterLogger?.Increment(subscriptionId);

                        var messagePacket = JsonConvert.DeserializeObject<MessagePacketEnvelope>(item.jsonMessage);

                        if (messagePacket == null)
                        {
                            logger.LogWarning("Failed to deserialize message for telemetry. Message: {Message}", item.jsonMessage);
                            continue;
                        }

                        if (ProcessMessageSequenceDetection(item, messagePacket))
                        {
                            sequenceDetector?.Process(messagePacket.PerformanceId, messagePacket.EventType, messagePacket.SequenceNumber);
                        }

                        if (messagePacket!.PublishTime.HasValue && messagePacket.PublishTime.Value > 0)
                        {
                            var publishTimeMillis = messagePacket.PublishTime.Value / 1_000_000;
                            var latencyMillis = item.ReceivedAtMillis - publishTimeMillis;

                            if (latencyMillis >= 0)
                            {
                                latencyLogger?.RecordLatency(subscriptionId, latencyMillis);
                            }
                        }

                        var nowTick = Environment.TickCount64;
                        if (nowTick - lastFlushTick >= FlushIntervalMillis)
                        {
                            sequenceDetector?.Prune(nowTick);
                            Flush();
                            lastFlushTick = nowTick;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in telemetry loop for subscription {SubscriptionId}.", subscriptionId);
            }
            finally
            {
                try
                {
                    Flush();
                    if (ownsDetector)
                    {
                        sequenceDetector?.Clear();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to flush telemetry on shutdown for subscription {SubscriptionId}.", subscriptionId);
                }
            }
        }

        private async Task StartHeartbeatMonitorAsync(
            ClientWebSocket ws,
            Func<DateTime> getLastHeartbeat,
            CancellationTokenSource shutdownCancellationTokenSource,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                await Task.Delay(heartbeatCheckInterval, cancellationToken);

                if (DateTime.UtcNow - getLastHeartbeat() > heartbeatTimeout)
                {
                    logger.LogWarning("Server heartbeat timeout. Aborting WebSocket.");
                    await shutdownCancellationTokenSource.CancelAsync();
                    AbortWebSocket(ws);
                    break;
                }
            }
        }

        private async Task<bool> TrySendHeartbeatAckAsync(
            Guid subscriptionId,
            ClientWebSocket ws,
            CancellationTokenSource shutdownCancellationTokenSource,
            CancellationToken cancellationToken)
        {
            var acknowledgement = new MessagePacketEnvelope
            {
                EventType = EventTypes.HeartBeatAcknowledged,
                AcknowledgedTime = DateTimeHelper.NanosFromEpoch()
            };

            var json = JsonConvert.SerializeObject(acknowledgement);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var heartbeatAcknowledgementCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            heartbeatAcknowledgementCancellationTokenSource.CancelAfter(heartbeatAcknowledgementTimeout);

            try
            {
                await ws.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    heartbeatAcknowledgementCancellationTokenSource.Token);

                return true;
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Timed out sending heartbeat acknowledgement for subscription {SubscriptionId}. Aborting WebSocket.", subscriptionId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send heartbeat acknowledgement for subscription {SubscriptionId}. Aborting WebSocket.", subscriptionId);
            }

            await shutdownCancellationTokenSource.CancelAsync();
            AbortWebSocket(ws);
            return false;
        }

        private static string ToDisconnectType(DisconnectKind disconnectKind)
        {
            return disconnectKind == DisconnectKind.Expected
                ? ExpectedDisconnectType
                : UnexpectedDisconnectType;
        }

        internal static bool TryGetExpectedDisconnectType(string jsonMessage, out string disconnectType)
        {
            disconnectType = UnexpectedDisconnectType;

            if (!TryGetDisconnectKind(jsonMessage, out var disconnectKind))
            {
                return false;
            }

            disconnectType = ToDisconnectType(disconnectKind);
            return true;
        }

        internal static string GetPendingDisconnectType(string jsonMessage)
        {
            return ToDisconnectType(GetNextPendingDisconnectKind(jsonMessage));
        }

        internal static string GetUpdatedPendingDisconnectType(string currentDisconnectType, string jsonMessage)
        {
            var currentDisconnectKind = string.Equals(currentDisconnectType, ExpectedDisconnectType, StringComparison.OrdinalIgnoreCase)
                ? DisconnectKind.Expected
                : DisconnectKind.Unexpected;

            return ToDisconnectType(UpdatePendingDisconnectKind(currentDisconnectKind, jsonMessage));
        }

        private static DisconnectKind UpdatePendingDisconnectKind(DisconnectKind currentDisconnectKind, string jsonMessage)
        {
            if (currentDisconnectKind == DisconnectKind.Expected)
            {
                return DisconnectKind.Expected;
            }

            return TryGetDisconnectKind(jsonMessage, out var disconnectKind)
                ? disconnectKind
                : currentDisconnectKind;
        }

        private static DisconnectKind GetNextPendingDisconnectKind(string jsonMessage)
        {
            return TryGetDisconnectKind(jsonMessage, out var disconnectKind)
                ? disconnectKind
                : DisconnectKind.Unexpected;
        }

        private static bool TryGetDisconnectKind(string jsonMessage, out DisconnectKind disconnectKind)
        {
            disconnectKind = DisconnectKind.Unexpected;

            try
            {
                var payload = JObject.Parse(jsonMessage);

                if (IsExpectedDisconnect(payload))
                {
                    disconnectKind = DisconnectKind.Expected;
                    return true;
                }
            }
            catch (JsonException)
            {
                // Ignore invalid telemetry payloads for lifecycle classification.
            }

            return false;
        }

        private static bool IsExpectedDisconnect(JObject payload)
        {
            var eventType = GetPropertyCaseInsensitive(payload, "EventType")?.Value<string>();
            if (string.Equals(eventType, EventTypes.Admin, StringComparison.OrdinalIgnoreCase))
            {
                var message = GetPropertyCaseInsensitive(payload, "Message");
                return string.Equals(
                    GetPropertyCaseInsensitive(message, "NoticeType")?.Value<string>(),
                    "Disconnect",
                    StringComparison.OrdinalIgnoreCase);
            }

            var eventTypes = GetPropertyCaseInsensitive(payload, "EventTypes") as JArray;
            var hasAdminEventType = eventTypes?.Values<string>()
                .Any(value => string.Equals(value, EventTypes.Admin, StringComparison.OrdinalIgnoreCase)) == true;

            if (!hasAdminEventType)
            {
                return false;
            }

            var admin = GetPropertyCaseInsensitive(payload, "Admin");
            return string.Equals(
                GetPropertyCaseInsensitive(admin, "NoticeType")?.Value<string>(),
                "Disconnect",
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Whether an already-classified Admin/Disconnect notice also opts into arbitration
        /// (proactive replacement connection) via its "Arbitrate" flag. Only called for messages
        /// that already matched <see cref="IsExpectedDisconnect"/>, so this is not on the hot path.
        /// </summary>
        /// <param name="noticeMinutes">The notice's NoticeMinutes value, if present - how long until the server actually closes the connection.</param>
        internal static bool ShouldArbitrate(string jsonMessage, out int? noticeMinutes)
        {
            noticeMinutes = null;

            try
            {
                var payload = JObject.Parse(jsonMessage);
                var eventType = GetPropertyCaseInsensitive(payload, "EventType")?.Value<string>();

                var noticePayload = string.Equals(eventType, EventTypes.Admin, StringComparison.OrdinalIgnoreCase)
                    ? GetPropertyCaseInsensitive(payload, "Message")
                    : GetPropertyCaseInsensitive(payload, "Admin");

                if (GetPropertyCaseInsensitive(noticePayload, "Arbitrate")?.Value<bool?>() != true)
                {
                    return false;
                }

                noticeMinutes = GetPropertyCaseInsensitive(noticePayload, "NoticeMinutes")?.Value<int?>();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static JToken? GetPropertyCaseInsensitive(JToken? token, string propertyName)
        {
            return (token as JObject)?.Property(propertyName, StringComparison.OrdinalIgnoreCase)?.Value;
        }

        private bool ProcessMessageSequenceDetection(TelemetryItem item, MessagePacketEnvelope messagePacket)
        {
            if (IsAdminMessage(messagePacket) || IsSnapshotMessage(messagePacket))
            {
                // Admin/control messages (e.g. disconnect notices) legitimately carry no
                // PerformanceId or SequenceNumber; they are not subject to sequence tracking.
                return false;
            }

            if (!messagePacket.SequenceNumber.HasValue || string.IsNullOrEmpty(messagePacket.PerformanceId) || string.IsNullOrEmpty(messagePacket.EventType))
            {
                logger.LogWarning("Message missing required fields for telemetry sequence detection. Message: {Message}", item.jsonMessage);
            }

            return true;
        }

        internal static bool IsAdminMessage(MessagePacketEnvelope messagePacket)
            => string.Equals(messagePacket.EventType, EventTypes.Admin, StringComparison.OrdinalIgnoreCase);

        internal static bool IsSnapshotMessage(MessagePacketEnvelope messagePacket)
            => string.Equals(messagePacket.EventType, EventTypes.Snapshot, StringComparison.OrdinalIgnoreCase);
    }
}