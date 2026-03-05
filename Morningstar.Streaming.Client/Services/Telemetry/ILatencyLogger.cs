namespace Morningstar.Streaming.Client.Services.Telemetry;

public interface ILatencyLogger
{
    void RegisterSubscription(Guid subscriptionId, string serializationFormat, string? purpose);
    void UnregisterSubscription(Guid subscriptionId);
    void RecordLatency(Guid subscriptionId, long millis);
    void Flush();
}
