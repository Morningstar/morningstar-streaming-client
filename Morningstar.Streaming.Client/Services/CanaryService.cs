using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Client.Services.WebSockets;
using Morningstar.Streaming.Domain;
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

            var succeededUrls = new List<string>();
            var consumerTasks = new List<Task>();
            var consumerStartExceptions = new List<(string Url, Exception Ex)>();

            var sub = new SubscriptionGroup
            {
                Guid = Guid.NewGuid(),
                WebSocketUrls = new List<string>(),
                StartedAt = DateTime.UtcNow,
                ExpiresAt = req.DurationSeconds.HasValue ? DateTime.UtcNow.AddSeconds(req.DurationSeconds.Value) : null,
                CancellationTokenSource = streamResult.CancellationTokenSource,
                Format = req.StreamingFormat,
                Purpose = req.Purpose
            };

            foreach (var url in streamResult.WebSocketUrls)
            {
                var wsUrl = $"{url}/{req.StreamingFormat}";
                try
                {
                    var consumer = factory.Create(wsUrl, logMessages, req.Purpose);
                    var connectedTcs = new TaskCompletionSource<bool>();
                    var startTask = consumer.StartConsumingAsync(connectedTcs, sub.CancellationTokenSource.Token);
                    await connectedTcs.Task; 
                    consumerTasks.Add(startTask);
                    succeededUrls.Add(url);
                }
                catch (Exception ex)
                {
                    consumerStartExceptions.Add((url, ex));
                }
            }

            // Only keep succeeded URLs in the subscription
            sub.WebSocketUrls = succeededUrls;

            if (succeededUrls.Count == 0)
            {
                logger.LogError("Failed to start all WebSocket consumers: {Errors}", string.Join("; ", consumerStartExceptions.Select(e => $"{e.Url}: {e.Ex.Message}")));
                return new StartSubscriptionResponse
                {
                    ApiResponse = new StreamResponse
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Message = "Failed to start any WebSocket consumers."
                    }
                };
            }

            // Add to subscriptionManager if at least one succeeded
            subscriptionManager.TryAdd(sub);

            // Start background task to monitor consumers and remove subscription when done
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

            // If some failed, return partial success
            if (consumerStartExceptions.Count > 0)
            {
                logger.LogWarning("Some WebSocket consumers failed to start: {Errors}", string.Join("; ", consumerStartExceptions.Select(e => $"{e.Url}: {e.Ex.Message}")));
                return new StartSubscriptionResponse
                {
                    SubscriptionGuid = sub.Guid,
                    StartedAt = sub.StartedAt,
                    ExpiresAt = sub.ExpiresAt,
                    ApiResponse = new StreamResponse
                    {
                        StatusCode = HttpStatusCode.PartialContent,
                        Message = $"Some WebSocket consumers failed to start: {string.Join("; ", consumerStartExceptions.Select(e => $"{e.Url}: {e.Ex.Message}"))}"
                    }
                };
            }

            // All succeeded
            return new StartSubscriptionResponse
            {
                SubscriptionGuid = sub.Guid,
                StartedAt = sub.StartedAt,
                ExpiresAt = sub.ExpiresAt,
                ApiResponse = streamResult.ApiResponse,
                Format = req.StreamingFormat,
                Purpose = req.Purpose
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
                WebSocketUrls = s.WebSocketUrls,
                Format = s.Format,
                Purpose = s.Purpose
            }).ToList();
        }

        /// <summary>
        /// Protected method for consuming WebSocket streams.
        /// Can be used by derived classes for custom subscription implementations.
        /// </summary>
        protected virtual async Task ConsumeWebSocketStreamAsync(string wsUrl, string? purpose, SubscriptionGroup sub, bool logToFile, TaskCompletionSource<bool> connectedTcs, CancellationToken token)
        {
            var consumer = factory.Create(wsUrl, logToFile, purpose);
            await consumer.StartConsumingAsync(connectedTcs, token);
        }
    }
}
