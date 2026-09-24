using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Client.Services.WebSockets;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Constants;
using Morningstar.Streaming.Domain.Contracts;
using Morningstar.Streaming.Domain.Models;

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
        private const string StoppedDisconnectType = "Stopped";

        /// <inheritdoc />
        public event Action<Guid, Guid, string?, string>? SubscriptionStarted;

        /// <inheritdoc />
        public event Action<Guid, ArbitrationOutcome>? SubscriptionArbitrationCompleted;

        /// <inheritdoc />
        public event Action<Guid, Guid, string?, string, string>? SubscriptionDisconnected;

        /// <inheritdoc />
        public event Action<Guid, Guid, string?, string, string>? SubscriptionReconnected;

        public CanaryService(
            ISubscriptionGroupManager subscriptionManager,
            IStreamSubscriptionFactory streamSubscriptionFactory,
            IWebSocketConsumerFactory factory,
            ILogger<CanaryService> logger,
            IOptions<AppConfig> appConfig,
            IObservableMetric<IMetric>? observableMetric)
        {
            this.subscriptionManager = subscriptionManager;
            this.streamSubscriptionFactory = streamSubscriptionFactory;
            this.factory = factory;
            this.logger = logger;
            logMessages = appConfig.Value.LogMessages;
        }

        public Task<StartSubscriptionResponse> StartLevel1SubscriptionAsync(StartSubscriptionRequest req)
            => StartSubscriptionCoreAsync(req, streamSubscriptionFactory.CreateAsync);

        public Task<StartSubscriptionResponse> StartLevel2SubscriptionAsync(StartSubscriptionRequest req)
            => StartSubscriptionCoreAsync(req, streamSubscriptionFactory.CreateLevel2Async);

        /// <summary>
        /// Protected method that handles the core subscription logic.
        /// This can be called by derived classes to implement additional subscription methods.
        /// </summary>
        protected virtual async Task<StartSubscriptionResponse> StartSubscriptionCoreAsync<TRequest>(
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
                    consumer.Observer = new SubscriptionObserverRelay(sub.Guid, this);
                    var connectedTcs = new TaskCompletionSource<bool>();
                    var startTask = consumer.StartConsumingAsync(connectedTcs, sub.CancellationTokenSource.Token);
                    await connectedTcs.Task;
                    consumerTasks.Add(startTask);
                    succeededUrls.Add(url);

                    try
                    {
                        SubscriptionStarted?.Invoke(sub.Guid, sub.Guid, req.Purpose, wsUrl);
                    }
                    catch (Exception notifyEx)
                    {
                        logger.LogWarning(notifyEx, "Failed to notify started subscription {SubscriptionGuid} for WebSocket URL {WebSocketUrl}", sub.Guid, wsUrl);
                    }
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
            if (!subscriptionManager.TryAdd(sub))
            {
                logger.LogError("Failed to add subscription {SubscriptionGuid} to the subscription manager; stopping started WebSocket consumers.", sub.Guid);

                await sub.CancellationTokenSource.CancelAsync();
                sub.CancellationTokenSource.Dispose();

                return new StartSubscriptionResponse
                {
                    ApiResponse = new StreamResponse
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Message = "Failed to register the subscription."
                    }
                };
            }

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
                    if (subscriptionManager.TryRemove(sub.Guid, out _))
                    {
                        logger.LogInformation("Subscription {SubscriptionGuid} removed from manager after all consumers completed", sub.Guid);
                        sub.CancellationTokenSource.Dispose();
                    }
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

        public async Task<StopSubscriptionResponse> StopSubscriptionAsync(Guid guid)
        {
            if (!subscriptionManager.TryRemove(guid, out var sub) || sub == null)
            {
                logger.LogWarning("Attempted to stop non-existent subscription {SubscriptionGuid}", guid);
                return new StopSubscriptionResponse
                {
                    Success = false,
                    SubscriptionGuid = guid,
                    ErrorCode = ErrorCodes.SubscriptionNotFound,
                    Message = $"Subscription with ID {guid} was not found or has already been removed"
                };
            }

            await sub.CancellationTokenSource.CancelAsync();

            try
            {
                NotifySubscriptionStopped(sub);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to notify stopped subscription {SubscriptionGuid}", guid);
            }
            finally
            {
                sub.CancellationTokenSource.Dispose();
            }

            return new StopSubscriptionResponse
            {
                Success = true,
                SubscriptionGuid = guid,
                Message = "Subscription stopped successfully"
            };
        }

        private void NotifySubscriptionStopped(SubscriptionGroup subscription)
        {
            foreach (var webSocketUrl in GetMetricWebSocketUrls(subscription))
            {
                try
                {
                    SubscriptionDisconnected?.Invoke(subscription.Guid, subscription.Guid, subscription.Purpose, webSocketUrl, StoppedDisconnectType);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to notify stopped subscription {SubscriptionGuid} for WebSocket URL {WebSocketUrl}", subscription.Guid, webSocketUrl);
                }
            }
        }

        private static IEnumerable<string> GetMetricWebSocketUrls(SubscriptionGroup subscription)
        {
            foreach (var baseUrl in subscription.WebSocketUrls)
            {
                if (string.IsNullOrWhiteSpace(subscription.Format))
                {
                    yield return baseUrl;
                    continue;
                }

                yield return $"{baseUrl}/{subscription.Format}";
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

        /// <summary>Adapts a single WebSocketConsumer's consolidated observer notifications onto this subscription's own events, tagging them with the logical subscription's Guid.</summary>
        private sealed class SubscriptionObserverRelay(Guid subscriptionGuid, CanaryService owner) : IWebSocketConsumerObserver
        {
            public void OnArbitrationCompleted(ArbitrationOutcome outcome) =>
                owner.SubscriptionArbitrationCompleted?.Invoke(subscriptionGuid, outcome);

            public void OnDisconnected(Guid topicGuid, string? purpose, string webSocketUrl, string disconnectType) =>
                owner.SubscriptionDisconnected?.Invoke(subscriptionGuid, topicGuid, purpose, webSocketUrl, disconnectType);

            public void OnReconnected(Guid topicGuid, string? purpose, string webSocketUrl, string previousDisconnectType) =>
                owner.SubscriptionReconnected?.Invoke(subscriptionGuid, topicGuid, purpose, webSocketUrl, previousDisconnectType);
        }
    }
}
