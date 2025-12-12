namespace Morningstar.Streaming.Client.Services.Counter;

public interface ICounterLogger
{
    void RegisterSubscription(Guid subscriptionId);
    void UnregisterSubscription(Guid subscriptionId);
    void Increment(Guid subscriptionId);
}
