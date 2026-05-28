using Morningstar.Streaming.Client.Services.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Morningstar.Streaming.Client.Sample.Services.Telemetry;

public class WebSocketLifecycleMetricLogger : IObservableMetric<IMetric>, IHostedService, IDisposable
{
    private readonly AtomicCounter disconnectionCounter = new();
    private readonly AtomicCounter reconnectionCounter = new();
    private Timer? timer;
    private readonly ILogger<WebSocketLifecycleMetricLogger> logger;
    private bool disposed;

    public WebSocketLifecycleMetricLogger(ILogger<WebSocketLifecycleMetricLogger> logger)
    {
        this.logger = logger;
    }
    public Task RecordMetric(string name, IMetric value, IDictionary<string, string>? tags = null)
    {
        if (name == MetricEvents.WebSocketDisconnections)
        {
            disconnectionCounter.Increment();
            LogMetric(name, tags);
            return Task.CompletedTask;
        }

        if (name == MetricEvents.WebSocketReconnections)
        {
            reconnectionCounter.Increment();
            LogMetric(name, tags);
        }

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = new Timer(LogAndCleanup, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }

    internal void LogAndCleanup(object? state)
    {
        try
        {
            var disconnectionCount = disconnectionCounter.ResetAndGet();
            var reconnectionCount = reconnectionCounter.ResetAndGet();

            if (disconnectionCount > 0)
            {
                logger.LogInformation("[Counter] Total Disconnections: {Count}", disconnectionCount);
            }

            if (reconnectionCount > 0)
            {
                logger.LogInformation("[Counter] Total Reconnections: {Count}", reconnectionCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in disconnection counter logger cleanup cycle");
        }
    }

    private void LogMetric(string name, IDictionary<string, string>? tags)
    {
        var disconnectTypeTagName = name == MetricEvents.WebSocketReconnections
            ? "PreviousDisconnectType"
            : "DisconnectType";

        logger.LogInformation(
            "Observed {MetricName}. SubscriptionId: {SubscriptionId}. TopicGuid: {TopicGuid}. {DisconnectTypeTagName}: {DisconnectType}. WebSocketUrl: {WebSocketUrl}",
            name,
            GetTagValue(tags, "SubscriptionId"),
            GetTagValue(tags, "TopicGuid"),
            disconnectTypeTagName,
            GetTagValue(tags, disconnectTypeTagName),
            GetTagValue(tags, "WebSocketUrl"));
    }

    private static string GetTagValue(IDictionary<string, string>? tags, string key)
    {
        return tags != null && tags.TryGetValue(key, out var value) ? value : "n/a";
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer?.Dispose();
        return Task.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                timer?.Dispose();
            }
            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
