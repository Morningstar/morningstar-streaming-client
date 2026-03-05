namespace Morningstar.Streaming.Client.Services.Telemetry;

public class AtomicLong : IMetric
{
    private long currentValue;

    public long Value
    {
        get => Interlocked.Read(ref currentValue);
        set => Interlocked.Exchange(ref currentValue, value);
    }

    public long Increment() => Interlocked.Increment(ref currentValue);

    public long Exchange(long newValue) => Interlocked.Exchange(ref currentValue, newValue);
}