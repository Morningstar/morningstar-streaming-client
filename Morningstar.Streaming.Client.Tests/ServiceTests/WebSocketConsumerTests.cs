using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services.Counter;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Client.Services.WebSockets;

namespace Morningstar.Streaming.Client.Tests.ServiceTests;

public class WebSocketConsumerTests
{
    private readonly Mock<ICounterLogger> mockCounterLogger;
    private readonly Mock<IWebSocketLoggerFactory> mockWsLoggerFactory;
    private readonly Mock<ILogger<WebSocketConsumer>> mockLogger;
    private readonly Mock<IStreamingApiClient> mockClient;
    private readonly Mock<ILogger> mockEventsLogger;
    private readonly Mock<IObservableMetric<IMetric>> mockObservableMetric;

    public WebSocketConsumerTests()
    {
        // Arrange - Initialize mocks
        mockCounterLogger = new Mock<ICounterLogger>();
        mockWsLoggerFactory = new Mock<IWebSocketLoggerFactory>();
        mockLogger = new Mock<ILogger<WebSocketConsumer>>();
        mockClient = new Mock<IStreamingApiClient>();
        mockEventsLogger = new Mock<ILogger>();
        mockObservableMetric = new Mock<IObservableMetric<IMetric>>();

        // Setup default WebSocketLoggerFactory behavior
        mockWsLoggerFactory
            .Setup(x => x.GetLogger(It.IsAny<Guid>()))
            .Returns(mockEventsLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var wsUrl = "wss://test.com/stream/12345678-1234-1234-1234-123456789012";
        var logToFile = true;

        // Act
        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        // Assert
        consumer.Should().NotBeNull();
        consumer.Should().BeAssignableTo<IWebSocketConsumer>();
    }

    [Fact]
    public void Constructor_ExtractsGuidFromUrl_CallsWebSocketLoggerFactory()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = false;

        // Act
        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        // Assert
        consumer.Should().NotBeNull();
        mockWsLoggerFactory.Verify(x => x.GetLogger(guid), Times.Once);
    }

    [Fact]
    public void Constructor_WithUrlWithoutValidGuid_CallsWebSocketLoggerFactoryWithEmptyGuid()
    {
        // Arrange
        var wsUrl = "wss://test.com/stream/not-a-valid-guid";
        var logToFile = true;

        // Act
        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        // Assert
        consumer.Should().NotBeNull();
        mockWsLoggerFactory.Verify(x => x.GetLogger(Guid.Empty), Times.Once);
    }

    [Fact]
    public async Task StartConsumingAsync_RegistersSubscription_WithCounterLogger()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = false;

        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel to exit quickly

        // Act
        await consumer.StartConsumingAsync(cts.Token);

        // Assert
        mockCounterLogger.Verify(x => x.RegisterSubscription(guid), Times.Once);
    }

    [Fact]
    public async Task StartConsumingAsync_UnregistersSubscription_AfterCompletion()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = false;

        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel to exit quickly

        // Act
        await consumer.StartConsumingAsync(cts.Token);

        // Assert
        mockCounterLogger.Verify(x => x.UnregisterSubscription(guid), Times.Once);
    }

    [Fact]
    public async Task StartConsumingAsync_CallsClientSubscribeAsync_WithCorrectUrl()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = true;

        mockClient
            .Setup(x => x.SubscribeAsync(
                wsUrl,
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel to exit quickly

        // Act
        await consumer.StartConsumingAsync(cts.Token);

        // Assert
        mockClient.Verify(
            x => x.SubscribeAsync(
                wsUrl,
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartConsumingAsync_CallsClientSubscribeAsync_WithCancellationToken()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = false;

        CancellationToken capturedToken = default;
        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, Task>, CancellationToken>(
                (url, callback, token) => capturedToken = token)
            .Returns(Task.CompletedTask);

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel to exit quickly

        // Act
        await consumer.StartConsumingAsync(cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task StartConsumingAsync_MessageCallback_IncrementsCounter()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = false;

        Func<string, Task>? messageCallback = null;
        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, Task>, CancellationToken>(
                (url, callback, token) => messageCallback = callback)
            .Returns(Task.CompletedTask);

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel to exit quickly

        await consumer.StartConsumingAsync(cts.Token);

        // Act
        messageCallback.Should().NotBeNull();
        await messageCallback!("test message");

        // Give the channel time to process
        await Task.Delay(50);

        // Assert
        mockCounterLogger.Verify(x => x.Increment(guid), Times.Once);
    }

    [Fact]
    public async Task StartConsumingAsync_WithLogToFileTrue_PassesMessagesToChannel()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = true;

        Func<string, Task>? messageCallback = null;
        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, Task>, CancellationToken>(
                (url, callback, token) => messageCallback = callback)
            .Returns(Task.CompletedTask);

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel to exit quickly

        await consumer.StartConsumingAsync(cts.Token);

        // Act
        messageCallback.Should().NotBeNull();
        await messageCallback!("test websocket message");

        await Task.Delay(50); // Give channel time to process

        // Assert
        // When logToFile is true, the consumer will try to log messages from the channel
        // The message should be written to the channel successfully
        mockCounterLogger.Verify(x => x.Increment(guid), Times.Once);
    }

    [Fact]
    public async Task StartConsumingAsync_WithLogToFileFalse_DoesNotLogMessages()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = false;

        Func<string, Task>? messageCallback = null;
        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, Task>, CancellationToken>(
                (url, callback, token) => messageCallback = callback)
            .Returns(Task.CompletedTask);

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel to exit quickly

        await consumer.StartConsumingAsync(cts.Token);

        // Act
        if (messageCallback != null)
        {
            await messageCallback("test message that should not be logged");
        }

        await Task.Delay(50); // Give time for potential logging

        // Assert
        mockEventsLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task StartConsumingAsync_WithCancellation_LogsWarning()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = true;

        var tcs = new TaskCompletionSource<bool>();
        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await tcs.Task;
            });

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();

        // Act
        var consumeTask = consumer.StartConsumingAsync(cts.Token);
        await Task.Delay(50); // Let it start
        await cts.CancelAsync(); // Cancel after starting
        tcs.SetResult(true); // Complete the subscription task

        await consumeTask;

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("LogFromChannelAsync cancelled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartConsumingAsync_RespectsPreCancelledToken()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = false;

        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Pre-cancel

        // Act
        Func<Task> act = async () => await consumer.StartConsumingAsync(cts.Token);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartConsumingAsync_WithMultipleMessages_IncrementsCounterMultipleTimes()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = false;

        Func<string, Task>? messageCallback = null;
        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<string, Task>, CancellationToken>(
                (url, callback, token) => messageCallback = callback)
            .Returns(Task.CompletedTask);

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await consumer.StartConsumingAsync(cts.Token);

        // Act
        messageCallback.Should().NotBeNull();
        await messageCallback!("message 1");
        await messageCallback("message 2");
        await messageCallback("message 3");

        await Task.Delay(50); // Give channel time to process

        // Assert
        mockCounterLogger.Verify(x => x.Increment(guid), Times.Exactly(3));
    }

    [Fact]
    public async Task StartConsumingAsync_ExecutionOrder_RegistersBeforeSubscribing()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = false;
        var callOrder = new List<string>();

        mockCounterLogger
            .Setup(x => x.RegisterSubscription(guid))
            .Callback(() => callOrder.Add("Register"));

        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Subscribe"))
            .Returns(Task.CompletedTask);

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        await consumer.StartConsumingAsync(cts.Token);

        // Assert
        callOrder.Should().HaveCountGreaterOrEqualTo(2);
        callOrder[0].Should().Be("Register", "should register subscription before subscribing");
    }

    [Fact]
    public async Task StartConsumingAsync_ExecutionOrder_UnregistersAfterCompletion()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = false;
        var callOrder = new List<string>();

        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Subscribe"))
            .Returns(Task.CompletedTask);

        mockCounterLogger
            .Setup(x => x.UnregisterSubscription(guid))
            .Callback(() => callOrder.Add("Unregister"));

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        await consumer.StartConsumingAsync(cts.Token);

        // Assert
        callOrder.Should().Contain("Unregister");
        var unregisterIndex = callOrder.IndexOf("Unregister");
        var subscribeIndex = callOrder.IndexOf("Subscribe");
        unregisterIndex.Should().BeGreaterThan(subscribeIndex, "should unregister after subscribing");
    }

    [Fact]
    public async Task StartConsumingAsync_WithUnexpectedDisconnectiion_DiconectionMetricRecored()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var wsUrl = $"wss://test.com/stream/{guid}";
        var logToFile = true;

        mockClient
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(100);
                throw new Exception("WebSocket connection lost unexpectedly");
            });

        var consumer = new WebSocketConsumer(
            mockCounterLogger.Object,
            mockWsLoggerFactory.Object,
            mockLogger.Object,
            mockClient.Object,
            mockObservableMetric.Object,
            wsUrl,
            logToFile
        );

        using var cts = new CancellationTokenSource();

        // Act
        var consumeTask = consumer.StartConsumingAsync(cts.Token);
        await consumeTask;

        // Assert
        mockObservableMetric.Verify(x => x.RecordMetric(
            "WebSocketDisconnections",
            It.IsAny<AtomicLong>(),
            It.IsAny<Dictionary<string, string>>()),
            Times.Once);
    }
}