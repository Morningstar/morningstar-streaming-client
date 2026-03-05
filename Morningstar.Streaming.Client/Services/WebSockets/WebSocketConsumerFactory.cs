using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Clients;
using Microsoft.Extensions.DependencyInjection;
using Morningstar.Streaming.Client.Services.Telemetry;

namespace Morningstar.Streaming.Client.Services.WebSockets
{
    public class WebSocketConsumerFactory : IWebSocketConsumerFactory
    {
        private readonly ILogger<WebSocketConsumer> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IWebSocketLoggerFactory wsLoggerFactory;
        private readonly IStreamingApiClient client;
        private readonly IObservableMetric<IMetric>? observableMetric;

        public WebSocketConsumerFactory
        (
            ILogger<WebSocketConsumer> logger,
            IServiceProvider serviceProvider,
            IWebSocketLoggerFactory wsLoggerFactory,
            IStreamingApiClient client,
            IObservableMetric<IMetric>? observableMetric
        )
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.wsLoggerFactory = wsLoggerFactory;
            this.client = client;
            this.observableMetric = observableMetric;
        }

        public WebSocketConsumerFactory
        (
            ILogger<WebSocketConsumer> logger,
            IServiceProvider serviceProvider,
            IWebSocketLoggerFactory wsLoggerFactory,
            IStreamingApiClient client
        )
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.wsLoggerFactory = wsLoggerFactory;
            this.client = client;
        }

        public IWebSocketConsumer Create(string wsUrl, bool logToFile, string? purpose)
        {
            var counterLogger = serviceProvider.GetService<ICounterLogger>();
            var latencyLogger = serviceProvider.GetService<ILatencyLogger>();

            return new WebSocketConsumer(counterLogger, latencyLogger, wsLoggerFactory, logger, client, observableMetric, wsUrl, logToFile, purpose);
        }
    }
}