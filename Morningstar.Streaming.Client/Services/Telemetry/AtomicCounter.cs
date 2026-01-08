namespace Morningstar.Streaming.Client.Services.Telemetry;

public class AtomicCounter
{
    // use long (Int64). Interlocked operations on long are atomic on modern .NET
    private AtomicLong _count = new();

    // very cheap per-increment
    public void Increment() => Interlocked.Increment(ref _count.Value);

    // read current value (atomic read)
    public long Get() => Interlocked.Read(ref _count.Value);

    // atomically set to zero and return previous value
    public long ResetAndGet() => Interlocked.Exchange(ref _count.Value, 0);
}
