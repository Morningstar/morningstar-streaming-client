using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Services.Telemetry;

namespace Morningstar.Streaming.Client.Sample.Services.Telemetry;

/// <summary>
/// Sample <see cref="ISequenceLogger"/> implementation demonstrating how to consume the client's
/// sequence-integrity classifications. Sequence detection (out-of-order, duplicate, missing,
/// recovered, expired) is performed inside the client; this logger simply aggregates the classified
/// results per subscription and, on each <see cref="Flush"/> (driven by the client's telemetry loop),
/// emits counter metrics and a log line. Mirrors the structure of
/// <see cref="CounterLogger"/> and <see cref="LatencyLogger"/>.
/// </summary>
public class SequenceLogger : ISequenceLogger
{
    internal readonly ConcurrentDictionary<Guid, SequenceEntry> Subscriptions = new();
    private readonly ILogger<SequenceLogger> logger;

    private static readonly Meter Meter = new("SequenceLogger");
    private readonly Counter<long> outOfOrderMetric = Meter.CreateCounter<long>("messages_out_of_order_total");
    private readonly Counter<long> duplicateMetric = Meter.CreateCounter<long>("messages_duplicate_total");
    private readonly Counter<long> missingMetric = Meter.CreateCounter<long>("messages_missing_total");
    private readonly Counter<long> recoveredMetric = Meter.CreateCounter<long>("messages_recovered_total");
    private readonly Counter<long> expiredMetric = Meter.CreateCounter<long>("messages_missing_expired_total");
    private readonly Counter<long> unclassifiedMetric = Meter.CreateCounter<long>("messages_unclassified_total");

    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);

    public SequenceLogger(ILogger<SequenceLogger> logger)
    {
        this.logger = logger;
    }

    public void RegisterSubscription(Guid subscriptionId, string serializationFormat, string? purpose)
    {
        var entry = Subscriptions.GetOrAdd(subscriptionId, _ => new SequenceEntry());
        entry.SetMetadata(serializationFormat, purpose);
        entry.Touch(DateTime.UtcNow);
    }

    public void UnregisterSubscription(Guid subscriptionId) => Subscriptions.TryRemove(subscriptionId, out _);

    public void Record(Guid subscriptionId, SequenceFlags flags, long missingCount)
    {
        var entry = Subscriptions.GetOrAdd(subscriptionId, _ => new SequenceEntry());
        entry.Apply(flags, missingCount);
        entry.Touch(DateTime.UtcNow);
    }

    public void Flush() => LogAndCleanup(null);

    internal void LogAndCleanup(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;

            foreach (var kvp in Subscriptions.ToArray())
            {
                var id = kvp.Key;
                var entry = kvp.Value;

                var (outOfOrder, duplicate, missing, recovered, expired, unclassified) = entry.ExchangeCounts();

                var hasAny = outOfOrder > 0 || duplicate > 0 || missing > 0
                    || recovered > 0 || expired > 0 || unclassified > 0;

                if (hasAny)
                {
                    entry.Touch(now);
                }

                var age = now - entry.LastUpdated;
                if (age > StaleThreshold)
                {
                    Subscriptions.TryRemove(id, out _);
                    logger.LogInformation("[Sequence] Removed stale subscription {SubId} (age={Age})", id, age);
                    continue;
                }

                if (!hasAny)
                {
                    continue;
                }

                TagList tags = new();
                tags.Add("subscription_id", id.ToString());
                tags.Add("purpose", entry.Purpose);
                tags.Add("format", entry.SerializationFormat);

                Emit(outOfOrderMetric, outOfOrder, tags);
                Emit(duplicateMetric, duplicate, tags);
                Emit(missingMetric, missing, tags);
                Emit(recoveredMetric, recovered, tags);
                Emit(expiredMetric, expired, tags);
                Emit(unclassifiedMetric, unclassified, tags);

                logger.LogInformation(
                    "[Sequence] Subscription {SubId}: out_of_order={Ooo} duplicate={Dup} missing={Missing} recovered={Rec} expired={Exp} unclassified={Unc}",
                    id, outOfOrder, duplicate, missing, recovered, expired, unclassified);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in sequence logger cleanup cycle");
        }
    }

    private static void Emit(Counter<long> metric, long value, in TagList tags)
    {
        if (value > 0)
        {
            metric.Add(value, tags);
        }
    }

    internal sealed class SequenceEntry
    {
        private long outOfOrder;
        private long duplicate;
        private long missing;
        private long recovered;
        private long expired;
        private long unclassified;

        public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;
        public string? Purpose { get; private set; }
        public string? SerializationFormat { get; private set; }

        public void Apply(SequenceFlags flags, long missingCount)
        {
            if ((flags & SequenceFlags.OutOfOrder) != 0)
            {
                Interlocked.Increment(ref outOfOrder);
            }

            if ((flags & SequenceFlags.Duplicate) != 0)
            {
                Interlocked.Increment(ref duplicate);
            }

            if ((flags & SequenceFlags.Missing) != 0 && missingCount > 0)
            {
                Interlocked.Add(ref missing, missingCount);
            }

            if ((flags & SequenceFlags.Recovered) != 0)
            {
                Interlocked.Increment(ref recovered);
            }

            if ((flags & SequenceFlags.Expired) != 0 && missingCount > 0)
            {
                Interlocked.Add(ref expired, missingCount);
            }

            if ((flags & SequenceFlags.Unclassified) != 0)
            {
                Interlocked.Increment(ref unclassified);
            }
        }

        public (long OutOfOrder, long Duplicate, long Missing, long Recovered, long Expired, long Unclassified) ExchangeCounts() => (
            Interlocked.Exchange(ref outOfOrder, 0),
            Interlocked.Exchange(ref duplicate, 0),
            Interlocked.Exchange(ref missing, 0),
            Interlocked.Exchange(ref recovered, 0),
            Interlocked.Exchange(ref expired, 0),
            Interlocked.Exchange(ref unclassified, 0));

        public void Touch(DateTime now) => LastUpdated = now;

        public void SetMetadata(string serializationFormat, string? purpose)
        {
            Purpose = string.IsNullOrWhiteSpace(purpose) ? Purpose : purpose;
            SerializationFormat = serializationFormat;
        }
    }
}
