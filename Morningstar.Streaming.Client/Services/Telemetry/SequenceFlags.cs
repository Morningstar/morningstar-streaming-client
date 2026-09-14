namespace Morningstar.Streaming.Client.Services.Telemetry;

/// <summary>
/// Classification of a message relative to the last-seen sequence numbers for its
/// (PerformanceId, event type) key. Flags can co-occur on a single message
/// (for example <see cref="OutOfOrder"/> together with <see cref="Duplicate"/>
/// or <see cref="Recovered"/>).
/// </summary>
[Flags]
public enum SequenceFlags
{
    None = 0,

    /// <summary>Expected next sequence, including the first sequence seen for a key.</summary>
    InOrder = 1 << 0,

    /// <summary>A sequence that has already been seen for the key.</summary>
    Duplicate = 1 << 1,

    /// <summary>Arrived below the current high-water mark (a late arrival).</summary>
    OutOfOrder = 1 << 2,

    /// <summary>This arrival revealed a forward gap; the gap size is reported separately.</summary>
    Missing = 1 << 3,

    /// <summary>An out-of-order arrival that filled a previously-missing sequence.</summary>
    Recovered = 1 << 4,

    /// <summary>A gap pruned unfilled, or an arrival older than the tracked window.</summary>
    Expired = 1 << 5,

    /// <summary>Message lacked the sequence number, PerformanceId, or event type needed to classify it.</summary>
    Unclassified = 1 << 6,
}
