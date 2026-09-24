using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Domain.Config;

namespace Morningstar.Streaming.Client.Services.WebSockets
{
    public class WebSocketConsumerFactory : IWebSocketConsumerFactory
    {
        private readonly ILogger<WebSocketConsumer> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IWebSocketLoggerFactory wsLoggerFactory;
        private readonly IStreamingApiClient client;
        private readonly IObservableMetric<IMetric>? observableMetric;
        private readonly int defaultArbitrationRetirementMinutes;

        public WebSocketConsumerFactory
        (
            ILogger<WebSocketConsumer> logger,
            IServiceProvider serviceProvider,
            IWebSocketLoggerFactory wsLoggerFactory,
            IStreamingApiClient client,
            IObservableMetric<IMetric>? observableMetric,
            IOptions<AppConfig>? appConfig = null
        )
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.wsLoggerFactory = wsLoggerFactory;
            this.client = client;
            this.observableMetric = observableMetric;
            defaultArbitrationRetirementMinutes = appConfig?.Value.DefaultArbitrationRetirementMinutes ?? 5;
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
            defaultArbitrationRetirementMinutes = 5;
        }

        public IWebSocketConsumer Create(string wsUrl, bool logToFile, string? purpose)
        {
            var counterLogger = serviceProvider.GetService<ICounterLogger>();
            var latencyLogger = serviceProvider.GetService<ILatencyLogger>();
            var sequenceLogger = serviceProvider.GetService<ISequenceLogger>();

            return new WebSocketConsumer(counterLogger, latencyLogger, wsLoggerFactory, logger, client, observableMetric, wsUrl, logToFile, purpose, sequenceLogger, defaultArbitrationRetirementMinutes);
        }
    }
}