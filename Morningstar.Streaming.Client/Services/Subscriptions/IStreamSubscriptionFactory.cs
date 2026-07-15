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

    /// <summary>
    /// Creates a Level 2 stream subscription using the Level 2 endpoint.
    /// </summary>
    Task<StreamSubscriptionResult> CreateLevel2Async(StartSubscriptionRequest req);
}
