using System;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Services.Telemetry;

namespace Morningstar.Streaming.Client.Sample.Services.Telemetry;

public class CounterLogger : ICounterLogger
{
    private long globalCounter = 0;
    internal readonly ConcurrentDictionary<Guid, CounterEntry> SubscriptionCounters = new();
    private readonly ILogger<CounterLogger> logger;

    private static readonly Meter Meter = new("CounterLogger");
    private readonly Counter<long> globalCounterMetric = Meter.CreateCounter<long>("messages_global_total");
    private readonly Counter<long> subscriptionCounterMetric = Meter.CreateCounter<long>("messages_subscription_total");

    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);

    public CounterLogger(ILogger<CounterLogger> logger)
    {
        this.logger = logger;
    }

    public void RegisterSubscription(Guid subscriptionId, Guid userId, string serializationFormat, string? purpose)
    {
        var entry = SubscriptionCounters.GetOrAdd(subscriptionId, _ => new CounterEntry());
        entry.SetMetadata(userId, serializationFormat, purpose);
    }

    public void UnregisterSubscription(Guid subscriptionId) => SubscriptionCounters.TryRemove(subscriptionId, out _);

    public void Increment(Guid subscriptionId)
    {
        Interlocked.Increment(ref globalCounter);

        var entry = SubscriptionCounters.GetOrAdd(subscriptionId, _ => new CounterEntry());
        entry.Counter.Increment();
    }

    

    public void Flush() => LogAndCleanup(null);

    internal void LogAndCleanup(object? state)
    {
        try
        {
            var global = Interlocked.Exchange(ref globalCounter, 0);
            var now = DateTime.UtcNow;

            if (global > 0)
            {
                globalCounterMetric.Add(global);
            }

            var activeSubs = SubscriptionCounters.Count;
            var avg = activeSubs > 0 ? (global / activeSubs) : 0;

            if (global > 0)
            {
                logger.LogInformation("[Throughput] Global: {GlobalCount} msg/sec | Active: {ActiveCount} | Avg/Sub: {Avg}",
                    global, activeSubs, avg);
            }

            foreach (var kvp in SubscriptionCounters.ToArray())
            {
                var id = kvp.Key;
                var entry = kvp.Value;
                var count = entry.Counter.Exchange(0);
                if (count > 0)
                {
                    entry.Touch(now);
                }

                var age = now - entry.LastUpdated;

                if (age > StaleThreshold)
                {
                    SubscriptionCounters.TryRemove(id, out _);
                    logger.LogInformation("[Counter] Removed stale subscription {SubId} (age={Age})", id, age);
                    continue;
                }

                if (count > 0)
                {
                    subscriptionCounterMetric.Add(count,
                        new KeyValuePair<string, object?>("subscription_id", id.ToString()),
                        new KeyValuePair<string, object?>("purpose", entry.Purpose),
                        new KeyValuePair<string, object?>("format", entry.SerializationFormat),
                        new KeyValuePair<string, object?>("user_id", entry.UserId));
                    logger.LogInformation("[Throughput] Subscription {SubId}: {Count} msg/sec", id, count);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in counter logger cleanup cycle");
        }
    }


    internal sealed record CounterEntry
    {
        public AtomicLong Counter { get; } = new();
        public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

        public string? Purpose { get; private set; }
        public string? SerializationFormat { get; private set; }
        public string? UserId { get; private set; }

        public void Touch(DateTime now) => LastUpdated = now;

        public void SetMetadata(Guid userId, string serializationFormat, string? purpose)
        {
            Purpose = string.IsNullOrWhiteSpace(purpose) ? Purpose : purpose;
            SerializationFormat = serializationFormat;
            UserId = userId == Guid.Empty ? UserId : userId.ToString();
        }
    }
}
