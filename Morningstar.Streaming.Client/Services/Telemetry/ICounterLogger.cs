namespace Morningstar.Streaming.Client.Services.Telemetry;

public interface ICounterLogger
{
    void RegisterSubscription(Guid subscriptionId, Guid userId, string serializationFormat, string? purpose);
    void UnregisterSubscription(Guid subscriptionId);
    void Increment(Guid subscriptionId);
    void Flush();
}
