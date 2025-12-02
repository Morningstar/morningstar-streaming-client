using Morningstar.Streaming.Domain.Contracts;

namespace Morningstar.Streaming.Client.Services.Subscriptions;

/// <summary>
/// Factory interface for creating stream subscriptions.
/// </summary>
public interface IStreamSubscriptionFactory
{
    /// <summary>
    /// Creates a stream subscription using the standard endpoint.
    /// </summary>
    Task<StreamSubscriptionResult> CreateAsync(StartSubscriptionRequest req);
}
