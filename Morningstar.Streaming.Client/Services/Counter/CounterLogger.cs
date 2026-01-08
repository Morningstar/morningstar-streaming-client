using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Services.Telemetry;
using System.Collections.Concurrent;

namespace Morningstar.Streaming.Client.Services.Counter;

public class CounterLogger : ICounterLogger, IHostedService, IDisposable
{
    private long globalCounter = 0;
    internal readonly ConcurrentDictionary<Guid, AtomicLong> SubscriptionCounters = new();
    private Timer? timer;
    private readonly ILogger<CounterLogger> logger;

    public CounterLogger(ILogger<CounterLogger> logger)
    {
        this.logger = logger;
    }

    public void RegisterSubscription(Guid subscriptionId)
    {
        SubscriptionCounters.TryAdd(subscriptionId, new AtomicLong());
    }

    public void UnregisterSubscription(Guid subscriptionId)
    {
        SubscriptionCounters.TryRemove(subscriptionId, out _);
    }

    public void Increment(Guid subscriptionId)
    {
        Interlocked.Increment(ref globalCounter);

        if (SubscriptionCounters.TryGetValue(subscriptionId, out var counter))
        {
            Interlocked.Increment(ref counter.Value);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = new Timer(LogCounts, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }

    private void LogCounts(object? state)
    {
        var global = Interlocked.Exchange(ref globalCounter, 0);
        if (global == 0)
        {
            return;
        }
        var activeSubs = SubscriptionCounters.Count;
        var avg = activeSubs > 0 ? (global / activeSubs) : 0;

        logger.LogInformation("[Throughput] Global: {GlobalCount} msg/sec | Active: {ActiveCount} | Avg/Sub: {Avg}",
            global, activeSubs, avg);

        foreach (var kvp in SubscriptionCounters)
        {
            var count = Interlocked.Exchange(ref kvp.Value.Value, 0);
            if (count > 0)
            {
                logger.LogInformation("[Throughput] Subscription {SubscriptionId}: {Count} msg/sec",
                    kvp.Key, count);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() => timer?.Dispose();
}