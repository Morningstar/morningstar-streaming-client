using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Morningstar.Streaming.Client.Services;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Client.Services.WebSockets;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Constants;
using Morningstar.Streaming.Domain.Contracts;
using Morningstar.Streaming.Domain.Models;
using System.Net;

namespace Morningstar.Streaming.Client.Tests.ServiceTests;

public class CanaryServiceTests
{
    private readonly Mock<ISubscriptionGroupManager> mockSubscriptionManager;
    private readonly Mock<IStreamSubscriptionFactory> mockStreamSubscriptionFactory;
    private readonly Mock<IWebSocketConsumerFactory> mockWebSocketConsumerFactory;
    private readonly Mock<ILogger<CanaryService>> mockLogger;
    private readonly Mock<IOptions<AppConfig>> mockAppConfig;
    private readonly CanaryService canaryService;

    public CanaryServiceTests()
    {
        // Arrange - Initialize mocks
        mockSubscriptionManager = new Mock<ISubscriptionGroupManager>();
        mockStreamSubscriptionFactory = new Mock<IStreamSubscriptionFactory>();
        mockWebSocketConsumerFactory = new Mock<IWebSocketConsumerFactory>();
        mockLogger = new Mock<ILogger<CanaryService>>();
        mockAppConfig = new Mock<IOptions<AppConfig>>();

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
            mockAppConfig.Object
        );
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

        var mockConsumer = new Mock<IWebSocketConsumer>();
        mockConsumer
            .Setup(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockWebSocketConsumerFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockConsumer.Object);

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
        var expectedCancellationTokenSource = new CancellationTokenSource();

        var streamResult = new StreamSubscriptionResult
        {
            ApiResponse = new StreamResponse { StatusCode = HttpStatusCode.PartialContent },
            WebSocketUrls = expectedWebSocketUrls,
            CancellationTokenSource = expectedCancellationTokenSource
        };

        mockStreamSubscriptionFactory
            .Setup(x => x.CreateAsync(request))
            .ReturnsAsync(streamResult);

        mockSubscriptionManager
            .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
            .Returns(true);

        var mockConsumer = new Mock<IWebSocketConsumer>();
        mockConsumer
            .Setup(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockWebSocketConsumerFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockConsumer.Object);

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

        var mockConsumer = new Mock<IWebSocketConsumer>();
        mockConsumer
            .Setup(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockWebSocketConsumerFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockConsumer.Object);

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

        var mockConsumer = new Mock<IWebSocketConsumer>();
        mockConsumer
            .Setup(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockWebSocketConsumerFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockConsumer.Object);

        // Act
        var result = await canaryService.StartLevel1SubscriptionAsync(request);

        // Allow some time for background tasks to start
        await Task.Delay(100);

        // Assert
        mockWebSocketConsumerFactory.Verify(
            x => x.Create(It.IsAny<string>(), false),
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
            mockAppConfig.Object
        );

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

        var mockConsumer = new Mock<IWebSocketConsumer>();
        mockConsumer
            .Setup(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockWebSocketConsumerFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockConsumer.Object);

        // Act
        var result = await sutWithLogging.StartLevel1SubscriptionAsync(request);

        // Allow some time for background tasks to start
        await Task.Delay(100);

        // Assert
        mockWebSocketConsumerFactory.Verify(
            x => x.Create(It.IsAny<string>(), true),
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
            CancellationTokenSource = cancellationTokenSource
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
    public async Task StartLevel1SubscriptionAsync_AddsSubscriptionToManagerBeforeStartingConsumers()
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

        var addCalled = false;
        mockSubscriptionManager
            .Setup(x => x.TryAdd(It.IsAny<SubscriptionGroup>()))
            .Callback(() => addCalled = true)
            .Returns(true);

        var mockConsumer = new Mock<IWebSocketConsumer>();
        mockConsumer
            .Setup(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .Callback(() => addCalled.Should().BeTrue("TryAdd should be called before starting consumers"))
            .Returns(Task.CompletedTask);

        mockWebSocketConsumerFactory
            .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockConsumer.Object);

        // Act
        var result = await canaryService.StartLevel1SubscriptionAsync(request);

        // Assert
        addCalled.Should().BeTrue();
    }
}
