using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services.Counter;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Client.Services.WebSockets;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Constants;

namespace Morningstar.Streaming.Client.Tests.ServiceTests
{
    public class WebSocketConsumerFactoryTests
    {
        private readonly Mock<ILogger<WebSocketConsumer>> mockLogger;
        private readonly Mock<ICounterLogger> mockCounterLogger;
        private readonly Mock<IWebSocketLoggerFactory> mockWsLoggerFactory;
        private readonly Mock<IStreamingApiClient> mockClient;
        private readonly Mock<IObservableMetric<IMetric>> mockObservableMetric;
        private readonly WebSocketConsumerFactory webSocketConsumerFactory;

        public WebSocketConsumerFactoryTests()
        {
            // Arrange - Initialize mocks
            mockLogger = new Mock<ILogger<WebSocketConsumer>>();
            mockCounterLogger = new Mock<ICounterLogger>();
            mockWsLoggerFactory = new Mock<IWebSocketLoggerFactory>();
            mockClient = new Mock<IStreamingApiClient>();
            mockObservableMetric = new Mock<IObservableMetric<IMetric>>();

            // Setup default WebSocketLoggerFactory behavior
            var mockEventsLogger = new Mock<ILogger>();
            mockWsLoggerFactory
                .Setup(x => x.GetLogger(It.IsAny<Guid>()))
                .Returns(mockEventsLogger.Object);

            // System Under Test
            webSocketConsumerFactory = new WebSocketConsumerFactory(
                mockLogger.Object,
                mockCounterLogger.Object,
                mockWsLoggerFactory.Object,
                mockClient.Object,
                mockObservableMetric.Object
            );
        }

        [Fact]
        public void Create_WithValidParameters_ReturnsWebSocketConsumer()
        {
            // Arrange
            var wsUrl = "wss://test.com/stream/12345678-1234-1234-1234-123456789012";
            var logToFile = true;

            // Act
            var result = webSocketConsumerFactory.Create(wsUrl, logToFile);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IWebSocketConsumer>();
            result.Should().BeOfType<WebSocketConsumer>();
        }

        [Fact]
        public void Create_WithLogToFileTrue_ReturnsConsumerWithLogging()
        {
            // Arrange
            var wsUrl = "wss://test.com/stream/abcdef12-3456-7890-abcd-ef1234567890";
            var logToFile = true;

            // Act
            var result = webSocketConsumerFactory.Create(wsUrl, logToFile);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<WebSocketConsumer>();
        }

        [Fact]
        public void Create_WithLogToFileFalse_ReturnsConsumerWithoutLogging()
        {
            // Arrange
            var wsUrl = "wss://test.com/stream/99999999-8888-7777-6666-555544443333";
            var logToFile = false;

            // Act
            var result = webSocketConsumerFactory.Create(wsUrl, logToFile);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<WebSocketConsumer>();
        }

        [Fact]
        public void Create_WithDifferentUrls_CreatesUniqueConsumers()
        {
            // Arrange
            var wsUrl1 = "wss://test.com/stream/11111111-1111-1111-1111-111111111111";
            var wsUrl2 = "wss://test.com/stream/22222222-2222-2222-2222-222222222222";

            // Act
            var consumer1 = webSocketConsumerFactory.Create(wsUrl1, true);
            var consumer2 = webSocketConsumerFactory.Create(wsUrl2, false);

            // Assert
            consumer1.Should().NotBeNull();
            consumer2.Should().NotBeNull();
            consumer1.Should().NotBeSameAs(consumer2);
        }

        [Fact]
        public void Create_WithShortUrl_ReturnsConsumer()
        {
            // Arrange
            var wsUrl = "wss://test.com/abc";
            var logToFile = false;

            // Act
            var result = webSocketConsumerFactory.Create(wsUrl, logToFile);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<WebSocketConsumer>();
        }

        [Fact]
        public void Create_WithUrlContainingGuid_ReturnsConsumer()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var wsUrl = $"wss://stream.example.com/topics/{guid}";
            var logToFile = true;

            // Act
            var result = webSocketConsumerFactory.Create(wsUrl, logToFile);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<WebSocketConsumer>();
        }

        [Fact]
        public void Create_MultipleCallsWithSameUrl_CreatesNewInstancesEachTime()
        {
            // Arrange
            var wsUrl = "wss://test.com/stream/55555555-5555-5555-5555-555555555555";
            var logToFile = true;

            // Act
            var consumer1 = webSocketConsumerFactory.Create(wsUrl, logToFile);
            var consumer2 = webSocketConsumerFactory.Create(wsUrl, logToFile);

            // Assert
            consumer1.Should().NotBeNull();
            consumer2.Should().NotBeNull();
            consumer1.Should().NotBeSameAs(consumer2, "factory should create new instances each time");
        }
    }
}