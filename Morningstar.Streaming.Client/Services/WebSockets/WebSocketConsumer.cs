using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services.Counter;
using Morningstar.Streaming.Client.Services.Telemetry;

namespace Morningstar.Streaming.Client.Services.WebSockets
{
    public class WebSocketConsumer : IWebSocketConsumer
    {
        private readonly ILogger<WebSocketConsumer> logger;
        private readonly IStreamingApiClient client;
        private readonly string wsUrl;
        private readonly bool logToFile;

        private readonly ICounterLogger counterLogger;
        private readonly IObservableMetric<IMetric>? observableMetric;
        private readonly ILogger eventsLogger;
        private readonly Channel<string> channel;
        private readonly Guid topicGuid;

        public WebSocketConsumer
        (
            ICounterLogger counterLogger,
            IWebSocketLoggerFactory wsLoggerFactory,
            ILogger<WebSocketConsumer> logger,
            IStreamingApiClient client,
            IObservableMetric<IMetric>? observableMetric,
            string wsUrl,
            bool logToFile
        )
        {
            this.logger = logger;
            this.client = client;
            this.wsUrl = wsUrl;
            this.logToFile = logToFile;
            this.counterLogger = counterLogger;
            this.observableMetric = observableMetric;

            channel = Channel.CreateUnbounded<string>();

            Guid.TryParse(wsUrl.Substring(wsUrl.LastIndexOf('/') + 1), out Guid result);
            topicGuid = result;
            eventsLogger = wsLoggerFactory.GetLogger(topicGuid);
        }

        public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
        {
            counterLogger.RegisterSubscription(topicGuid);
            var logTask = LogFromChannelAsync(cancellationToken);

            try
            {
                var subTask = client.SubscribeAsync(
                    wsUrl,
                    async message =>
                    {
                        if (!channel.Writer.TryWrite(message))
                        {
                            logger.LogError("Failed to enqueue message into channel. Message: {Message}", message);
                        }
                        counterLogger.Increment(topicGuid);
                        await Task.CompletedTask;
                    },
                    cancellationToken);

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
                counterLogger.UnregisterSubscription(topicGuid);
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

        private bool IsUnexpectedDisconnection(Task subTask, CancellationToken cancellationToken)
            => subTask.IsFaulted && !cancellationToken.IsCancellationRequested;

        private bool IsUnexpectedCompletion(Task subTask, CancellationToken cancellationToken)
            => subTask.IsCompleted && !subTask.IsFaulted && !cancellationToken.IsCancellationRequested;
    }
}