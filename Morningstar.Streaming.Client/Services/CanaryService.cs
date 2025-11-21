using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Client.Services.WebSockets;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Contracts;
using Morningstar.Streaming.Domain.Models;
using System.Net;

namespace Morningstar.Streaming.Client.Services
{
    /// <summary>
    /// Base implementation of the Canary service for managing Morningstar Streaming API subscriptions.
    /// </summary>
    public class CanaryService : ICanaryService
    {
        protected readonly ISubscriptionGroupManager subscriptionManager;
        protected readonly IStreamSubscriptionFactory streamSubscriptionFactory;
        protected readonly IWebSocketConsumerFactory factory;
        protected readonly ILogger logger;
        protected readonly bool logMessages;

        public CanaryService(
            ISubscriptionGroupManager subscriptionManager,
            IStreamSubscriptionFactory streamSubscriptionFactory,
            IWebSocketConsumerFactory factory,
            ILogger<CanaryService> logger,
            IOptions<AppConfig> appConfig)
        {
            this.subscriptionManager = subscriptionManager;
            this.streamSubscriptionFactory = streamSubscriptionFactory;
            this.factory = factory;
            this.logger = logger;
            logMessages = appConfig.Value.LogMessages;
        }

        public Task<StartSubscriptionResponse> StartLevel1SubscriptionAsync(StartSubscriptionRequest req)
            => StartLevel1SubscriptionInternalAsync(req, streamSubscriptionFactory.CreateAsync);

        /// <summary>
        /// Protected method that handles the core subscription logic.
        /// This can be called by derived classes to implement additional subscription methods.
        /// </summary>
        protected virtual async Task<StartSubscriptionResponse> StartLevel1SubscriptionInternalAsync<TRequest>(
            TRequest req,
            Func<TRequest, Task<StreamSubscriptionResult>> createFunc)
            where TRequest : SubscriptionBaseRequest
        {
            var streamResult = await createFunc(req);

            if (streamResult.ApiResponse.StatusCode != HttpStatusCode.OK && streamResult.ApiResponse.StatusCode != HttpStatusCode.PartialContent)
            {
                return new StartSubscriptionResponse
                {
                    ApiResponse = streamResult.ApiResponse
                };
            }

            var sub = new SubscriptionGroup
            {
                Guid = Guid.NewGuid(),
                WebSocketUrls = streamResult.WebSocketUrls,
                StartedAt = DateTime.UtcNow,
                ExpiresAt = req.DurationSeconds.HasValue ? DateTime.UtcNow.AddSeconds(req.DurationSeconds.Value) : null,
                CancellationTokenSource = streamResult.CancellationTokenSource,
            };

            subscriptionManager.TryAdd(sub);

            foreach (var url in streamResult.WebSocketUrls)
            {
                _ = Task.Run(() => ConsumeWebSocketStreamAsync(url, sub, logMessages, sub.CancellationTokenSource.Token));
            }

            return new StartSubscriptionResponse
            {
                SubscriptionGuid = sub.Guid,
                StartedAt = sub.StartedAt,
                ExpiresAt = sub.ExpiresAt,
                ApiResponse = streamResult.ApiResponse
            };
        }

        public Task<bool> StopSubscriptionAsync(Guid guid)
        {
            var sub = subscriptionManager.Get(guid);
            if (sub == null) return Task.FromResult(false);
            sub.CancellationTokenSource.Cancel();
            subscriptionManager.Remove(guid);
            return Task.FromResult(true);
        }

        public List<SubscriptionGroupView> GetActiveSubscriptions()
        {
            return subscriptionManager.Get().Select(s => new SubscriptionGroupView
            {
                ExpiresAt = s.ExpiresAt,
                Guid = s.Guid,
                StartedAt = s.StartedAt,
                WebSocketUrls = s.WebSocketUrls
            }).ToList();
        }

        /// <summary>
        /// Protected method for consuming WebSocket streams.
        /// Can be used by derived classes for custom subscription implementations.
        /// </summary>
        protected virtual async Task ConsumeWebSocketStreamAsync(string wsUrl, SubscriptionGroup sub, bool logToFile, CancellationToken token)
        {
            var consumer = factory.Create(wsUrl, logToFile);
            await consumer.StartConsumingAsync(token);
        }
    }
}
