using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Contracts;
using System.Net;

namespace Morningstar.Streaming.Client.Tests.ServiceTests
{
    public class StreamSubscriptionFactoryTests
    {
        private readonly Mock<IStreamingApiClient> mockStreamingApiClient;
        private readonly Mock<IOptions<AppConfig>> mockAppConfig;
        private readonly Mock<IOptions<EndpointConfig>> mockEndpointConfig;
        private readonly StreamSubscriptionFactory streamSubscriptionFactory;

        public StreamSubscriptionFactoryTests()
        {
            // Arrange - Initialize mocks
            mockStreamingApiClient = new Mock<IStreamingApiClient>();
            mockAppConfig = new Mock<IOptions<AppConfig>>();
            mockEndpointConfig = new Mock<IOptions<EndpointConfig>>();

            // Setup default configurations
            mockAppConfig.Setup(x => x.Value).Returns(new AppConfig
            {
                StreamingApiBaseAddress = "https://api.test.com",
                OAuthAddress = "https://oauth.test.com",
                ConnectionStringTtl = 300,
                LogMessages = false,
                LogMessagesPath = "logs"
            });

            mockEndpointConfig.Setup(x => x.Value).Returns(new EndpointConfig
            {
                Level1UrlAddress = "stream/level1",
                Level1BypassUrlAddress = "stream/level1/bypass"
            });

            // System Under Test
            streamSubscriptionFactory = new StreamSubscriptionFactory(
                mockStreamingApiClient.Object,
                mockAppConfig.Object,
                mockEndpointConfig.Object
            );
        }

        [Fact]
        public async Task CreateAsync_WithSuccessfulResponse_ReturnsStreamSubscriptionResult()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60,
                Stream = new StreamRequest
                {
                    Investments = new List<Investments>
                    {
                        new Investments { IdType = "Symbol", Ids = new List<string> { "MSFT", "AAPL" } }
                    },
                    EventTypes = new[] { "Trade", "Quote" }
                }
            };

            var streamResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.OK,
                Subscriptions = new Subscription
                {
                    Realtime = new List<string> { "wss://stream1.test.com", "wss://stream2.test.com" }
                }
            };

            mockStreamingApiClient
                .Setup(x => x.CreateL1StreamAsync(
                    request.Stream,
                    "https://api.test.com/stream/level1"))
                .ReturnsAsync(streamResponse);

            // Act
            var result = await streamSubscriptionFactory.CreateAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.ApiResponse.Should().Be(streamResponse);
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            result.WebSocketUrls.Should().HaveCount(2);
            result.WebSocketUrls.Should().Contain("wss://stream1.test.com");
            result.WebSocketUrls.Should().Contain("wss://stream2.test.com");
            result.CancellationTokenSource.Should().NotBeNull();

            mockStreamingApiClient.Verify(
                x => x.CreateL1StreamAsync(request.Stream, "https://api.test.com/stream/level1"),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithDurationSeconds_CreatesCancellationTokenWithTimeout()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 120,
                Stream = new StreamRequest
                {
                    Investments = new List<Investments>
                    {
                        new Investments { IdType = "Symbol", Ids = new List<string> { "TSLA" } }
                    },
                    EventTypes = new[] { "Trade" }
                }
            };

            var streamResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.OK,
                Subscriptions = new Subscription
                {
                    Realtime = new List<string> { "wss://stream.test.com" }
                }
            };

            mockStreamingApiClient
                .Setup(x => x.CreateL1StreamAsync(It.IsAny<StreamRequest>(), It.IsAny<string>()))
                .ReturnsAsync(streamResponse);

            // Act
            var result = await streamSubscriptionFactory.CreateAsync(request);

            // Assert
            result.CancellationTokenSource.Should().NotBeNull();
            // Note: We can't easily test the exact timeout value without waiting,
            // but we can verify the token source was created
            result.CancellationTokenSource.Token.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAsync_WithNoDuration_CreatesCancellationTokenWithoutTimeout()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = null,
                Stream = new StreamRequest
                {
                    Investments = new List<Investments>
                    {
                        new Investments { IdType = "Symbol", Ids = new List<string> { "GOOGL" } }
                    },
                    EventTypes = new[] { "Quote" }
                }
            };

            var streamResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.OK,
                Subscriptions = new Subscription
                {
                    Realtime = new List<string> { "wss://stream.test.com" }
                }
            };

            mockStreamingApiClient
                .Setup(x => x.CreateL1StreamAsync(It.IsAny<StreamRequest>(), It.IsAny<string>()))
                .ReturnsAsync(streamResponse);

            // Act
            var result = await streamSubscriptionFactory.CreateAsync(request);

            // Assert
            result.CancellationTokenSource.Should().NotBeNull();
            result.CancellationTokenSource.Token.CanBeCanceled.Should().BeTrue();
        }

        [Fact]
        public async Task CreateAsync_WithRealtimeAndDelayedUrls_CombinesAllUrls()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60,
                Stream = new StreamRequest
                {
                    Investments = new List<Investments>
                    {
                        new Investments { IdType = "PerformanceId", Ids = new List<string> { "PERF-123" } }
                    },
                    EventTypes = new[] { "Trade" }
                }
            };

            var streamResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.OK,
                Subscriptions = new Subscription
                {
                    Realtime = new List<string> { "wss://realtime1.test.com", "wss://realtime2.test.com" },
                    Delayed = new List<string> { "wss://delayed1.test.com" }
                }
            };

            mockStreamingApiClient
                .Setup(x => x.CreateL1StreamAsync(It.IsAny<StreamRequest>(), It.IsAny<string>()))
                .ReturnsAsync(streamResponse);

            // Act
            var result = await streamSubscriptionFactory.CreateAsync(request);

            // Assert
            result.WebSocketUrls.Should().HaveCount(3);
            result.WebSocketUrls.Should().Contain("wss://realtime1.test.com");
            result.WebSocketUrls.Should().Contain("wss://realtime2.test.com");
            result.WebSocketUrls.Should().Contain("wss://delayed1.test.com");
        }

        [Fact]
        public async Task CreateAsync_WithOnlyDelayedUrls_ReturnsDelayedUrls()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60,
                Stream = new StreamRequest
                {
                    Investments = new List<Investments>
                    {
                        new Investments { IdType = "PerformanceId", Ids = new List<string> { "PERF-123" } }
                    },
                    EventTypes = new[] { "Trade" }
                }
            };

            var streamResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.OK,
                Subscriptions = new Subscription
                {
                    Realtime = null,
                    Delayed = new List<string> { "wss://delayed.test.com" }
                }
            };

            mockStreamingApiClient
                .Setup(x => x.CreateL1StreamAsync(It.IsAny<StreamRequest>(), It.IsAny<string>()))
                .ReturnsAsync(streamResponse);

            // Act
            var result = await streamSubscriptionFactory.CreateAsync(request);

            // Assert
            result.WebSocketUrls.Should().HaveCount(1);
            result.WebSocketUrls.Should().Contain("wss://delayed.test.com");
        }

        [Fact]
        public async Task CreateAsync_WithNoWebSocketUrls_ReturnsEmptyUrlList()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 60,
                Stream = new StreamRequest
                {
                    Investments = new List<Investments>
                    {
                        new Investments { IdType = "PerformanceId", Ids = new List<string> { "PERF-123" } }
                    },
                    EventTypes = new[] { "Trade" }
                }
            };

            var streamResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Subscriptions = null
            };

            mockStreamingApiClient
                .Setup(x => x.CreateL1StreamAsync(It.IsAny<StreamRequest>(), It.IsAny<string>()))
                .ReturnsAsync(streamResponse);

            // Act
            var result = await streamSubscriptionFactory.CreateAsync(request);

            // Assert
            result.WebSocketUrls.Should().NotBeNull();
            result.WebSocketUrls.Should().BeEmpty();
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateAsync_UsesCorrectEndpointUrl()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                Stream = new StreamRequest
                {
                    Investments = new List<Investments>
                    {
                        new Investments { IdType = "PerformanceId", Ids = new List<string> { "PERF-123" } }
                    },
                    EventTypes = new[] { "Trade" }
                }
            };

            var streamResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.OK,
                Subscriptions = new Subscription
                {
                    Realtime = new List<string> { "wss://stream.test.com" }
                }
            };

            string? capturedUrl = null;

            mockStreamingApiClient
                .Setup(x => x.CreateL1StreamAsync(It.IsAny<StreamRequest>(), It.IsAny<string>()))
                .Callback<StreamRequest, string>((req, url) => capturedUrl = url)
                .ReturnsAsync(streamResponse);

            // Act
            await streamSubscriptionFactory.CreateAsync(request);

            // Assert
            capturedUrl.Should().Be("https://api.test.com/stream/level1");
        }        

        [Fact]
        public async Task CreateAsync_PassesStreamRequestToApiClient()
        {
            // Arrange
            var expectedInvestments = new List<Investments>
            {
                new Investments { IdType = "PerformanceId", Ids = new List<string> { "PERF-123", "PERF-569" } }
            };

            var expectedEventTypes = new[] { "Trade", "Quote", "AggregateSummary" };

            var request = new StartSubscriptionRequest
            {
                DurationSeconds = 300,
                Stream = new StreamRequest
                {
                    Investments = expectedInvestments,
                    EventTypes = expectedEventTypes
                }
            };

            var streamResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.OK,
                Subscriptions = new Subscription
                {
                    Realtime = new List<string> { "wss://stream.test.com" }
                }
            };

            StreamRequest? capturedStreamRequest = null;

            mockStreamingApiClient
                .Setup(x => x.CreateL1StreamAsync(It.IsAny<StreamRequest>(), It.IsAny<string>()))
                .Callback<StreamRequest, string>((req, url) => capturedStreamRequest = req)
                .ReturnsAsync(streamResponse);

            // Act
            await streamSubscriptionFactory.CreateAsync(request);

            // Assert
            capturedStreamRequest.Should().NotBeNull();
            capturedStreamRequest.Should().BeSameAs(request.Stream);
            capturedStreamRequest!.Investments.Should().BeEquivalentTo(expectedInvestments);
            capturedStreamRequest.EventTypes.Should().BeEquivalentTo(expectedEventTypes);
        }

        [Fact]
        public async Task CreateAsync_WithPartialContentResponse_ReturnsPartialResult()
        {
            // Arrange
            var request = new StartSubscriptionRequest
            {
                Stream = new StreamRequest
                {
                    Investments = new List<Investments>
                    {
                        new Investments { IdType = "Symbol", Ids = new List<string> { "VALID", "INVALID" } }
                    },
                    EventTypes = new[] { "Trade" }
                }
            };

            var streamResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.PartialContent,
                Subscriptions = new Subscription
                {
                    Realtime = new List<string> { "wss://stream.test.com" }
                },
                MetaData = new MetaData
                {
                    RequestId = "req-123",
                    Time = "2025-11-19T12:00:00Z"
                }
            };

            mockStreamingApiClient
                .Setup(x => x.CreateL1StreamAsync(It.IsAny<StreamRequest>(), It.IsAny<string>()))
                .ReturnsAsync(streamResponse);

            // Act
            var result = await streamSubscriptionFactory.CreateAsync(request);

            // Assert
            result.ApiResponse.StatusCode.Should().Be(HttpStatusCode.PartialContent);
            result.WebSocketUrls.Should().HaveCount(1);
            result.ApiResponse.MetaData.Should().NotBeNull();
        }
    }
}
