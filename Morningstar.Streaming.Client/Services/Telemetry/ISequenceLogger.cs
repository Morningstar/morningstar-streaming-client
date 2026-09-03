namespace Morningstar.Streaming.Client.Services.Telemetry;

/// <summary>
/// Optional telemetry hook for tracking message sequence integrity per
/// (PerformanceId, event type). Sequence classification (out-of-order,
/// duplicate, missing) is performed inside the client; implementations of this
/// interface aggregate the classified results and publish metrics.
/// Register your own implementation via dependency injection to enable it.
/// </summary>
public interface ISequenceLogger
{
    void RegisterSubscription(Guid subscriptionId, string serializationFormat, string? purpose);

    void UnregisterSubscription(Guid subscriptionId);

    /// <summary>
    /// Records the classification of a single message (or, for <see cref="SequenceFlags.Expired"/>,
    /// a pruning event). <paramref name="missingCount"/> is the number of newly-detected missing
    /// sequences this event revealed; it is 0 unless <see cref="SequenceFlags.Missing"/> or
    /// <see cref="SequenceFlags.Expired"/> is set.
    /// </summary>
    void Record(Guid subscriptionId, SequenceFlags flags, long missingCount);

    void Flush();
}
