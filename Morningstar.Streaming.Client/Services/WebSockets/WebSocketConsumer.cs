using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services.Telemetry;

namespace Morningstar.Streaming.Client.Services.WebSockets
{
    public class WebSocketConsumer : IWebSocketConsumer
    {
        private readonly ILogger<WebSocketConsumer> logger;
        private readonly IStreamingApiClient client;
        private readonly string wsUrl;
        private readonly bool logToFile;
        private readonly string? purpose;

        private readonly ICounterLogger? counterLogger;
        private readonly ILatencyLogger? latencyLogger;
        private readonly IObservableMetric<IMetric>? observableMetric;
        private readonly ILogger eventsLogger;
        private readonly Channel<string> channel;
        private readonly Guid topicGuid;

        public WebSocketConsumer
        (
            ICounterLogger? counterLogger,
            ILatencyLogger? latencyLogger,
            IWebSocketLoggerFactory wsLoggerFactory,
            ILogger<WebSocketConsumer> logger,
            IStreamingApiClient client,
            IObservableMetric<IMetric>? observableMetric,
            string wsUrl,
            bool logToFile,
            string? purpose
        )
        {
            this.logger = logger;
            this.client = client;
            this.wsUrl = wsUrl;
            this.logToFile = logToFile;
            this.purpose = purpose;
            this.counterLogger = counterLogger;
            this.latencyLogger = latencyLogger;
            this.observableMetric = observableMetric;

            channel = Channel.CreateUnbounded<string>();

            var pathSegments = wsUrl
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var topicGuidSegment = pathSegments.Length >= 2 ? pathSegments[^2] : string.Empty;
            Guid.TryParse(topicGuidSegment, out var topicGuidResult);
            
            topicGuid = topicGuidResult;
            eventsLogger = wsLoggerFactory.GetLogger(topicGuid);
        }

        public async Task StartConsumingAsync(TaskCompletionSource<bool> connectedTcs, CancellationToken cancellationToken = default)
        {
            var serializationFormat = wsUrl[(wsUrl.LastIndexOf('/') + 1)..];
            counterLogger?.RegisterSubscription(topicGuid, Guid.Empty, serializationFormat, purpose);
            latencyLogger?.RegisterSubscription(topicGuid, serializationFormat, purpose);
            var logTask = LogFromChannelAsync(cancellationToken);

            try
            {
                var subTask = client.SubscribeAsync(
                    topicGuid,
                    wsUrl,
                    purpose,
                    async message =>
                    {
                        if (!channel.Writer.TryWrite(message))
                        {
                            logger.LogError("Failed to enqueue message into channel. Message: {Message}", message);
                        }
                        await Task.CompletedTask;
                    },
                    connectedTcs,
                    cancellationToken,
                    counterLogger,
                    latencyLogger);

                // Wait for either task to complete
                // SubscribeAsync should run indefinitely with retries
                // logTask will run until cancellation or channel completion
                await Task.WhenAny(logTask, subTask);

                _ = HandleSubscriptionTaskCompletion(subTask, cancellationToken);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation(ex, "Cancellation requested. Stopping consumer.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WebSocket consumer failed unexpectedly.");
                throw;
            }
            finally
            {
                channel.Writer.Complete();
                counterLogger?.UnregisterSubscription(topicGuid);
                latencyLogger?.UnregisterSubscription(topicGuid);
            }
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
                logger.LogWarning(ex, "LogFromChannelAsync cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception in LogFromChannelAsync.");
            }
        }

        private async Task RecordWebSocketDisconnectionMetric()
        {
            if (observableMetric == null)
            {
                return;
            }

            var metric = new AtomicLong { Value = 1 };
            _ = observableMetric.RecordMetric("WebSocketDisconnections", metric, new Dictionary<string, string>
                {
                    { "TopicGuid", topicGuid.ToString() },
                    { "WebSocketUrl", wsUrl }
                });
        }
        private async Task HandleSubscriptionTaskCompletion(Task subTask, CancellationToken cancellationToken)
        {
            // If subscription task faults, record disconnection metric
            if (IsUnexpectedDisconnection(subTask, cancellationToken))
            {
                await RecordWebSocketDisconnectionMetric();
            }

            // If we reach here due to cancellation, that's expected
            // If subTask completed unexpectedly, we should know about it
            if (IsUnexpectedCompletion(subTask, cancellationToken))
            {
                logger.LogWarning("WebSocket subscription task completed unexpectedly without cancellation.");
            }
        }

        private static bool IsUnexpectedDisconnection(Task subTask, CancellationToken cancellationToken)
            => subTask.IsFaulted && !cancellationToken.IsCancellationRequested;

        private static bool IsUnexpectedCompletion(Task subTask, CancellationToken cancellationToken)
            => subTask.IsCompleted && !subTask.IsFaulted && !cancellationToken.IsCancellationRequested;
    }
}