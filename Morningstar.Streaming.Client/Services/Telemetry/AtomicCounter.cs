namespace Morningstar.Streaming.Client.Services.Telemetry;

public class AtomicCounter
{
    // use long (Int64). Interlocked operations on long are atomic on modern .NET
    private readonly AtomicLong _count = new();

    // very cheap per-increment
    public void Increment() => _count.Increment();

    // read current value (atomic read)
    public long Get() => _count.Value;

    // atomically set to zero and return previous value
    public long ResetAndGet() => _count.Exchange(0);
}
