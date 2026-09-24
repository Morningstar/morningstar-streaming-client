using System.Collections.Concurrent;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Morningstar.Streaming.Client.Services;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Client.Services.WebSockets;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Constants;
using Morningstar.Streaming.Domain.Contracts;
using Morningstar.Streaming.Domain.Models;

namespace Morningstar.Streaming.Client.Tests.ServiceTests
{
    /// <summary>
    /// Wraps a real <see cref="SubscriptionGroupManager"/> so tests observe genuine add/remove/get
    /// semantics (single source of truth, no duplicated in-memory tracking per test) while still
    /// exposing call counts and completion signals for deterministic, interaction-style assertions
    /// where needed. This lets tests verify actual state (e.g. "is it really gone?") instead of only
    /// verifying that a method was invoked.
    /// </summary>
    internal sealed class RealSubscriptionGroupManager : ISubscriptionGroupManager
    {
        private readonly SubscriptionGroupManager inner = new();
        private readonly ConcurrentDictionary<string, int> callCounts = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Guid>> completionSignals = new();

        public int CallCount(string methodName) => callCounts.GetValueOrDefault(methodName);

        public Task<Guid> WaitForCallAsync(string methodName) =>
            completionSignals.GetOrAdd(methodName, _ => new TaskCompletionSource<Guid>()).Task;

        public bool TryAdd(SubscriptionGroup sub)
        {
            Record(nameof(TryAdd), sub.Guid);
            return inner.TryAdd(sub);
        }

        public SubscriptionGroup Get(Guid guid)
        {
            Record(nameof(Get), guid);
            return inner.Get(guid);
        }

        public List<SubscriptionGroup> Get()
        {
            Record(nameof(Get), Guid.Empty);
            return inner.Get();
        }

        public bool TryRemove(Guid guid, out SubscriptionGroup? sub)
        {
            Record(nameof(TryRemove), guid);
            var removed = inner.TryRemove(guid, out sub);
            if (removed)
            {
                completionSignals.GetOrAdd(nameof(TryRemove), _ => new TaskCompletionSource<Guid>()).TrySetResult(guid);
            }

            return removed;
        }

        private void Record(string methodName, Guid guid)
        {
            callCounts.AddOrUpdate(methodName, 1, (_, count) => count + 1);
            completionSignals.GetOrAdd(methodName, _ => new TaskCompletionSource<Guid>()).TrySetResult(guid);
        }
    }

    public class CanaryServiceTests
    {
        private readonly Mock<ISubscriptionGroupManager> mockSubscriptionManager;
        private readonly Mock<IStreamSubscriptionFactory> mockStreamSubscriptionFactory;
        private readonly Mock<IWebSocketConsumerFactory> mockWebSocketConsumerFactory;
        private readonly Mock<ILogger<CanaryService>> mockLogger;
        private readonly Mock<IOptions<AppConfig>> mockAppConfig;
        private readonly Mock<IObservableMetric<IMetric>> mockObservableMetric;
        private readonly CanaryService canaryService;

        public CanaryServiceTests()
        {
            // Arrange - Initialize mocks
            mockSubscriptionManager = new Mock<ISubscriptionGroupManager>();
            mockStreamSubscriptionFactory = new Mock<IStreamSubscriptionFactory>();
            mockWebSocketConsumerFactory = new Mock<IWebSocketConsumerFactory>();
            mockLogger = new Mock<ILogger<CanaryService>>();
            mockAppConfig = new Mock<IOptions<AppConfig>>();
            mockObservableMetric = new Mock<IObservableMetric<IMetric>>();

            // Setup AppConfig with default values
            mockAppConfig.Setup(x => x.Value).Returns(new AppConfig
            {
                LogMessages = false,
                StreamingApiBaseAddress = "https://api.test.com",
                OAuthAddress = "https://oauth.test.com",
                ConnectionStringTtl = 300,
                LogMessagesPath = "logs"
            });

            // System Under Test
            canaryService = new CanaryService(
                mockSubscriptionManager.Object,
                mockStreamSubscriptionFactory.Object,
                mockWebSocketConsumerFactory.Object,
                mockLogger.Object,
                mockAppConfig.Object,
                mockObservableMetric.Object
            );
        }

        private static StreamSubscriptionResult CreateStreamResult(
            HttpStatusCode statusCode,
            List<string> webSocketUrls,
            CancellationTokenSource? cancellationTokenSource = null)
        {
            return new StreamSubscriptionResult
            {
                ApiResponse = new StreamResponse { StatusCode = statusCode },
                WebSocketUrls = webSocketUrls,
                CancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource()
            };
        }

        private void SetupTryAddSuccess()
        {
            mockSubscriptionManager
                .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
                .Returns(true);
        }

        private Mock<IWebSocketConsumer> SetupSuccessfulConsumer()
        {
            var mockConsumer = new Mock<IWebSocketConsumer>();
            mockConsumer
                .Setup(x => x.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
                .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) => tcs.SetResult(true))
                .Returns(Task.CompletedTask);

            mockWebSocketConsumerFactory
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockConsumer.Object);

            return mockConsumer;
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WithSuccessfulResponse_ReturnsStartSubscriptionResponse()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60
            };

            var expectedWebSocketUrls = new List<string> { "wss://test.com/stream1", "wss://test.com/stream2" };
            var streamResult = CreateStreamResult(HttpStatusCode.OK, expectedWebSocketUrls);

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            SetupTryAddSuccess();
            SetupSuccessfulConsumer();

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.SubscriptionGuid.Should().NotBeEmpty();
            result.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.ExpiresAt.Should().NotBeNull();
            result.ExpiresAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddSeconds(60), TimeSpan.FromSeconds(5));
            result.ApiResponse.Should().NotBeNull();
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            mockStreamSubscriptionFactory.Verify(x => x.CreateAsync(request), Times.Once);
            mockSubscriptionManager.Verify(x => x.TryAdd(It.IsAny<SubscriptionGroup>()), Times.Once);
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WithSuccessfulConsumers_RaisesSubscriptionStartedPerUrl()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60,
                Purpose = "Sample purpose",
                StreamingFormat = "avro"
            };

            var expectedWebSocketUrls = new List<string> { "wss://test.com/stream1", "wss://test.com/stream2" };
            var streamResult = CreateStreamResult(HttpStatusCode.OK, expectedWebSocketUrls);

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            SetupTryAddSuccess();
            SetupSuccessfulConsumer();

            var startedEvents = new List<(Guid TopicGuid, string? Purpose, string WebSocketUrl)>();
            canaryService.SubscriptionStarted += (_, topicGuid, purpose, url) =>
                startedEvents.Add((topicGuid, purpose, url));

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);

            // Assert
            startedEvents.Should().HaveCount(2);
            startedEvents.Should().OnlyContain(e => e.TopicGuid == result.SubscriptionGuid && e.Purpose == "Sample purpose");
            startedEvents.Select(e => e.WebSocketUrl).Should().BeEquivalentTo(new[] { "wss://test.com/stream1/avro", "wss://test.com/stream2/avro" });
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WithPartialContentResponse_ReturnsStartSubscriptionResponse()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 120
            };

            var expectedWebSocketUrls = new List<string> { "wss://test.com/stream1" };
            var streamResult = CreateStreamResult(HttpStatusCode.PartialContent, expectedWebSocketUrls);

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            SetupTryAddSuccess();
            SetupSuccessfulConsumer();

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.SubscriptionGuid.Should().NotBeEmpty();
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.PartialContent);
            mockStreamSubscriptionFactory.Verify(x => x.CreateAsync(request), Times.Once);
            mockSubscriptionManager.Verify(x => x.TryAdd(It.IsAny<SubscriptionGroup>()), Times.Once);
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WithBadRequestResponse_ReturnsResponseWithoutCreatingSubscription()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60
            };

            var streamResult = new StreamSubscriptionResult
            {
                ApiResponse = new StreamResponse { StatusCode = HttpStatusCode.BadRequest },
                WebSocketUrls = new List<string>(),
                CancellationTokenSource = new CancellationTokenSource()
            };

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.SubscriptionGuid.Should().BeNull();
            result.StartedAt.Should().BeNull();
            result.ExpiresAt.Should().BeNull();
            result.ApiResponse.Should().NotBeNull();
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            mockStreamSubscriptionFactory.Verify(x => x.CreateAsync(request), Times.Once);
            mockSubscriptionManager.Verify(x => x.TryAdd(It.IsAny<SubscriptionGroup>()), Times.Never);
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WithNoDuration_CreatesSubscriptionWithoutExpiryDate()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = null
            };

            var expectedWebSocketUrls = new List<string> { "wss://test.com/stream1" };
            var streamResult = CreateStreamResult(HttpStatusCode.OK, expectedWebSocketUrls);

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            SetupTryAddSuccess();
            SetupSuccessfulConsumer();

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.SubscriptionGuid.Should().NotBeEmpty();
            result.ExpiresAt.Should().BeNull();
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_CreatesWebSocketConsumersForAllUrls()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60
            };

            var expectedWebSocketUrls = new List<string>
            {
                "wss://test.com/stream1",
                "wss://test.com/stream2",
                "wss://test.com/stream3"
            };
            var streamResult = CreateStreamResult(HttpStatusCode.OK, expectedWebSocketUrls);

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            SetupTryAddSuccess();

            var mockConsumer = new Mock<IWebSocketConsumer>();
            mockConsumer
                .Setup(x => x.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
                .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) => tcs.SetResult(true))
                .Returns(Task.CompletedTask);

            var createdCount = 0;
            var allConsumersCreated = new TaskCompletionSource();
            mockWebSocketConsumerFactory
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockConsumer.Object)
                .Callback(() =>
                {
                    if (Interlocked.Increment(ref createdCount) == expectedWebSocketUrls.Count)
                    {
                        allConsumersCreated.TrySetResult();
                    }
                });

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);

            // Wait deterministically for all background consumer creations to complete.
            var completedTask = await Task.WhenAny(allConsumersCreated.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            completedTask.Should().Be(allConsumersCreated.Task, "all web socket consumers should be created for each url");

            // Assert
            mockWebSocketConsumerFactory.Verify(
                x => x.Create(It.IsAny<string>(), false, It.IsAny<string?>()),
                Times.Exactly(expectedWebSocketUrls.Count)
            );
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WithLogMessagesEnabled_CreatesConsumersWithLoggingEnabled()
        {
            // Arrange
            mockAppConfig.Setup(x => x.Value).Returns(new AppConfig
            {
                LogMessages = true,
                StreamingApiBaseAddress = "https://api.test.com",
                OAuthAddress = "https://oauth.test.com",
                ConnectionStringTtl = 300,
                LogMessagesPath = "logs"
            });

            var sutWithLogging = new CanaryService(
                mockSubscriptionManager.Object,
                mockStreamSubscriptionFactory.Object,
                mockWebSocketConsumerFactory.Object,
                mockLogger.Object,
                mockAppConfig.Object,
                mockObservableMetric.Object
            );

            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60
            };

            var expectedWebSocketUrls = new List<string> { "wss://test.com/stream1" };
            var streamResult = CreateStreamResult(HttpStatusCode.OK, expectedWebSocketUrls);

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            SetupTryAddSuccess();

            var createdEvent = new TaskCompletionSource();
            var mockConsumer = new Mock<IWebSocketConsumer>();
            mockConsumer
                .Setup(x => x.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
                .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) => tcs.SetResult(true))
                .Returns(Task.CompletedTask);

            mockWebSocketConsumerFactory
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockConsumer.Object)
                .Callback(() => createdEvent.TrySetResult());

            // Act
            var result = await sutWithLogging.StartLevel1SubscriptionAsync(request);

            await Task.WhenAny(createdEvent.Task, Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert
            mockWebSocketConsumerFactory.Verify(
                x => x.Create(It.IsAny<string>(), true, It.IsAny<string?>()),
                Times.Once
            );
        }

        [Fact]
        public async Task StopSubscriptionAsync_WithExistingSubscription_CancelsSubscription()
        {
            // Arrange
            var subscriptionGuid = Guid.NewGuid();
            var cancellationTokenSource = new CancellationTokenSource();

            var subscriptionGroup = new SubscriptionGroup
            {
                Guid = subscriptionGuid,
                WebSocketUrls = new List<string> { "wss://test.com/stream1" },
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(60),
                CancellationTokenSource = cancellationTokenSource,
                Format = "avro",
                Purpose = "Sample purpose"
            };

            SubscriptionGroup? outSub = subscriptionGroup;
            mockSubscriptionManager
                .Setup(x => x.TryRemove(subscriptionGuid, out outSub))
                .Returns(true);

            var disconnectedEvents = new List<(Guid TopicGuid, string? Purpose, string WebSocketUrl, string DisconnectType)>();
            canaryService.SubscriptionDisconnected += (_, topicGuid, purpose, url, disconnectType) =>
                disconnectedEvents.Add((topicGuid, purpose, url, disconnectType));

            // Act
            var result = await canaryService.StopSubscriptionAsync(subscriptionGuid);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.SubscriptionGuid.Should().Be(subscriptionGuid);
            result.Message.Should().Be("Subscription stopped successfully");
            result.ErrorCode.Should().BeNull();
            cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
            mockSubscriptionManager.Verify(x => x.TryRemove(subscriptionGuid, out outSub), Times.Once);
            disconnectedEvents.Should().ContainSingle(e =>
                e.TopicGuid == subscriptionGuid &&
                e.Purpose == "Sample purpose" &&
                e.DisconnectType == "Stopped" &&
                e.WebSocketUrl == "wss://test.com/stream1/avro");

            Action accessTokenAfterStop = () => _ = cancellationTokenSource.Token;
            accessTokenAfterStop.Should().Throw<ObjectDisposedException>("the CancellationTokenSource should be disposed once the subscription is genuinely removed");
        }

        [Fact]
        public async Task StopSubscriptionAsync_WhenOneWebSocketUrlMetricFails_StillRecordsMetricsForRemainingUrls()
        {
            // Arrange
            var subscriptionGuid = Guid.NewGuid();
            var cancellationTokenSource = new CancellationTokenSource();

            var subscriptionGroup = new SubscriptionGroup
            {
                Guid = subscriptionGuid,
                WebSocketUrls = new List<string> { "wss://test.com/stream1", "wss://test.com/stream2" },
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(60),
                CancellationTokenSource = cancellationTokenSource,
                Format = "avro",
                Purpose = "Sample purpose"
            };

            SubscriptionGroup? outSub = subscriptionGroup;
            mockSubscriptionManager
                .Setup(x => x.TryRemove(subscriptionGuid, out outSub))
                .Returns(true);

            var notifiedUrls = new List<string>();
            canaryService.SubscriptionDisconnected += (_, _, _, url, _) =>
            {
                notifiedUrls.Add(url);

                if (url == "wss://test.com/stream1/avro")
                {
                    throw new InvalidOperationException("Telemetry backend unavailable for stream1");
                }
            };

            // Act
            var result = await canaryService.StopSubscriptionAsync(subscriptionGuid);

            // Assert
            result.Success.Should().BeTrue();

            // The failing URL's notification was attempted, and the throwing subscriber did not
            // prevent the second URL's notification from still being raised.
            notifiedUrls.Should().BeEquivalentTo(new[] { "wss://test.com/stream1/avro", "wss://test.com/stream2/avro" });
        }

        [Fact]
        public async Task StopSubscriptionAsync_WhenRecordMetricsThrows_StillCancelsAndRemovesSubscriptionAndReturnsSuccess()
        {
            // Arrange - use a real subscription manager so removal can be verified genuinely
            var realSubscriptionManager = new SubscriptionGroupManager();
            var canaryService = new CanaryService(
                realSubscriptionManager,
                mockStreamSubscriptionFactory.Object,
                mockWebSocketConsumerFactory.Object,
                mockLogger.Object,
                mockAppConfig.Object,
                mockObservableMetric.Object);

            var subscriptionGuid = Guid.NewGuid();
            var cancellationTokenSource = new CancellationTokenSource();

            var subscriptionGroup = new SubscriptionGroup
            {
                Guid = subscriptionGuid,
                WebSocketUrls = new List<string> { "wss://test.com/stream1" },
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(60),
                CancellationTokenSource = cancellationTokenSource,
                Format = "avro",
                Purpose = "Sample purpose"
            };

            realSubscriptionManager.TryAdd(subscriptionGroup).Should().BeTrue();

            canaryService.SubscriptionDisconnected += (_, _, _, _, _) =>
                throw new InvalidOperationException("Telemetry backend unavailable");

            // Act
            var result = await canaryService.StopSubscriptionAsync(subscriptionGuid);

            // Assert - cancellation and removal happen regardless of the subscriber failure
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.SubscriptionGuid.Should().Be(subscriptionGuid);
            result.Message.Should().Be("Subscription stopped successfully");
            cancellationTokenSource.IsCancellationRequested.Should().BeTrue();

            // Assert - subscription is genuinely gone from the manager, not just that Remove was invoked
            realSubscriptionManager.Get().Should().BeEmpty();
            Assert.Throws<InvalidOperationException>(() => realSubscriptionManager.Get(subscriptionGuid));
        }

        [Fact]
        public async Task StopSubscriptionAsync_WhenCalledConcurrentlyForSameSubscription_OnlyOneCallSucceedsAndMetricsRecordedOnce()
        {
            // Arrange
            var realSubscriptionManager = new SubscriptionGroupManager();
            var canaryService = new CanaryService(
                realSubscriptionManager,
                mockStreamSubscriptionFactory.Object,
                mockWebSocketConsumerFactory.Object,
                mockLogger.Object,
                mockAppConfig.Object,
                mockObservableMetric.Object);

            var subscriptionGuid = Guid.NewGuid();
            var cancellationTokenSource = new CancellationTokenSource();

            var subscriptionGroup = new SubscriptionGroup
            {
                Guid = subscriptionGuid,
                WebSocketUrls = new List<string> { "wss://test.com/stream1" },
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(60),
                CancellationTokenSource = cancellationTokenSource,
                Format = "avro",
                Purpose = "Sample purpose"
            };

            realSubscriptionManager.TryAdd(subscriptionGroup).Should().BeTrue();

            var notificationCount = 0;
            canaryService.SubscriptionDisconnected += (_, _, _, _, _) => Interlocked.Increment(ref notificationCount);

            // Act
            var stopTask1 = canaryService.StopSubscriptionAsync(subscriptionGuid);
            var stopTask2 = canaryService.StopSubscriptionAsync(subscriptionGuid);
            var results = await Task.WhenAll(stopTask1, stopTask2);

            // Assert
            results.Count(r => r.Success).Should().Be(1, "only one caller should be able to claim the subscription for cleanup");
            results.Count(r => !r.Success).Should().Be(1, "the losing caller should be told the subscription was already removed");
            results.Should().OnlyContain(r => r.SubscriptionGuid == subscriptionGuid);

            var failedResult = results.Single(r => !r.Success);
            failedResult.ErrorCode.Should().Be(ErrorCodes.SubscriptionNotFound);

            cancellationTokenSource.IsCancellationRequested.Should().BeTrue();

            // Assert
            realSubscriptionManager.Get().Should().BeEmpty();
            Assert.Throws<InvalidOperationException>(() => realSubscriptionManager.Get(subscriptionGuid));
            notificationCount.Should().Be(1);
        }

        [Fact]
        public async Task StopSubscriptionAsync_CancelsAndRemovesSubscriptionBeforeRecordingMetrics()
        {
            // Arrange - use a real subscription manager so removal can be verified genuinely
            var realSubscriptionManager = new SubscriptionGroupManager();
            var canaryService = new CanaryService(
                realSubscriptionManager,
                mockStreamSubscriptionFactory.Object,
                mockWebSocketConsumerFactory.Object,
                mockLogger.Object,
                mockAppConfig.Object,
                mockObservableMetric.Object);

            var subscriptionGuid = Guid.NewGuid();
            var cancellationTokenSource = new CancellationTokenSource();

            var subscriptionGroup = new SubscriptionGroup
            {
                Guid = subscriptionGuid,
                WebSocketUrls = new List<string> { "wss://test.com/stream1" },
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(60),
                CancellationTokenSource = cancellationTokenSource,
                Format = "avro",
                Purpose = "Sample purpose"
            };

            realSubscriptionManager.TryAdd(subscriptionGroup).Should().BeTrue();

            var callOrder = new List<string>();

            canaryService.SubscriptionDisconnected += (_, _, _, _, _) =>
            {
                // Confirm the subscription is genuinely gone from the manager by the time subscribers are notified
                Assert.Throws<InvalidOperationException>(() => realSubscriptionManager.Get(subscriptionGuid));
                callOrder.Add("Notify");
            };

            // Act
            await canaryService.StopSubscriptionAsync(subscriptionGuid);

            // Assert - subscription is removed before subscribers are notified
            callOrder.Should().Equal("Notify");
            realSubscriptionManager.Get().Should().BeEmpty();
        }

        [Fact]
        public async Task StopSubscriptionAsync_WithNonExistingSubscription_ReturnsErrorResponse()
        {
            // Arrange
            var subscriptionGuid = Guid.NewGuid();

            SubscriptionGroup? outSub = null;
            mockSubscriptionManager
                .Setup(x => x.TryRemove(subscriptionGuid, out outSub))
                .Returns(false);

            var notified = false;
            canaryService.SubscriptionDisconnected += (_, _, _, _, _) => notified = true;

            // Act
            var result = await canaryService.StopSubscriptionAsync(subscriptionGuid);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.SubscriptionGuid.Should().Be(subscriptionGuid);
            result.ErrorCode.Should().Be(ErrorCodes.SubscriptionNotFound);
            result.Message.Should().Contain("not found");
            mockSubscriptionManager.Verify(x => x.TryRemove(subscriptionGuid, out outSub), Times.Once);
            notified.Should().BeFalse();
        }

        [Fact]
        public void GetActiveSubscriptions_WithNoSubscriptions_ReturnsEmptyList()
        {
            // Arrange
            mockSubscriptionManager
                .Setup(x => x.Get())
                .Returns(new List<SubscriptionGroup>());

            // Act
            var result = canaryService.GetActiveSubscriptions();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            mockSubscriptionManager.Verify(x => x.Get(), Times.Once);
        }

        [Fact]
        public void GetActiveSubscriptions_WithMultipleSubscriptions_ReturnsAllSubscriptions()
        {
            // Arrange
            var subscription1 = new SubscriptionGroup
            {
                Guid = Guid.NewGuid(),
                WebSocketUrls = new List<string> { "wss://test.com/stream1" },
                StartedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiresAt = DateTime.UtcNow.AddMinutes(55),
                CancellationTokenSource = new CancellationTokenSource()
            };

            var subscription2 = new SubscriptionGroup
            {
                Guid = Guid.NewGuid(),
                WebSocketUrls = new List<string> { "wss://test.com/stream2", "wss://test.com/stream3" },
                StartedAt = DateTime.UtcNow.AddMinutes(-3),
                ExpiresAt = null,
                CancellationTokenSource = new CancellationTokenSource()
            };

            var subscriptions = new List<SubscriptionGroup> { subscription1, subscription2 };

            mockSubscriptionManager
                .Setup(x => x.Get())
                .Returns(subscriptions);

            // Act
            var result = canaryService.GetActiveSubscriptions();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            result[0].Guid.Should().Be(subscription1.Guid);
            result[0].WebSocketUrls.Should().BeEquivalentTo(subscription1.WebSocketUrls);
            result[0].StartedAt.Should().Be(subscription1.StartedAt);
            result[0].ExpiresAt.Should().Be(subscription1.ExpiresAt);

            result[1].Guid.Should().Be(subscription2.Guid);
            result[1].WebSocketUrls.Should().BeEquivalentTo(subscription2.WebSocketUrls);
            result[1].StartedAt.Should().Be(subscription2.StartedAt);
            result[1].ExpiresAt.Should().BeNull();

            mockSubscriptionManager.Verify(x => x.Get(), Times.Once);
        }

        [Fact]
        public async Task AddsSubscriptionToManagerAfterStartingConsumers()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60
            };

            var expectedWebSocketUrls = new List<string> { "wss://test.com/stream1" };
            var expectedCancellationTokenSource = new CancellationTokenSource();

            var streamResult = new StreamSubscriptionResult
            {
                ApiResponse = new StreamResponse { StatusCode = HttpStatusCode.OK },
                WebSocketUrls = expectedWebSocketUrls,
                CancellationTokenSource = expectedCancellationTokenSource
            };

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            var callSequence = new List<string>();
            mockSubscriptionManager
                .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
                .Callback(() => callSequence.Add("TryAdd"))
                .Returns(true);

            var mockConsumer = new Mock<IWebSocketConsumer>();
            mockConsumer
                .Setup(x => x.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
                .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) =>
                {
                    callSequence.Add("StartConsumingAsync");
                    tcs.SetResult(true);
                })
                .Returns(Task.CompletedTask);

            mockWebSocketConsumerFactory
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockConsumer.Object);

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);

            // Assert
            callSequence.Should().ContainInOrder("StartConsumingAsync", "TryAdd");
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WhenConsumerTaskFaults_RemovesSubscriptionFromManager()
        {
            // Arrange
            var realSubscriptionManager = new RealSubscriptionGroupManager();
            var canaryService = new CanaryService(
                realSubscriptionManager,
                mockStreamSubscriptionFactory.Object,
                mockWebSocketConsumerFactory.Object,
                mockLogger.Object,
                mockAppConfig.Object,
                mockObservableMetric.Object);

            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60
            };

            var expectedWebSocketUrls = new List<string> { "wss://test.com/stream1" };
            var expectedCancellationTokenSource = new CancellationTokenSource();

            var streamResult = new StreamSubscriptionResult
            {
                ApiResponse = new StreamResponse { StatusCode = HttpStatusCode.OK },
                WebSocketUrls = expectedWebSocketUrls,
                CancellationTokenSource = expectedCancellationTokenSource
            };

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            var consumerTaskSource = new TaskCompletionSource();
            var mockConsumer = new Mock<IWebSocketConsumer>();
            mockConsumer
                .Setup(x => x.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
                .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) => tcs.SetResult(true))
                .Returns(consumerTaskSource.Task);

            mockWebSocketConsumerFactory
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockConsumer.Object);

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);

            realSubscriptionManager.Get(result.SubscriptionGuid!.Value).Should().NotBeNull();

            consumerTaskSource.SetException(new InvalidOperationException("Simulated disconnection"));

            var completedTask = await Task.WhenAny(realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove)), Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert
            completedTask.Should().Be(realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove)), "the subscription should be removed from the manager after the consumer task faults");
            var removedGuid = await realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove));
            removedGuid.Should().Be(result.SubscriptionGuid!.Value);

            // Assert
            realSubscriptionManager.Get().Should().BeEmpty();
            Assert.Throws<InvalidOperationException>(() => realSubscriptionManager.Get(result.SubscriptionGuid!.Value));
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WhenMultipleConsumerTasksFault_RemovesSubscriptionExactlyOnce()
        {
            // Arrange
            var realSubscriptionManager = new RealSubscriptionGroupManager();
            var canaryService = new CanaryService(
                realSubscriptionManager,
                mockStreamSubscriptionFactory.Object,
                mockWebSocketConsumerFactory.Object,
                mockLogger.Object,
                mockAppConfig.Object,
                mockObservableMetric.Object);

            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60
            };

            var expectedWebSocketUrls = new List<string> { "wss://test.com/stream1", "wss://test.com/stream2" };
            var expectedCancellationTokenSource = new CancellationTokenSource();

            var streamResult = new StreamSubscriptionResult
            {
                ApiResponse = new StreamResponse { StatusCode = HttpStatusCode.OK },
                WebSocketUrls = expectedWebSocketUrls,
                CancellationTokenSource = expectedCancellationTokenSource
            };

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            var consumerTaskSource1 = new TaskCompletionSource();
            var consumerTaskSource2 = new TaskCompletionSource();
            var callIndex = 0;
            var taskSources = new[] { consumerTaskSource1, consumerTaskSource2 };

            var mockConsumer = new Mock<IWebSocketConsumer>();
            mockConsumer
                .Setup(x => x.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
                .Returns((TaskCompletionSource<bool> tcs, CancellationToken _) =>
                {
                    var index = Interlocked.Increment(ref callIndex) - 1;
                    tcs.SetResult(true);
                    return taskSources[index].Task;
                });

            mockWebSocketConsumerFactory
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockConsumer.Object);

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);

            realSubscriptionManager.Get(result.SubscriptionGuid!.Value).Should().NotBeNull();

            consumerTaskSource1.SetException(new InvalidOperationException("Simulated disconnection 1"));
            consumerTaskSource2.SetException(new InvalidOperationException("Simulated disconnection 2"));

            await Task.WhenAny(
                Task.WhenAll(
                    consumerTaskSource1.Task.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted),
                    consumerTaskSource2.Task.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted)),
                Task.Delay(TimeSpan.FromSeconds(5)));
            consumerTaskSource1.Task.IsFaulted.Should().BeTrue("consumer 1's task should have faulted");
            consumerTaskSource2.Task.IsFaulted.Should().BeTrue("consumer 2's task should have faulted");
            consumerTaskSource1.Task.Exception!.InnerException!.Message.Should().Be("Simulated disconnection 1");
            consumerTaskSource2.Task.Exception!.InnerException!.Message.Should().Be("Simulated disconnection 2");

            var completedTask = await Task.WhenAny(realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove)), Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert
            completedTask.Should().Be(realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove)), "the subscription should be removed after all consumer tasks fault");
            var removedGuid = await realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove));
            removedGuid.Should().Be(result.SubscriptionGuid!.Value);

            await Task.Delay(TimeSpan.FromMilliseconds(200));
            realSubscriptionManager.CallCount(nameof(ISubscriptionGroupManager.TryRemove)).Should().Be(1, "TryRemove should only succeed once even when multiple consumer tasks fault");

            // Assert
            realSubscriptionManager.Get().Should().BeEmpty();
            Assert.Throws<InvalidOperationException>(() => realSubscriptionManager.Get(result.SubscriptionGuid!.Value));
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WhenConsumerTaskCompletesNormally_RemovesSubscriptionFromManager()
        {
            // Arrange - use a real subscription manager so removal can be verified genuinely.
            var realSubscriptionManager = new RealSubscriptionGroupManager();
            var canaryService = new CanaryService(
                realSubscriptionManager,
                mockStreamSubscriptionFactory.Object,
                mockWebSocketConsumerFactory.Object,
                mockLogger.Object,
                mockAppConfig.Object,
                mockObservableMetric.Object);

            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60
            };

            var expectedWebSocketUrls = new List<string> { "wss://test.com/stream1" };
            var expectedCancellationTokenSource = new CancellationTokenSource();

            var streamResult = new StreamSubscriptionResult
            {
                ApiResponse = new StreamResponse { StatusCode = HttpStatusCode.OK },
                WebSocketUrls = expectedWebSocketUrls,
                CancellationTokenSource = expectedCancellationTokenSource
            };

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            var consumerTaskSource = new TaskCompletionSource();
            var mockConsumer = new Mock<IWebSocketConsumer>();
            mockConsumer
                .Setup(x => x.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
                .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) => tcs.SetResult(true))
                .Returns(consumerTaskSource.Task);

            mockWebSocketConsumerFactory
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockConsumer.Object);

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);

            realSubscriptionManager.Get(result.SubscriptionGuid!.Value).Should().NotBeNull();

            consumerTaskSource.SetResult();

            var completedTask = await Task.WhenAny(realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove)), Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert
            completedTask.Should().Be(realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove)), "the subscription should be removed once the consumer task completes, even without a fault");
            var removedGuid = await realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove));
            removedGuid.Should().Be(result.SubscriptionGuid!.Value);

            // Assert
            realSubscriptionManager.Get().Should().BeEmpty();
            Assert.Throws<InvalidOperationException>(() => realSubscriptionManager.Get(result.SubscriptionGuid!.Value));
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WhenOneOfMultipleSubscriptionsFaults_OnlyRemovesFaultedSubscription()
        {
            // Arrange
            var realSubscriptionManager = new RealSubscriptionGroupManager();
            var canaryService = new CanaryService(
                realSubscriptionManager,
                mockStreamSubscriptionFactory.Object,
                mockWebSocketConsumerFactory.Object,
                mockLogger.Object,
                mockAppConfig.Object,
                mockObservableMetric.Object);

            var request1 = new StartSubscriptionRequest { DurationSeconds = 60 };
            var request2 = new StartSubscriptionRequest { DurationSeconds = 60 };

            var streamResult1 = new StreamSubscriptionResult
            {
                ApiResponse = new StreamResponse { StatusCode = HttpStatusCode.OK },
                WebSocketUrls = new List<string> { "wss://test.com/stream1" },
                CancellationTokenSource = new CancellationTokenSource()
            };

            var streamResult2 = new StreamSubscriptionResult
            {
                ApiResponse = new StreamResponse { StatusCode = HttpStatusCode.OK },
                WebSocketUrls = new List<string> { "wss://test.com/stream2" },
                CancellationTokenSource = new CancellationTokenSource()
            };

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request1))
                .ReturnsAsync(streamResult1);
            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request2))
                .ReturnsAsync(streamResult2);

            var faultingConsumerTaskSource = new TaskCompletionSource();
            var healthyConsumerTaskSource = new TaskCompletionSource();

            var mockFaultingConsumer = new Mock<IWebSocketConsumer>();
            mockFaultingConsumer
                .Setup(x => x.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
                .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) => tcs.SetResult(true))
                .Returns(faultingConsumerTaskSource.Task);

            var mockHealthyConsumer = new Mock<IWebSocketConsumer>();
            mockHealthyConsumer
                .Setup(x => x.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
                .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) => tcs.SetResult(true))
                .Returns(healthyConsumerTaskSource.Task);

            mockWebSocketConsumerFactory
                .Setup(x => x.Create(It.Is<string>(u => u.StartsWith("wss://test.com/stream1")), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockFaultingConsumer.Object);
            mockWebSocketConsumerFactory
                .Setup(x => x.Create(It.Is<string>(u => u.StartsWith("wss://test.com/stream2")), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockHealthyConsumer.Object);

            // Act
            var result1 = await canaryService.StartLevel1SubscriptionAsync(request1);
            var result2 = await canaryService.StartLevel1SubscriptionAsync(request2);

            realSubscriptionManager.Get().Should().HaveCount(2);

            faultingConsumerTaskSource.SetException(new InvalidOperationException("Simulated disconnection"));

            var completedTask = await Task.WhenAny(realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove)), Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert
            completedTask.Should().Be(realSubscriptionManager.WaitForCallAsync(nameof(ISubscriptionGroupManager.TryRemove)), "the faulted subscription should be removed");

            await Task.Delay(TimeSpan.FromMilliseconds(200));

            realSubscriptionManager.CallCount(nameof(ISubscriptionGroupManager.TryRemove)).Should().Be(1, "only the faulted subscription should be removed");

            var activeSubscriptions = canaryService.GetActiveSubscriptions();
            activeSubscriptions.Should().HaveCount(1, "only the healthy subscription should remain after the faulted one is removed");
            activeSubscriptions.Select(s => s.Guid).Should().NotContain(result1.SubscriptionGuid!.Value);
            activeSubscriptions.Single().Guid.Should().Be(result2.SubscriptionGuid!.Value);

            // Assert
            Assert.Throws<InvalidOperationException>(() => realSubscriptionManager.Get(result1.SubscriptionGuid!.Value));
            realSubscriptionManager.Get(result2.SubscriptionGuid!.Value).Should().NotBeNull();

            healthyConsumerTaskSource.SetResult();
        }

        [Fact]
        public async Task StartLevel2SubscriptionAsync_WithSuccessfulResponse_ReturnsStartSubscriptionResponse()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60
            };

            var expectedWebSocketUrls = new List<string> { "wss://test.com/stream1", "wss://test.com/stream2" };
            var expectedCancellationTokenSource = new CancellationTokenSource();

            var streamResult = new StreamSubscriptionResult
            {
                ApiResponse = new StreamResponse { StatusCode = HttpStatusCode.OK },
                WebSocketUrls = expectedWebSocketUrls,
                CancellationTokenSource = expectedCancellationTokenSource
            };

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateLevel2Async(request))
                .ReturnsAsync(streamResult);

            SetupTryAddSuccess();
            SetupSuccessfulConsumer();

            // Act
            var result = await canaryService.StartLevel2SubscriptionAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            result.SubscriptionGuid.Should().NotBeEmpty();
            result.StartedAt.Should().NotBe(default);

            mockStreamSubscriptionFactory.Verify(x => x.CreateLevel2Async(request), Times.Once);
            mockStreamSubscriptionFactory.Verify(x => x.CreateAsync(It.IsAny<StartSubscriptionRequest>()), Times.Never);
        }

        [Fact]
        public async Task StartLevel2SubscriptionAsync_WhenApiReturnsNonSuccess_ReturnsErrorResponse()
        {
            // Arrange
            var request = new StartSubscriptionRequest { DurationSeconds = 60 };

            var streamResult = new StreamSubscriptionResult
            {
                ApiResponse = new StreamResponse { StatusCode = HttpStatusCode.Unauthorized },
                WebSocketUrls = new List<string>(),
                CancellationTokenSource = new CancellationTokenSource()
            };

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateLevel2Async(request))
                .ReturnsAsync(streamResult);

            // Act
            var result = await canaryService.StartLevel2SubscriptionAsync(request);

            // Assert
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            result.SubscriptionGuid.Should().BeNull();
            mockSubscriptionManager.Verify(x => x.TryAdd(It.IsAny<SubscriptionGroup>()), Times.Never);
        }

        [Fact]
        public async Task StartLevel2SubscriptionAsync_AddsSubscriptionToManager()
        {
            // Arrange
            var request = new StartSubscriptionRequest { DurationSeconds = 60 };
            var streamResult = CreateStreamResult(HttpStatusCode.OK, new List<string> { "wss://test.com/stream1" });

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateLevel2Async(request))
                .ReturnsAsync(streamResult);

            SubscriptionGroup? addedGroup = null;
            mockSubscriptionManager
                .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
                .Callback<SubscriptionGroup>(g => addedGroup = g)
                .Returns(true);

            SetupSuccessfulConsumer();

            // Act
            var result = await canaryService.StartLevel2SubscriptionAsync(request);

            // Assert
            mockSubscriptionManager.Verify(x => x.TryAdd(It.IsAny<SubscriptionGroup>()), Times.Once);
            addedGroup.Should().NotBeNull();
            addedGroup!.Guid.Should().Be(result.SubscriptionGuid!.Value);
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WhenTryAddFails_ReturnsErrorAndSubscriptionDoesNotAppearInActiveSubscriptions()
        {
            // Arrange
            var request = new StartSubscriptionRequest { DurationSeconds = 60 };
            var cts = new CancellationTokenSource();
            var streamResult = CreateStreamResult(HttpStatusCode.OK, new List<string> { "wss://test.com/stream1" }, cts);

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            mockSubscriptionManager
                .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
                .Returns(false);

            mockSubscriptionManager
                .Setup(x => x.Get())
                .Returns(new List<SubscriptionGroup>());

            SetupSuccessfulConsumer();

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);
            var activeSubscriptions = canaryService.GetActiveSubscriptions();

            // Assert
            result.SubscriptionGuid.Should().BeNull();
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            activeSubscriptions.Should().BeEmpty("the subscription was never actually added to the manager");

            cts.IsCancellationRequested.Should().BeTrue();
            Assert.Throws<ObjectDisposedException>(() => cts.Token);
        }
    }
}
