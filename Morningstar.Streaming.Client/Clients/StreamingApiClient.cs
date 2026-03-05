using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services.AvroBinaryDeserializer;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Constants;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

namespace Morningstar.Streaming.Client.Clients
{
    public class StreamingApiClient : IStreamingApiClient
    {
        private const int FlushIntervalMillis = 1000;
        private readonly IApiHelper apiHelper;
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<StreamingApiClient> logger;
        private readonly IAvroBinaryDeserializer avroBinaryDeserializer;
        private readonly TimeSpan heartbeatTimeout = TimeSpan.FromSeconds(30);
        private readonly TimeSpan heartbeatCheckInterval = TimeSpan.FromSeconds(5);

        private readonly record struct IncomingMessage(WebSocketMessageType MessageType, byte[] Payload, long ReceivedAtMillis);

        private readonly record struct TelemetryItem(WebSocketMessageType MessageType, string jsonMessage, long ReceivedAtMillis);

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
            return SubscribeAsync(subscriptionId, webSocketUrl, purpose, onMessageAsync, connected, cancellationToken, null, null);
        }

        public async Task SubscribeAsync(
            Guid subscriptionId,
            string webSocketUrl,
            string? purpose,
            Func<string, Task> onMessageAsync,
            TaskCompletionSource<bool> connected,
            CancellationToken cancellationToken,
            ICounterLogger? counterLogger,
            ILatencyLogger? latencyLogger)
        {
            await ConnectWithRetryAsync(
                subscriptionId,
                webSocketUrl,
                purpose,
                onMessageAsync,
                connected,
                counterLogger,
                latencyLogger,
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
            CancellationToken cancellationToken)
        {
            const int maxAttempts = 5;
            int attempt = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                attempt++;

                try
                {
                    using var ws = await ConnectWebSocketAsync(webSocketUrl, purpose, cancellationToken);

                    logger.LogInformation("WebSocket connected on attempt {Attempt}.", attempt);

                    // Signal connection established
                    connected.TrySetResult(true);

                    // Reset attempt counter after successful connection
                    attempt = 0;

                    await StartReceiveLoopAsync(subscriptionId, ws, onMessageAsync, cancellationToken, counterLogger, latencyLogger);

                    // Connection ended gracefully - reset counter and retry
                    logger.LogInformation("WebSocket disconnected. Attempting to reconnect...");
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    // Only exit if cancellation was explicitly requested
                    logger.LogInformation(ex, "Cancellation requested. Stopping WebSocket connection.");
                    if (!connected.Task.IsCompleted) connected.TrySetCanceled();
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "WebSocket failed (attempt {Attempt} of {MaxAttempts}). Reconnecting...", attempt, maxAttempts);

                    if (attempt >= maxAttempts)
                    {
                        logger.LogError("Maximum retry attempts ({MaxAttempts}) reached. Stopping WebSocket connection.", maxAttempts);
                        if (!connected.Task.IsCompleted)
                            connected.TrySetException(ex);
                        throw;
                    }

                    // Check if cancellation was requested before delaying
                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogInformation("Cancellation requested during reconnect delay. Stopping WebSocket connection.");
                        if (!connected.Task.IsCompleted) connected.TrySetCanceled();
                        return;
                    }

                    var delaySeconds = Math.Min(Math.Pow(2, attempt), 30);
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    }
                    catch (OperationCanceledException delayEx)
                    {
                        // Cancellation during delay - exit gracefully
                        logger.LogInformation(delayEx, "Cancellation requested during reconnect delay. Stopping WebSocket connection.");
                        if (!connected.Task.IsCompleted) connected.TrySetCanceled();
                        return;
                    }
                }
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

        private async Task StartReceiveLoopAsync(
        Guid subscriptionId,
        ClientWebSocket ws,
        Func<string, Task> onMessageAsync,
        CancellationToken cancellationToken,
        ICounterLogger? counterLogger,
        ILatencyLogger? latencyLogger)
        {
            var buffer = new byte[4096];
            var lastHeartbeat = DateTime.UtcNow;

            var messageChannel = Channel.CreateBounded<IncomingMessage>(new BoundedChannelOptions(100_000)
            {
                SingleReader = true,
                SingleWriter = true
            });

            var telemetryChannel = Channel.CreateBounded<TelemetryItem>(new BoundedChannelOptions(100_000)
            {
                SingleReader = true,
                SingleWriter = true
            });

            var processorTask = ProcessMessageChannelAsync(
                messageChannel.Reader,
                ws,
                onMessageAsync,
                () => lastHeartbeat = DateTime.UtcNow,
                telemetryChannel.Writer,
                cancellationToken);

            var telemetryTask = TelemetryLoopAsync(
                subscriptionId,
                telemetryChannel.Reader,
                counterLogger, 
                latencyLogger, 
                cancellationToken);

            var heartbeatTask = StartHeartbeatMonitorAsync(ws, () => lastHeartbeat, cancellationToken);
            var receivedAtMillis = 0L;
            while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result;

                try
                {
                    result = await ws.ReceiveAsync(buffer, cancellationToken);
                    receivedAtMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var payload = buffer.AsSpan(0, result.Count).ToArray();
                    messageChannel.Writer.TryWrite(new IncomingMessage(WebSocketMessageType.Text, payload, receivedAtMillis));
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var payload = buffer.AsSpan(0, result.Count).ToArray();
                    messageChannel.Writer.TryWrite(new IncomingMessage(WebSocketMessageType.Binary, payload, receivedAtMillis));
                }
            }

            messageChannel.Writer.TryComplete();

            await Task.WhenAny(processorTask, telemetryTask, heartbeatTask);
        }

        private async Task ProcessMessageChannelAsync(
            ChannelReader<IncomingMessage> reader,
            ClientWebSocket ws,
            Func<string, Task> onMessageAsync,
            Action updateLastHeartbeat,
            ChannelWriter<TelemetryItem> telemetryWriter,
            CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var message in reader.ReadAllAsync(cancellationToken))
                {
                    string? jsonMessage = null;

                    if (message.MessageType == WebSocketMessageType.Text)
                    {
                        jsonMessage = Encoding.UTF8.GetString(message.Payload);
                    }
                    else if (message.MessageType == WebSocketMessageType.Binary)
                    {
                        try
                        {
                            jsonMessage = await avroBinaryDeserializer.DeserializeAsync<string>(message.Payload);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to process binary Avro message. Skipping.");
                            continue;
                        }
                    }

                    if (jsonMessage == null)
                    {
                        logger.LogWarning("Deserialized message is null. Skipping.");
                        continue;
                    }

                    if (jsonMessage.Contains(EventTypes.HeartBeat, StringComparison.OrdinalIgnoreCase))
                    {
                        updateLastHeartbeat();
                        await SendHeartbeatAckAsync(ws, cancellationToken);
                        continue;
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
        } 

        private async Task TelemetryLoopAsync(
            Guid subscriptionId,
            ChannelReader<TelemetryItem> reader,
            ICounterLogger? counterLogger,
            ILatencyLogger?  latencyLogger,
            CancellationToken cancellationToken)
        {
            void Flush()
            {
                latencyLogger?.Flush();
                counterLogger?.Flush();
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

                        if(messagePacket == null)
                        {
                            logger.LogWarning("Failed to deserialize message for telemetry. Message: {Message}", item.jsonMessage);
                            continue;
                        }

                        if(messagePacket!.PublishTime.HasValue && messagePacket.PublishTime.Value > 0)
                        {
                            latencyLogger?.RecordLatency(subscriptionId, item.ReceivedAtMillis - messagePacket.PublishTime.Value);
                        }

                        var nowTick = Environment.TickCount64;
                        if (nowTick - lastFlushTick >= FlushIntervalMillis)
                        {
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
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                await Task.Delay(heartbeatCheckInterval, cancellationToken);

                if (DateTime.UtcNow - getLastHeartbeat() > heartbeatTimeout)
                {
                    logger.LogWarning("Server heartbeat timeout. Closing WebSocket.");
                    if (ws.State is WebSocketState.Open or WebSocketState.CloseSent)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Heartbeat timeout", cancellationToken);
                    }
                    break;
                }
            }
        }

        private async Task SendHeartbeatAckAsync(ClientWebSocket ws, CancellationToken cancellationToken)
        {
            var acknowledgement = new MessagePacketEnvelope
            {
                EventType = EventTypes.HeartBeatAcknowledged,
                AcknowledgedTime = DateTimeHelper.NanosFromEpoch()
            };

            var json = JsonConvert.SerializeObject(acknowledgement);
            var bytes = Encoding.UTF8.GetBytes(json);

            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
        }

    }
}
