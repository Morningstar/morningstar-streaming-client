using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services.Counter;
using Morningstar.Streaming.Client.Services.Telemetry;

namespace Morningstar.Streaming.Client.Services.WebSockets;

public class WebSocketConsumerFactory : IWebSocketConsumerFactory
{
    private readonly ILogger<WebSocketConsumer> logger;
    private readonly ICounterLogger counterLogger;
    private readonly IWebSocketLoggerFactory wsLoggerFactory;
    private readonly IStreamingApiClient client;
    private readonly IObservableMetric<IMetric> observableMetric;

    public WebSocketConsumerFactory
    (
        ILogger<WebSocketConsumer> logger,
        ICounterLogger counterLogger,
        IWebSocketLoggerFactory wsLoggerFactory,
        IStreamingApiClient client,
        IObservableMetric<IMetric> observableMetric
    )
    {
        this.logger = logger;
        this.counterLogger = counterLogger;
        this.wsLoggerFactory = wsLoggerFactory;
        this.client = client;
        this.observableMetric = observableMetric;
    }

    public IWebSocketConsumer Create(string wsUrl, bool logToFile)
    {
        return new WebSocketConsumer(counterLogger, wsLoggerFactory, logger, client, observableMetric, wsUrl, logToFile);
    }
}