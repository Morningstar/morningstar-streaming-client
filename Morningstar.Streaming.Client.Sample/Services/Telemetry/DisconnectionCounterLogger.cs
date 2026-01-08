using Morningstar.Streaming.Client.Services.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Morningstar.Streaming.Client.Sample.Services.Telemetry;

public class DisconnectionCounterLogger : IObservableMetric<IMetric>, IHostedService, IDisposable
{
    private readonly AtomicCounter disconnectionCounter = new();
    private Timer? timer;
    private readonly ILogger<DisconnectionCounterLogger> logger;
    private bool disposed;

    public DisconnectionCounterLogger(ILogger<DisconnectionCounterLogger> logger)
    {
        this.logger = logger;
    }
    public async Task RecordMetric(string name, IMetric value, IDictionary<string, string>? tags = null)
    {
        if (name != MetricEvents.WebSocketDisconnections)
        {
            return;
        }

        disconnectionCounter.Increment();
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
            var count = disconnectionCounter.ResetAndGet();

            if (count > 0)
            {
                logger.LogInformation("[Counter] Total Disconnections: {Count}", count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in disconnection counter logger cleanup cycle");
        }
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
