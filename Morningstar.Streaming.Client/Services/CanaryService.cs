using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Client.Services.WebSockets;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Constants;
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

            var consumerTasks = new List<Task>();
            foreach (var url in streamResult.WebSocketUrls)
            {
                var wsUrl = $"{url}/{req.StreamingFormat}";
                var task = Task.Run(() => ConsumeWebSocketStreamAsync(wsUrl, sub, logMessages, sub.CancellationTokenSource.Token));
                consumerTasks.Add(task);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(consumerTasks);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "One or more WebSocket consumers failed for subscription {SubscriptionGuid}", sub.Guid);
                }
                finally
                {
                    subscriptionManager.Remove(sub.Guid);
                    logger.LogInformation("Subscription {SubscriptionGuid} removed from manager after all consumers completed", sub.Guid);
                }
            });

            return new StartSubscriptionResponse
            {
                SubscriptionGuid = sub.Guid,
                StartedAt = sub.StartedAt,
                ExpiresAt = sub.ExpiresAt,
                ApiResponse = streamResult.ApiResponse
            };
        }

        public Task<StopSubscriptionResponse> StopSubscriptionAsync(Guid guid)
        {
            try
            {
                var sub = subscriptionManager.Get(guid);
                sub.CancellationTokenSource.Cancel();
                return Task.FromResult(new StopSubscriptionResponse
                {
                    Success = true,
                    SubscriptionGuid = guid,
                    Message = "Subscription stopped successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Attempted to stop non-existent subscription {SubscriptionGuid}", guid);
                return Task.FromResult(new StopSubscriptionResponse
                {
                    Success = false,
                    SubscriptionGuid = guid,
                    ErrorCode = ErrorCodes.SubscriptionNotFound,
                    Message = $"Subscription with ID {guid} was not found or has already been removed"
                });
            }
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
