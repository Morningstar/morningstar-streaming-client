namespace Morningstar.Streaming.Client.Services.Telemetry;

/// <summary>
/// Detects out-of-order, duplicate, and missing message sequence numbers per
/// (PerformanceId, event type) and reports classified results to an <see cref="ISequenceLogger"/>.
///
/// <para>
/// State is a windowed high-water mark plus the set of outstanding gaps. A sequence within
/// the window that is not an outstanding gap is treated as already seen (a duplicate). Only the
/// most recent <c>window</c> sequences below the high-water mark are retained; older gaps are
/// pruned and reported as <see cref="SequenceFlags.Expired"/>.
/// </para>
///
/// <para>
/// Thread-safe: internally synchronized so the same instance can be shared across two physical
/// connections during an arbitration overlap window (the retiring and incoming connections each
/// drive their own telemetry loop concurrently). Contention is expected to be rare and brief
/// (only during handovers), so a simple lock is used rather than lock-free structures.
/// </para>
/// </summary>
public class SequenceGapDetector
{
    internal const int DefaultWindow = 1000;
    internal static readonly long DefaultStaleThresholdMs = (long)TimeSpan.FromMinutes(5).TotalMilliseconds;

    private readonly Guid subscriptionId;
    private readonly ISequenceLogger logger;
    private readonly int window;
    private readonly long staleThresholdMs;
    private readonly Dictionary<SequenceKey, KeyState> states = new();
    private readonly object gate = new();

    // Reused across AdvanceFloor calls to avoid per-call closure allocation (safe: single-threaded).
    private long pruneFloor;
    private readonly Predicate<long> isAtOrBelowPruneFloor;

    public SequenceGapDetector(
        Guid subscriptionId,
        ISequenceLogger logger,
        int window = DefaultWindow,
        long? staleThresholdMs = null)
    {
        this.subscriptionId = subscriptionId;
        this.logger = logger;
        this.window = window < 1 ? 1 : window;
        this.staleThresholdMs = staleThresholdMs ?? DefaultStaleThresholdMs;
        isAtOrBelowPruneFloor = g => g <= pruneFloor;
    }

    /// <summary>
    /// Classifies a single message and reports the result to the logger. Messages missing the
    /// PerformanceId, event type, or sequence number are reported as <see cref="SequenceFlags.Unclassified"/>.
    /// </summary>
    public void Process(string? performanceId, string? eventType, long? sequenceNumber)
    {
        lock (gate)
        {
            ProcessCore(performanceId, eventType, sequenceNumber);
        }
    }

    private void ProcessCore(string? performanceId, string? eventType, long? sequenceNumber)
    {
        if (string.IsNullOrEmpty(performanceId) || string.IsNullOrEmpty(eventType) || sequenceNumber is null)
        {
            logger.Record(subscriptionId, SequenceFlags.Unclassified, 0);
            return;
        }

        var seq = sequenceNumber.Value;
        var key = new SequenceKey(performanceId, eventType);

        // Ensure we have a state object for this key.
        if (!states.TryGetValue(key, out var state))
        {
            states[key] = new KeyState(seq);
            logger.Record(subscriptionId, SequenceFlags.InOrder, 0);
            return;
        }

        state.Touch();
        var hwm = state.HighWaterMark;

        if (seq == hwm + 1)
        {
            // Normal advance.
            state.HighWaterMark = seq;
            AdvanceFloor(state, seq - window);
            logger.Record(subscriptionId, SequenceFlags.InOrder, 0);
        }
        else if (seq > hwm + 1)
        {
            // Forward gap: report the full gap as missing, but only retain the in-window tail for recovery.
            var gapStart = hwm + 1;
            var gapEnd = seq - 1;
            var missingCount = gapEnd - gapStart + 1;

            AdvanceFloor(state, seq - window);

            var addFrom = Math.Max(gapStart, state.Floor + 1);
            for (var g = addFrom; g <= gapEnd; g++)
            {
                state.Missing.Add(g);
            }

            state.HighWaterMark = seq;
            logger.Record(subscriptionId, SequenceFlags.Missing, missingCount);
        }
        else if (seq == hwm)
        {
            logger.Record(subscriptionId, SequenceFlags.Duplicate, 0);
        }
        else
        {
            // seq < hwm: late arrival.
            var flags = SequenceFlags.OutOfOrder;
            if (seq <= state.Floor)
            {
                flags |= SequenceFlags.Expired;
            }
            else if (state.Missing.Remove(seq))
            {
                flags |= SequenceFlags.Recovered;
            }
            else
            {
                flags |= SequenceFlags.Duplicate;
            }

            logger.Record(subscriptionId, flags, 0);
        }
    }

    /// <summary>Evicts keys idle beyond the stale threshold, flushing any outstanding gaps as expired.</summary>
    public void Prune() => Prune(Environment.TickCount64);

    internal void Prune(long nowTick)
    {
        lock (gate)
        {
            PruneCore(nowTick);
        }
    }

    private void PruneCore(long nowTick)
    {
        if (states.Count == 0)
        {
            return;
        }

        List<SequenceKey>? stale = null;
        foreach (var kvp in states)
        {
            if (nowTick - kvp.Value.LastTouchedTick <= staleThresholdMs)
            {
                continue;
            }

            (stale ??= new List<SequenceKey>()).Add(kvp.Key);
        }

        if (stale is null)
        {
            return;
        }

        foreach (var key in stale)
        {
            var state = states[key];
            if (state.Missing.Count > 0)
            {
                logger.Record(subscriptionId, SequenceFlags.Expired, state.Missing.Count);
            }

            states.Remove(key);
        }
    }

    /// <summary>Drops all tracked state (used on subscription teardown).</summary>
    public void Clear()
    {
        lock (gate)
        {
            states.Clear();
        }
    }

    internal int KeyCount
    {
        get
        {
            lock (gate)
            {
                return states.Count;
            }
        }
    }

    private void AdvanceFloor(KeyState state, long newFloor)
    {
        if (newFloor <= state.Floor)
        {
            return;
        }

        state.Floor = newFloor;

        if (state.Missing.Count == 0)
        {
            return;
        }

        pruneFloor = newFloor;
        var expired = state.Missing.RemoveWhere(isAtOrBelowPruneFloor);
        if (expired > 0)
        {
            logger.Record(subscriptionId, SequenceFlags.Expired, expired);
        }
    }

    private readonly record struct SequenceKey(string PerformanceId, string EventType);

    private sealed class KeyState
    {
        public long HighWaterMark;
        public long Floor;
        public readonly HashSet<long> Missing = new();
        public long LastTouchedTick = Environment.TickCount64;

        public KeyState(long seq)
        {
            HighWaterMark = seq;
            Floor = seq - 1;
        }

        public void Touch() => LastTouchedTick = Environment.TickCount64;
    }
}
