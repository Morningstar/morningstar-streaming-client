using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services.Channels;
using Morningstar.Streaming.Client.Services.Counter;

namespace Morningstar.Streaming.Client.Services.WebSockets
{
    public class WebSocketConsumer : IWebSocketConsumer
    {
        private readonly ILogger<WebSocketConsumer> logger;
        private readonly IStreamingApiClient client;
        private readonly string wsUrl;
        private readonly bool logToFile;

        private readonly ICounterLogger counterLogger;
        private readonly ILogger eventsLogger;
        private readonly CountingChannel<string> channel;
        private readonly Guid topicGuid;

        public WebSocketConsumer
        (
            ICounterLogger counterLogger,
            IWebSocketLoggerFactory wsLoggerFactory,
            ILogger<WebSocketConsumer> logger,
            IStreamingApiClient client,
            string wsUrl,
            bool logToFile
        )
        {
            this.logger = logger;
            this.client = client;
            this.wsUrl = wsUrl;
            this.logToFile = logToFile;
            this.counterLogger = counterLogger;
            channel = new();

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
                        if (!channel.TryWrite(message))
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

                // If we reach here due to cancellation, that's expected
                // If subTask completed unexpectedly, we should know about it
                if (subTask.IsCompleted && !subTask.IsFaulted && !cancellationToken.IsCancellationRequested)
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
                logger.LogError(ex, "WebSocket consumer failed unexpectedly.");
                throw;
            }
            finally
            {
                channel.Chnl.Writer.Complete();
                counterLogger.UnregisterSubscription(topicGuid);
            }
        }

        private async Task LogFromChannelAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var message in channel.Chnl.Reader.ReadAllAsync(cancellationToken))
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
    }
}
