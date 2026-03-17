using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Services.Telemetry;

namespace Morningstar.Streaming.Client.Sample.Services.Telemetry;

public class LatencyLogger : ILatencyLogger
{
   private readonly int maxLatencyMs = 15_000;
    private readonly int bucketSizeMs = 1;
    private const int OverflowBucketCount = 63;
    private readonly int bucketCount;
    private readonly long[] globalHistogram;
    private const string ClientSampleComponent = "ClientSample";
    

    internal readonly ConcurrentDictionary<Guid, HistogramEntry> SubscriptionHistograms = new();
    internal readonly ConcurrentDictionary<string, HistogramEntry> ComponentHistograms = new();

    private Timer? timer;
    private bool disposed;
    private readonly ILogger<LatencyLogger> logger;
    private static readonly Meter Meter = new("LatencyLogger");
    private readonly Histogram<long> latencyHistogram = Meter.CreateHistogram<long>("message_latency_ms");

    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);

    public LatencyLogger(ILogger<LatencyLogger> logger)
    {
        this.logger = logger;
        bucketCount = (maxLatencyMs / bucketSizeMs) + 1 + OverflowBucketCount;
        globalHistogram = new long[bucketCount];
    }

    public void RegisterSubscription(Guid subscriptionId, string serializationFormat, string? purpose)
    {
        var entry = SubscriptionHistograms.GetOrAdd(
            subscriptionId,
            static (key, bucketCount) => new HistogramEntry(new long[bucketCount], subscriptionId: key),
            bucketCount);

        entry.SetSubscriptionMetadata(serializationFormat, purpose);
        entry.Touch();
    }

    public void UnregisterSubscription(Guid subscriptionId)
    {
        if (SubscriptionHistograms.TryRemove(subscriptionId, out var entry))
        {
            EmitInactiveSubscriptionSnapshot(subscriptionId, entry);
        }
    }

    public void RecordLatency(Guid subscriptionId, long millis) =>
        RecordComponentLatency(ClientSampleComponent, millis, subscriptionId);

    internal void RecordComponentLatency(string componentName, long millis, Guid subscriptionId)
    {
        var subscriptionEntry = SubscriptionHistograms.GetOrAdd(
            subscriptionId,
            static (key, bucketCount) => new HistogramEntry(new long[bucketCount], subscriptionId: key),
            bucketCount);

        var key = BuildComponentKey(componentName, subscriptionId);

        var state = new ComponentEntryFactoryState(
            BucketCount: bucketCount,
            SubscriptionId: subscriptionId,
            ComponentName: componentName,
            SerializationFormat: subscriptionEntry.SerializationFormat,
            Purpose: subscriptionEntry.Purpose);

        var entry = ComponentHistograms.GetOrAdd(
            key,
            static (_, s) => new HistogramEntry(
                new long[s.BucketCount],
                s.SubscriptionId,
                s.ComponentName,
                purpose: s.Purpose,
                serializationFormat: s.SerializationFormat),
            state);

        RecordToHistogram(entry.Histogram, millis);
        entry.Touch();
    }

    private static string BuildComponentKey(string componentName, Guid subscriptionId)
        => string.Concat(componentName, "|", subscriptionId.ToString());

    private readonly record struct ComponentEntryFactoryState(
        int BucketCount,
        Guid SubscriptionId,
        string ComponentName,
        string? SerializationFormat,
        string? Purpose);

    private void RecordToHistogram(long[] hist, long millis)
    {
        var safeMillis = Math.Max(0, millis);

        var scaled = safeMillis / bucketSizeMs;
        int bucket;
        if (scaled <= maxLatencyMs)
        {
            bucket = (int)scaled;
        }
        else
        {
            var overflowValue = (ulong)(scaled - maxLatencyMs); // >= 1
            var overflowIndex = BitOperations.Log2(overflowValue); // 0 => [1], 1 => [2-3], 2 => [4-7], ...
            bucket = (maxLatencyMs + 1) + Math.Min(overflowIndex, OverflowBucketCount - 1);
        }

        Interlocked.Increment(ref hist[bucket]);
    }

    private long ApproxMillisFromBucket(int bucket)
    {
        if (bucket <= maxLatencyMs)
        {
            return (long)bucket * bucketSizeMs;
        }

        var overflowIndex = bucket - (maxLatencyMs + 1);
        if (overflowIndex < 0)
        {
            return 0;
        }

        var lower = 1UL << Math.Min(overflowIndex, OverflowBucketCount - 1);
        var lowerAsLong = lower > long.MaxValue ? long.MaxValue : (long)lower;
        return (maxLatencyMs + lowerAsLong) * bucketSizeMs;
    }

    public void Flush() => LogAndCleanup();

    internal void LogAndCleanup()
    {
        try
        {
            var now = DateTime.UtcNow;

            LogHistogram("Global", globalHistogram, reset: true, CreateBaseTags(scopeType: "global", scope: "global"));

            foreach (var kvp in SubscriptionHistograms.ToArray())
            {
                var subId = kvp.Key;
                var entry = kvp.Value;
                var age = now - entry.LastUpdated;

                if (age > StaleThreshold)
                {
                    if (SubscriptionHistograms.TryRemove(subId, out var removed))
                    {
                        EmitInactiveSubscriptionSnapshot(subId, removed);
                    }
                    logger.LogInformation("[Latency] Removed stale subscription {SubId} (age={Age})", subId, age);
                    continue;
                }

                LogHistogram(
                    $"Subscription {subId}",
                    entry.Histogram,
                    reset: true,
                    CreateBaseTags(
                        scopeType: "subscription",
                        scope: "subscription",
                        subscriptionId: subId,
                        purpose: entry.Purpose,
                        serializationFormat: entry.SerializationFormat,
                        isActive: true));
            }

            foreach (var kvp in ComponentHistograms.ToArray())
            {
                var comp = kvp.Value.ComponentName ?? kvp.Key;
                var entry = kvp.Value;
                var age = now - entry.LastUpdated;

                if (age > StaleThreshold)
                {
                    ComponentHistograms.TryRemove(kvp.Key, out _);
                    logger.LogInformation(
                        "[Latency] Removed stale component {Component} (subscriptionId={SubscriptionId}, purpose={Purpose}, age={Age})",
                        comp,
                        entry.SubscriptionId,
                        entry.Purpose,
                        age);
                    continue;
                }

                var logLabel = $"Component {comp}";
                if (entry.SubscriptionId is not null)
                {
                    logLabel += $" (Subscription {entry.SubscriptionId})";
                }

                if (!string.IsNullOrWhiteSpace(entry.Purpose))
                {
                    logLabel += $" (Purpose {entry.Purpose})";
                }

                LogHistogram(
                    logLabel,
                    entry.Histogram,
                    reset: true,
                    CreateBaseTags(
                        scopeType: "component",
                        scope: comp,
                        componentName: comp,
                        subscriptionId: entry.SubscriptionId,
                        purpose: entry.Purpose,
                        serializationFormat: entry.SerializationFormat,
                        isActive: true));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in latency logger cleanup cycle");
        }
    }

    private void LogHistogram(string label, long[] hist, bool reset, TagList baseTags)
    {
        long total = 0;
        foreach (var count in hist)
        {
            total += count;
        }

        if (total == 0)
        {
            return; // skip empty samples
        }

        var medianIndex = total / 2;
        var p99Index = (long)(total * 0.99);
        long cumulative = 0;
        int median = 0, p99 = 0;

        for (var i = 0; i < hist.Length; i++)
        {
            var count = Interlocked.Exchange(ref hist[i], reset ? 0 : hist[i]);
            cumulative += count;

            if (median == 0 && cumulative >= medianIndex)
            {
                median = (int)Math.Min(int.MaxValue, ApproxMillisFromBucket(i));
            }

            if (p99 == 0 && cumulative >= p99Index)
            {
                p99 = (int)Math.Min(int.MaxValue, ApproxMillisFromBucket(i));
                break;
            }
        }

        if (median > 0 || p99 > 0)
        {
            logger.LogInformation("[Latency] {Label} Median: {Median} ms | P99: {P99} ms | Count: {Count}",
            label, median, p99, total);

            EmitLatencyMetrics(median, p99, baseTags);
        }

    }

    private static TagList CreateBaseTags(
        string scopeType,
        string scope,
        string? componentName = null,
        Guid? subscriptionId = null,
        string? purpose = null,
        string? serializationFormat = null,
        bool? isActive = null)
    {
        TagList tags = new();
        tags.Add("scope_type", scopeType);
        tags.Add("scope", scope);

        if (!string.IsNullOrWhiteSpace(componentName))
        {
            tags.Add("component", componentName);
        }

        if (subscriptionId is Guid sid)
        {
            tags.Add("subscription_id", sid.ToString());
        }

        if (!string.IsNullOrWhiteSpace(purpose))
        {
            tags.Add("purpose", purpose);
        }

        if (!string.IsNullOrWhiteSpace(serializationFormat))
        {
            tags.Add("format", serializationFormat);
        }

        if (isActive is bool active)
        {
            tags.Add("is_active", active ? "true" : "false");
        }

        return tags;
    }

    private void EmitInactiveSubscriptionSnapshot(Guid subscriptionId, HistogramEntry entry)
    {
        var baseTags = CreateBaseTags(
            scopeType: "subscription",
            scope: "subscription",
            subscriptionId: subscriptionId,
            purpose: entry.Purpose,
            serializationFormat: entry.SerializationFormat,
            isActive: false);

        var (median, p99) = ComputePercentilesSnapshot(entry.Histogram);

        EmitLatencyMetrics(median, p99, baseTags);
    }

    private void EmitLatencyMetrics(int median, int p99, TagList baseTags)
    {
        var medianTags = baseTags;
        medianTags.Add("aggregation", "summary");
        medianTags.Add("type", "median");
        latencyHistogram.Record(median, medianTags);

        var p99Tags = baseTags;
        p99Tags.Add("aggregation", "summary");
        p99Tags.Add("type", "p99");
        latencyHistogram.Record(p99, p99Tags);
    }

    private (int Median, int P99) ComputePercentilesSnapshot(long[] hist)
    {
        long total = 0;
        for (var i = 0; i < hist.Length; i++)
        {
            total += Volatile.Read(ref hist[i]);
        }

        if (total == 0)
        {
            return (0, 0);
        }

        var medianIndex = total / 2;
        var p99Index = (long)(total * 0.99);
        long cumulative = 0;
        var median = 0;
        var p99 = 0;

        for (var i = 0; i < hist.Length; i++)
        {
            cumulative += Volatile.Read(ref hist[i]);

            if (median == 0 && cumulative >= medianIndex)
            {
                median = (int)Math.Min(int.MaxValue, ApproxMillisFromBucket(i));
            }

            if (p99 == 0 && cumulative >= p99Index)
            {
                p99 = (int)Math.Min(int.MaxValue, ApproxMillisFromBucket(i));
                break;
            }
        }

        return (median, p99);
    }

    internal sealed class HistogramEntry
    {
        public long[] Histogram { get; }
        public Guid? SubscriptionId { get; }
        public string? ComponentName { get; }
        public string? Purpose { get; private set; }
        public string? SerializationFormat { get; private set; }
        public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

        public HistogramEntry(
            long[] histogram,
            Guid? subscriptionId = null,
            string? componentName = null,
            string? purpose = null,
            string? serializationFormat = null)
        {
            Histogram = histogram;
            SubscriptionId = subscriptionId;
            ComponentName = componentName;
            Purpose = purpose;
            SerializationFormat = serializationFormat;
        }

        public void SetSubscriptionMetadata(string serializationFormat, string? purpose)
        {
            if (!string.IsNullOrWhiteSpace(purpose))
            {
                Purpose = purpose;
            }

            SerializationFormat = serializationFormat;            
        }

        public void Touch() => LastUpdated = DateTime.UtcNow;
    }
}
