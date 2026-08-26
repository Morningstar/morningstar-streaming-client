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
using System.Net;

namespace Morningstar.Streaming.Client.Tests.ServiceTests
{
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

            mockObservableMetric
                .Setup(x => x.RecordMetric(It.IsAny<string>(), It.IsAny<IMetric>(), It.IsAny<IDictionary<string, string>?>()))
                .Returns(Task.CompletedTask);

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

            // Wait deterministically for the background consumer creation to complete.
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

            mockSubscriptionManager
                .Setup(x => x.Get(subscriptionGuid))
                .Returns(subscriptionGroup);

            // Act
            var result = await canaryService.StopSubscriptionAsync(subscriptionGuid);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.SubscriptionGuid.Should().Be(subscriptionGuid);
            result.Message.Should().Be("Subscription stopped successfully");
            result.ErrorCode.Should().BeNull();
            cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
            mockSubscriptionManager.Verify(x => x.Get(subscriptionGuid), Times.Once);
            mockObservableMetric.Verify(
                x => x.RecordMetric(
                    MetricEvents.WebSocketDisconnections,
                    It.IsAny<AtomicLong>(),
                    It.Is<IDictionary<string, string>?>(tags =>
                        tags != null &&
                        tags["SubscriptionId"] == subscriptionGuid.ToString() &&
                        tags["TopicGuid"] == subscriptionGuid.ToString() &&
                        tags["Purpose"] == "Sample purpose" &&
                        tags["DisconnectType"] == "Stopped" &&
                        tags["WebSocketUrl"] == "wss://test.com/stream1/avro")),
                Times.Once);
        }

        [Fact]
        public async Task StopSubscriptionAsync_WithNonExistingSubscription_ReturnsErrorResponse()
        {
            // Arrange
            var subscriptionGuid = Guid.NewGuid();

            mockSubscriptionManager
                .Setup(x => x.Get(subscriptionGuid))
                .Throws(new InvalidOperationException($"Subscription does not exist {subscriptionGuid}"));

            // Act
            var result = await canaryService.StopSubscriptionAsync(subscriptionGuid);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.SubscriptionGuid.Should().Be(subscriptionGuid);
            result.ErrorCode.Should().Be(ErrorCodes.SubscriptionNotFound);
            result.Message.Should().Contain("not found");
            mockSubscriptionManager.Verify(x => x.Get(subscriptionGuid), Times.Once);
            mockObservableMetric.Verify(
                x => x.RecordMetric(It.IsAny<string>(), It.IsAny<IMetric>(), It.IsAny<IDictionary<string, string>?>()),
                Times.Never);
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

            mockSubscriptionManager
                .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
                .Returns(true);

            var removedEvent = new TaskCompletionSource<Guid>();
            mockSubscriptionManager
                .Setup(x => x.Remove(It.IsAny<Guid>()))
                .Callback((Guid guid) => removedEvent.TrySetResult(guid));

            // The consumer's "StartConsumingAsync" task represents the WebSocket connection.
            // Once the connection is established (tcs is signaled), the returned task later
            // faults to simulate a disconnection / streaming exception.
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

            // Simulate the WebSocket consumer faulting (e.g. disconnection/exception).
            consumerTaskSource.SetException(new InvalidOperationException("Simulated disconnection"));

            // Wait for the background monitoring task to observe the fault and remove the subscription.
            var completedTask = await Task.WhenAny(removedEvent.Task, Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert
            completedTask.Should().Be(removedEvent.Task, "the subscription should be removed from the manager after the consumer task faults");
            var removedGuid = await removedEvent.Task;
            removedGuid.Should().Be(result.SubscriptionGuid!.Value);

            mockSubscriptionManager.Verify(x => x.Remove(result.SubscriptionGuid!.Value), Times.Once);
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WhenMultipleConsumerTasksFault_RemovesSubscriptionExactlyOnce()
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
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            mockSubscriptionManager
                .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
                .Returns(true);

            var removeCallCount = 0;
            var removedEvent = new TaskCompletionSource<Guid>();
            mockSubscriptionManager
                .Setup(x => x.Remove(It.IsAny<Guid>()))
                .Callback((Guid guid) =>
                {
                    Interlocked.Increment(ref removeCallCount);
                    removedEvent.TrySetResult(guid);
                });

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

            // Simulate both consumers faulting (e.g. simultaneous disconnections).
            consumerTaskSource1.SetException(new InvalidOperationException("Simulated disconnection 1"));
            consumerTaskSource2.SetException(new InvalidOperationException("Simulated disconnection 2"));

            var completedTask = await Task.WhenAny(removedEvent.Task, Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert
            completedTask.Should().Be(removedEvent.Task, "the subscription should be removed after all consumer tasks fault");
            var removedGuid = await removedEvent.Task;
            removedGuid.Should().Be(result.SubscriptionGuid!.Value);

            // Give a small grace period to ensure no duplicate Remove calls occur.
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            removeCallCount.Should().Be(1, "Remove should only be called once even when multiple consumer tasks fault");
            mockSubscriptionManager.Verify(x => x.Remove(result.SubscriptionGuid!.Value), Times.Once);
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WhenConsumerTaskCompletesNormally_RemovesSubscriptionFromManager()
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

            mockSubscriptionManager
                .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
                .Returns(true);

            var removedEvent = new TaskCompletionSource<Guid>();
            mockSubscriptionManager
                .Setup(x => x.Remove(It.IsAny<Guid>()))
                .Callback((Guid guid) => removedEvent.TrySetResult(guid));

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

            // Simulate the WebSocket consumer completing normally, without any error.
            consumerTaskSource.SetResult();

            var completedTask = await Task.WhenAny(removedEvent.Task, Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert
            completedTask.Should().Be(removedEvent.Task, "the subscription should be removed once the consumer task completes, even without a fault");
            var removedGuid = await removedEvent.Task;
            removedGuid.Should().Be(result.SubscriptionGuid!.Value);

            mockSubscriptionManager.Verify(x => x.Remove(result.SubscriptionGuid!.Value), Times.Once);
        }

        [Fact]
        public async Task StartLevel1SubscriptionAsync_WhenOneOfMultipleSubscriptionsFaults_OnlyRemovesFaultedSubscription()
        {
            // Arrange
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

            var trackedSubscriptions = new List<SubscriptionGroup>();

            mockSubscriptionManager
                .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
                .Returns((SubscriptionGroup group) =>
                {
                    lock (trackedSubscriptions)
                    {
                        trackedSubscriptions.Add(group);
                    }
                    return true;
                });

            mockSubscriptionManager
                .Setup(x => x.Get())
                .Returns(() =>
                {
                    lock (trackedSubscriptions)
                    {
                        return trackedSubscriptions.ToList();
                    }
                });

            var removedGuids = new List<Guid>();
            var removedEvent = new TaskCompletionSource<Guid>();
            mockSubscriptionManager
                .Setup(x => x.Remove(It.IsAny<Guid>()))
                .Callback((Guid guid) =>
                {
                    lock (trackedSubscriptions)
                    {
                        trackedSubscriptions.RemoveAll(s => s.Guid == guid);
                    }
                    lock (removedGuids)
                    {
                        removedGuids.Add(guid);
                    }
                    removedEvent.TrySetResult(guid);
                });

            // Consumer for subscription 1 will fault; consumer for subscription 2 stays pending (healthy).
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

            // Simulate a disconnection/exception only for the first subscription's consumer.
            faultingConsumerTaskSource.SetException(new InvalidOperationException("Simulated disconnection"));

            var completedTask = await Task.WhenAny(removedEvent.Task, Task.Delay(TimeSpan.FromSeconds(5)));

            // Assert
            completedTask.Should().Be(removedEvent.Task, "the faulted subscription should be removed");

            // Give a small grace period to ensure the healthy subscription is not also removed.
            await Task.Delay(TimeSpan.FromMilliseconds(200));

            removedGuids.Should().ContainSingle().Which.Should().Be(result1.SubscriptionGuid!.Value);
            mockSubscriptionManager.Verify(x => x.Remove(result1.SubscriptionGuid!.Value), Times.Once);
            mockSubscriptionManager.Verify(x => x.Remove(result2.SubscriptionGuid!.Value), Times.Never);

            // Validate against the real public API: the faulted subscription's id should
            // no longer be present in the manager, while the healthy one should still be active.
            var activeSubscriptions = canaryService.GetActiveSubscriptions();
            activeSubscriptions.Should().HaveCount(1, "only the healthy subscription should remain after the faulted one is removed");
            activeSubscriptions.Select(s => s.Guid).Should().NotContain(result1.SubscriptionGuid!.Value);
            activeSubscriptions.Single().Guid.Should().Be(result2.SubscriptionGuid!.Value);

            // Cleanup: complete the healthy consumer task so its background monitoring task doesn't linger.
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
        public async Task StartLevel2SubscriptionAsync_UsesCreateLevel2Async_NotCreateAsync()
        {
            // Arrange
            var request = new StartSubscriptionRequest { DurationSeconds = 30 };
            var streamResult = CreateStreamResult(HttpStatusCode.OK, new List<string> { "wss://test.com/stream1" });

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateLevel2Async(request))
                .ReturnsAsync(streamResult);

            SetupTryAddSuccess();
            SetupSuccessfulConsumer();

            // Act
            await canaryService.StartLevel2SubscriptionAsync(request);

            // Assert
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
            // Simulates a Guid collision or any other rejection by the manager: TryAdd returns false,
            // meaning the subscription was never actually stored.
            var request = new StartSubscriptionRequest { DurationSeconds = 60 };
            var streamResult = CreateStreamResult(HttpStatusCode.OK, new List<string> { "wss://test.com/stream1" });

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            mockSubscriptionManager
                .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
                .Returns(false);

            // GetActiveSubscriptions reflects whatever is actually tracked by the manager.
            mockSubscriptionManager
                .Setup(x => x.Get())
                .Returns(new List<SubscriptionGroup>());

            SetupSuccessfulConsumer();

            // Act
            var result = await canaryService.StartLevel1SubscriptionAsync(request);
            var activeSubscriptions = canaryService.GetActiveSubscriptions();

            // Assert
            // The service now surfaces a failure response (instead of a misleading "success" with a
            // SubscriptionGuid) when TryAdd fails, and GetActiveSubscriptions correctly shows no active
            // subscriptions since it was never stored.
            result.SubscriptionGuid.Should().BeNull();
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            activeSubscriptions.Should().BeEmpty("the subscription was never actually added to the manager");
        }

        [Fact]
        public async Task StopSubscriptionAsync_WhenConsumerDoesNotHonorCancellation_SubscriptionIsRemovedFromActiveSubscriptionsImmediately()
        {
            // Arrange
            // The manager is backed by an in-memory dictionary to mimic real Add/Get/Remove semantics.
            // StopSubscriptionAsync should remove the subscription immediately, regardless of whether the
            // consumer honors cancellation or the background monitor has had a chance to run.
            var trackedSubscriptions = new Dictionary<Guid, SubscriptionGroup>();

            mockSubscriptionManager
                .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
                .Returns((SubscriptionGroup g) => trackedSubscriptions.TryAdd(g.Guid, g));

            mockSubscriptionManager
                .Setup(x => x.Get())
                .Returns(() => trackedSubscriptions.Values.ToList());

            mockSubscriptionManager
                .Setup(x => x.Get(It.IsAny<Guid>()))
                .Returns((Guid guid) => trackedSubscriptions.TryGetValue(guid, out var g)
                    ? g
                    : throw new InvalidOperationException($"Subscription does not exist {guid}"));

            mockSubscriptionManager
                .Setup(x => x.Remove(It.IsAny<Guid>()))
                .Callback((Guid guid) => trackedSubscriptions.Remove(guid));

            var request = new StartSubscriptionRequest { DurationSeconds = 60 };
            var streamResult = CreateStreamResult(HttpStatusCode.OK, new List<string> { "wss://test.com/stream1" });

            mockStreamSubscriptionFactory
                .Setup(x => x.CreateAsync(request))
                .ReturnsAsync(streamResult);

            // This consumer's task never completes, simulating a consumer that ignores the
            // cancellation token requested by StopSubscriptionAsync.
            var neverCompletingTask = new TaskCompletionSource();
            var mockConsumer = new Mock<IWebSocketConsumer>();
            mockConsumer
                .Setup(x => x.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
                .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) => tcs.SetResult(true))
                .Returns(neverCompletingTask.Task);

            mockWebSocketConsumerFactory
                .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
                .Returns(mockConsumer.Object);

            // Act
            var startResult = await canaryService.StartLevel1SubscriptionAsync(request);
            var stopResult = await canaryService.StopSubscriptionAsync(startResult.SubscriptionGuid!.Value);
            var activeSubscriptions = canaryService.GetActiveSubscriptions();

            // Assert
            // StopSubscriptionAsync now removes the subscription from the manager immediately after
            // cancelling the token, so it no longer appears as "active" even though the misbehaving
            // consumer's task never actually completes.
            stopResult.Success.Should().BeTrue();
            activeSubscriptions.Should().NotContain(s => s.Guid == startResult.SubscriptionGuid!.Value,
                "the subscription should be removed immediately on stop, without waiting for the consumer task to finish");
        }
    }
}
