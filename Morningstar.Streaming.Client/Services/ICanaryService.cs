using Morningstar.Streaming.Domain.Contracts;
using Morningstar.Streaming.Domain.Models;

namespace Morningstar.Streaming.Client.Services
{
    /// <summary>
    /// Main service interface for managing Morningstar Streaming API subscriptions.
    /// This is the primary interface that clients should use for creating and managing
    /// real-time market data subscriptions.
    /// </summary>
    public interface ICanaryService
    {
        /// <summary>
        /// Gets all currently active subscriptions.
        /// </summary>
        /// <returns>List of active subscription information</returns>
        List<SubscriptionGroupView> GetActiveSubscriptions();

        /// <summary>
        /// Starts a new Level 1 market data subscription.
        /// </summary>
        /// <param name="req">The subscription request with investment identifiers</param>
        /// <returns>Subscription response with GUID and WebSocket information</returns>
        Task<StartSubscriptionResponse> StartLevel1SubscriptionAsync(StartSubscriptionRequest req);

        /// <summary>
        /// Stops an active subscription by its GUID.
        /// </summary>
        /// <param name="guid">The unique identifier of the subscription to stop</param>
        /// <returns>True if successfully stopped, false if subscription not found</returns>
        Task<bool> StopSubscriptionAsync(Guid guid);
    }
}
