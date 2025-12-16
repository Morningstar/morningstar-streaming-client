using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services.Counter;

namespace Morningstar.Streaming.Client.Services.WebSockets
{
    public class WebSocketConsumerFactory : IWebSocketConsumerFactory
    {
        private readonly ILogger<WebSocketConsumer> logger;
        private readonly ICounterLogger counterLogger;
        private readonly IWebSocketLoggerFactory wsLoggerFactory;
        private readonly IStreamingApiClient client;
        private readonly string streamingFormat;

        public WebSocketConsumerFactory
        (
            ILogger<WebSocketConsumer> logger,
            ICounterLogger counterLogger,
            IWebSocketLoggerFactory wsLoggerFactory,
            IStreamingApiClient client
        )
        {
            this.logger = logger;
            this.counterLogger = counterLogger;
            this.wsLoggerFactory = wsLoggerFactory;
            this.client = client;
        }

        public IWebSocketConsumer Create(string wsUrl, bool logToFile)
        {
            return new WebSocketConsumer(counterLogger, wsLoggerFactory, logger, client, wsUrl, logToFile);
        }
    }
}
