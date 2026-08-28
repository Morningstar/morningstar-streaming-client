using Morningstar.Streaming.Domain.Models;

namespace Morningstar.Streaming.Client.Services.Subscriptions;

public interface ISubscriptionGroupManager
{
    bool TryAdd(SubscriptionGroup sub);
    SubscriptionGroup Get(Guid guid);
    bool TryRemove(Guid guid, out SubscriptionGroup? sub);
    List<SubscriptionGroup> Get();
}
