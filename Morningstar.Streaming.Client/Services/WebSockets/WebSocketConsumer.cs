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
        private readonly string serializationFormat;

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

            var lastSegment = pathSegments.Length >= 1 ? pathSegments[^1] : string.Empty;
            var topicGuidSegment = Guid.TryParse(lastSegment, out _) 
                ? lastSegment 
                : pathSegments.Length >= 2 ? pathSegments[^2] : string.Empty;
            Guid.TryParse(topicGuidSegment, out var topicGuidResult);
            serializationFormat = Guid.TryParse(lastSegment, out _) ? string.Empty : lastSegment;
            
            topicGuid = topicGuidResult;
            eventsLogger = wsLoggerFactory.GetLogger(topicGuid);
        }

        public async Task StartConsumingAsync(TaskCompletionSource<bool> connectedTcs, CancellationToken cancellationToken = default)
        {
            counterLogger?.RegisterSubscription(topicGuid, Guid.Empty, serializationFormat, purpose);
            latencyLogger?.RegisterSubscription(topicGuid, serializationFormat, purpose);
            var logTask = LogFromChannelAsync(cancellationToken);

            try
            {
                await client.SubscribeAsync(
                    topicGuid,
                    wsUrl,
                    purpose,
                    async message =>
                    {
                        counterLogger?.Increment(topicGuid);

                        if (!channel.Writer.TryWrite(message))
                        {
                            logger.LogError("Failed to enqueue message into channel. Message: {Message}", message);
                        }

                        await Task.CompletedTask;
                    },
                    connectedTcs,
                    cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning("WebSocket subscription task completed unexpectedly without cancellation.");
                }
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation(ex, "Cancellation requested. Stopping consumer.");
            }
            catch (Exception ex)
            {
                await RecordWebSocketDisconnectionMetric();
                logger.LogError(ex, "WebSocket consumer failed unexpectedly.");
            }
            finally
            {
                channel.Writer.Complete();
                await logTask;
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
    }
}