using Morningstar.Streaming.Domain.Models;
using System.Collections.Concurrent;

namespace Morningstar.Streaming.Client.Services.Subscriptions;

public class SubscriptionGroupManager : ISubscriptionGroupManager
{
    private readonly ConcurrentDictionary<Guid, SubscriptionGroup> subs;

    public SubscriptionGroupManager()
    {
        subs = new();
    }

    public bool TryAdd(SubscriptionGroup sub) => subs.TryAdd(sub.Guid, sub);
    public SubscriptionGroup Get(Guid guid) => subs.TryGetValue(guid, out var sub) ? sub : throw new InvalidOperationException($"Subscription does not exist {guid}");

    public List<SubscriptionGroup> Get() => subs.Values?.ToList() ?? new();
    public void Remove(Guid guid) => subs.TryRemove(guid, out _);
}

