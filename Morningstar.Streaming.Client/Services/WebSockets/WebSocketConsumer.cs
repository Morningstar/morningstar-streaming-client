using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Domain;
using Newtonsoft.Json;

namespace Morningstar.Streaming.Client.Services.WebSockets
{
    public class WebSocketConsumer : IWebSocketConsumer
    {
        private const string RetriesExhaustedDisconnectType = "RetriesExhausted";
        // Task.Delay's documented safe upper bound (int.MaxValue milliseconds); NoticeMinutes is
        // server-controlled input and must never be allowed to exceed it.
        private static readonly TimeSpan MaxRetirementTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private readonly ILogger<WebSocketConsumer> logger;
        private readonly IStreamingApiClient client;
        private readonly string wsUrl;
        private readonly bool logToFile;
        private readonly string? purpose;

        private readonly ICounterLogger? counterLogger;
        private readonly ILatencyLogger? latencyLogger;
        private readonly ISequenceLogger? sequenceLogger;
        private readonly ILogger eventsLogger;
        private readonly Channel<string> channel;
        private readonly Guid topicGuid;
        private readonly string serializationFormat;
        private readonly TimeSpan defaultArbitrationRetirementTimeout;
        // Shared across every physical connection for this consumer's lifetime so a replacement
        // connection isn't cold-started with no memory of what the retiring one already saw.
        private readonly SequenceGapDetector? sequenceDetector;

        /// <inheritdoc />
        public IWebSocketConsumerObserver? Observer { get; set; }

        public WebSocketConsumer
        (
            ICounterLogger? counterLogger,
            ILatencyLogger? latencyLogger,
            IWebSocketLoggerFactory wsLoggerFactory,
            ILogger<WebSocketConsumer> logger,
            IStreamingApiClient client,
            string wsUrl,
            bool logToFile,
            string? purpose,
            ISequenceLogger? sequenceLogger = null,
            int defaultArbitrationRetirementMinutes = 5
        )
        {
            this.logger = logger;
            this.client = client;
            this.wsUrl = wsUrl;
            this.logToFile = logToFile;
            this.purpose = purpose;
            this.counterLogger = counterLogger;
            this.latencyLogger = latencyLogger;
            this.sequenceLogger = sequenceLogger;
            defaultArbitrationRetirementTimeout = TimeSpan.FromMinutes(defaultArbitrationRetirementMinutes);

            channel = Channel.CreateUnbounded<string>();

            var pathSegments = wsUrl
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var lastSegment = pathSegments.Length >= 1 ? pathSegments[^1] : string.Empty;
            var topicGuidSegment = Guid.TryParse(lastSegment, out _)
                ? lastSegment
                : pathSegments.Length >= 2 ? pathSegments[^2] : string.Empty;
            Guid.TryParse(topicGuidSegment, out var topicGuidResult);
            serializationFormat = Guid.TryParse(lastSegment, out _) ? string.Empty : lastSegment;

            topicGuid = topicGuidResult;
            eventsLogger = wsLoggerFactory.GetLogger(topicGuid);
            sequenceDetector = sequenceLogger != null ? new SequenceGapDetector(topicGuid, sequenceLogger) : null;
        }

        public async Task StartConsumingAsync(TaskCompletionSource<bool> connectedTcs, CancellationToken cancellationToken = default)
        {
            counterLogger?.RegisterSubscription(topicGuid, Guid.Empty, serializationFormat, purpose);
            latencyLogger?.RegisterSubscription(topicGuid, serializationFormat, purpose);
            sequenceLogger?.RegisterSubscription(topicGuid, serializationFormat, purpose);
            var logTask = LogFromChannelAsync(cancellationToken);

            var coordinator = new ArbitrationCoordinator();
            var active = StartPhysicalSession(connectedTcs, coordinator, isIncomingSession: false, cancellationToken);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var completed = await Task.WhenAny(active.NoticeReceived.Task, active.RunTask);

                    if (completed != active.NoticeReceived.Task)
                    {
                        // Physical connection ended on its own (cancelled, or exhausted retries) without
                        // ever seeing an Admin/Disconnect notice - nothing to arbitrate.
                        await active.RunTask;

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            logger.LogWarning("WebSocket subscription task completed unexpectedly without cancellation.");
                            NotifyDisconnected(wsUrl, RetriesExhaustedDisconnectType);
                        }

                        return;
                    }

                    // Admin/Disconnect notice observed: bring up a replacement connection while the
                    // current one keeps running, then dedupe messages between them until it is safe
                    // to retire the old one.
                    var noticeMinutes = active.NoticeReceived.Task.Result;
                    var retirementTimeout = ClampRetirementTimeout(noticeMinutes.HasValue ? TimeSpan.FromMinutes(noticeMinutes.Value) : defaultArbitrationRetirementTimeout);
                    var incoming = StartPhysicalSession(new TaskCompletionSource<bool>(), coordinator, isIncomingSession: true, cancellationToken);
                    coordinator.BeginOverlap();

                    var timedOut = await WaitForRetirementAsync(active, coordinator, retirementTimeout, cancellationToken);
                    var confirmed = coordinator.DuplicateSeenOnIncoming.IsCompletedSuccessfully;
                    var replacementFailed = incoming.RunTask.IsFaulted;

                    if (!active.RunTask.IsCompleted)
                    {
                        active.GracefulCloseSource.Cancel();
                    }

                    await IgnoreExceptionsAsync(active.RunTask);
                    NotifyArbitrationCompleted(new ArbitrationOutcome(topicGuid, purpose, confirmed, replacementFailed, timedOut));
                    active.GracefulCloseSource.Dispose();
                    coordinator.EndOverlap();

                    if (replacementFailed)
                    {
                        // Never promote a connection that never came up - the old one is already
                        // gone too (that's why we got here), so there is nothing left to serve this
                        // subscription with. Already reported via OnArbitrationCompleted(ReplacementFailed=true).
                        logger.LogError("Replacement WebSocket connection failed to establish during arbitration for subscription {SubscriptionId}; ending consumer.", topicGuid);
                        await IgnoreExceptionsAsync(incoming.RunTask);
                        incoming.GracefulCloseSource.Dispose();
                        return;
                    }

                    active = incoming;
                }

                await IgnoreExceptionsAsync(active.RunTask);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation(ex, "Cancellation requested. Stopping consumer.");
                await IgnoreExceptionsAsync(active.RunTask);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WebSocket consumer failed unexpectedly.");
                NotifyDisconnected(wsUrl, RetriesExhaustedDisconnectType);
            }
            finally
            {
                // Disposes whichever session is still current: a no-op if the arbitration loop already
                // disposed it during a handover, but the only place the very last session gets disposed.
                active.GracefulCloseSource.Dispose();
                channel.Writer.Complete();
                await logTask;
                counterLogger?.UnregisterSubscription(topicGuid);
                latencyLogger?.UnregisterSubscription(topicGuid);
                sequenceLogger?.UnregisterSubscription(topicGuid);
            }
        }

        /// <summary>
        /// Waits until either the incoming connection has seen one duplicated message (proof it has
        /// caught up with the retiring one), the retiring connection ends on its own - e.g. the server
        /// kills it - or the given timeout elapses (e.g. the server told us via NoticeMinutes it would
        /// disconnect us by now, but never confirmed dedupe) - whichever happens first.
        /// </summary>
        /// <returns>True if the timeout elapsed before either of the other two outcomes.</returns>
        private static async Task<bool> WaitForRetirementAsync(PhysicalSession retiring, ArbitrationCoordinator coordinator, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
            var timeoutTask = Task.Delay(timeout, cancellationToken);
            var completed = await Task.WhenAny(retiring.RunTask, coordinator.DuplicateSeenOnIncoming, timeoutTask, cancellationTask);
            return completed == timeoutTask;
        }

        private PhysicalSession StartPhysicalSession(TaskCompletionSource<bool> connectedTcs, ArbitrationCoordinator coordinator, bool isIncomingSession, CancellationToken cancellationToken)
        {
            var noticeReceived = new TaskCompletionSource<int?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var gracefulCloseSource = new CancellationTokenSource();

            var runTask = client.SubscribeAsync(
                topicGuid,
                wsUrl,
                purpose,
                BuildMessageHandler(coordinator, isIncomingSession),
                connectedTcs,
                cancellationToken,
                counterLogger,
                latencyLogger,
                sequenceLogger,
                sequenceDetector,
                onDisconnectNoticeReceived: noticeMinutes => noticeReceived.TrySetResult(noticeMinutes),
                gracefulCloseToken: gracefulCloseSource.Token,
                onDisconnected: disconnectType => NotifyDisconnected(wsUrl, disconnectType),
                onReconnected: previousDisconnectType => NotifyReconnected(wsUrl, previousDisconnectType));

            return new PhysicalSession(runTask, noticeReceived, gracefulCloseSource);
        }

        private Func<string, Task<bool>> BuildMessageHandler(ArbitrationCoordinator coordinator, bool isIncomingSession) => message =>
        {
            if (coordinator.IsOverlapping)
            {
                var (performanceId, eventType, sequenceNumber) = TryParseSequenceKey(message);

                if (!coordinator.TryClaim(performanceId, eventType, sequenceNumber, isIncomingSession))
                {
                    // Already forwarded by the other, overlapping connection - suppress from both
                    // the file log and sequence telemetry; this is an expected handover artifact,
                    // not new data or a genuine duplicate.
                    return Task.FromResult(false);
                }
            }

            if (!channel.Writer.TryWrite(message))
            {
                logger.LogError("Failed to enqueue message into channel. Message: {Message}", message);
            }

            return Task.FromResult(true);
        };

        private static (string? PerformanceId, string? EventType, long? SequenceNumber) TryParseSequenceKey(string jsonMessage)
        {
            try
            {
                var packet = JsonConvert.DeserializeObject<MessagePacketEnvelope>(jsonMessage);
                return (packet?.PerformanceId, packet?.EventType, packet?.SequenceNumber);
            }
            catch (JsonException)
            {
                return (null, null, null);
            }
        }

        private async Task IgnoreExceptionsAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Retired WebSocket session for subscription {SubscriptionId} ended.", topicGuid);
            }
        }

        // Observer implementations are caller-supplied; a throwing observer must never be able to
        // skip our own cleanup (CancellationTokenSource disposal, coordinator bookkeeping) or corrupt
        // the connect/reconnect loop's failure accounting.
        private void NotifyDisconnected(string webSocketUrl, string disconnectType)
        {
            try
            {
                Observer?.OnDisconnected(topicGuid, purpose, webSocketUrl, disconnectType);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Observer.OnDisconnected threw for subscription {SubscriptionId}.", topicGuid);
            }
        }

        private void NotifyReconnected(string webSocketUrl, string previousDisconnectType)
        {
            try
            {
                Observer?.OnReconnected(topicGuid, purpose, webSocketUrl, previousDisconnectType);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Observer.OnReconnected threw for subscription {SubscriptionId}.", topicGuid);
            }
        }

        private void NotifyArbitrationCompleted(ArbitrationOutcome outcome)
        {
            try
            {
                Observer?.OnArbitrationCompleted(outcome);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Observer.OnArbitrationCompleted threw for subscription {SubscriptionId}.", topicGuid);
            }
        }

        private TimeSpan ClampRetirementTimeout(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero)
            {
                logger.LogWarning("Received a negative arbitration retirement timeout ({Timeout}) for subscription {SubscriptionId}; treating as immediate.", timeout, topicGuid);
                return TimeSpan.Zero;
            }

            if (timeout > MaxRetirementTimeout)
            {
                logger.LogWarning("Arbitration retirement timeout ({Timeout}) for subscription {SubscriptionId} exceeds the maximum supported delay; clamping to {MaxTimeout}.", timeout, topicGuid, MaxRetirementTimeout);
                return MaxRetirementTimeout;
            }

            return timeout;
        }

        private async Task LogFromChannelAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (logToFile) eventsLogger.LogInformation("{WebSocketMessage}", message);
                }
            }
            catch (OperationCanceledException ex)
            {
                logger.LogInformation(ex, "LogFromChannelAsync cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception in LogFromChannelAsync.");
            }
        }

        /// <summary>A single physical WebSocket connection under management by this logical subscription.</summary>
        private sealed record PhysicalSession(Task RunTask, TaskCompletionSource<int?> NoticeReceived, CancellationTokenSource GracefulCloseSource);

        /// <summary>
        /// Tracks message-forwarding dedup while two physical connections are running concurrently
        /// during an Admin/Disconnect-triggered handover, and signals once the incoming connection
        /// has proven it is caught up on every PerformanceId the retiring one delivered during the
        /// overlap - not just the first one, since a subscription can multiplex many instruments on
        /// one socket and an early duplicate for a busy instrument proves nothing about a quieter
        /// one. Not used at all outside an overlap window, so there is zero overhead in the common
        /// case.
        /// </summary>
        private sealed class ArbitrationCoordinator
        {
            private readonly ConcurrentDictionary<(string PerformanceId, string EventType, long Sequence), byte> claimed = new();
            private readonly ConcurrentDictionary<string, byte> expectedKeys = new();
            private readonly ConcurrentDictionary<string, byte> confirmedKeys = new();
            private readonly TaskCompletionSource<bool> allKeysConfirmed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private volatile bool overlapping;

            public bool IsOverlapping => overlapping;

            /// <summary>Completes once the incoming connection has delivered a message for every PerformanceId the retiring connection delivered during the overlap.</summary>
            public Task DuplicateSeenOnIncoming => allKeysConfirmed.Task;

            public void BeginOverlap() => overlapping = true;

            public void EndOverlap()
            {
                overlapping = false;
                claimed.Clear();
                expectedKeys.Clear();
                confirmedKeys.Clear();
            }

            public bool TryClaim(string? performanceId, string? eventType, long? sequenceNumber, bool isIncomingSession)
            {
                if (performanceId is null || eventType is null || sequenceNumber is null)
                {
                    // No dedupe key available (e.g. heartbeat/admin messages) - always forward.
                    return true;
                }

                var claimedNow = claimed.TryAdd((performanceId, eventType, sequenceNumber.Value), 0);

                if (!isIncomingSession)
                {
                    // Retiring connection: every distinct instrument it still delivers during the
                    // overlap is one the incoming connection must also prove it has caught up on.
                    expectedKeys.TryAdd(performanceId, 0);
                }
                else if (!claimedNow)
                {
                    // Incoming connection delivered a message already forwarded by the retiring one -
                    // proof of catch-up for this specific PerformanceId.
                    confirmedKeys.TryAdd(performanceId, 0);

                    if (expectedKeys.Count > 0 && AllExpectedKeysConfirmed())
                    {
                        allKeysConfirmed.TrySetResult(true);
                    }
                }

                return claimedNow;
            }

            private bool AllExpectedKeysConfirmed()
            {
                foreach (var key in expectedKeys.Keys)
                {
                    if (!confirmedKeys.ContainsKey(key))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}