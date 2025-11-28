using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Domain;
using System.Net;

namespace Morningstar.Streaming.Client.Tests.ClientTests
{
    public class StreamingApiClientTests
    {
        private readonly Mock<IApiHelper> mockApiHelper;
        private readonly Mock<ITokenProvider> mockTokenProvider;
        private readonly Mock<ILogger<StreamingApiClient>> mockLogger;
        private readonly StreamingApiClient streamingApiClient;

        public StreamingApiClientTests()
        {
            // Arrange - Initialize mocks
            mockApiHelper = new Mock<IApiHelper>();
            mockTokenProvider = new Mock<ITokenProvider>();
            mockLogger = new Mock<ILogger<StreamingApiClient>>();

            // Setup default token provider behavior
            mockTokenProvider
                .Setup(x => x.CreateBearerTokenAsync())
                .ReturnsAsync("Bearer test-token-12345");

            // System Under Test
            streamingApiClient = new StreamingApiClient(
                mockApiHelper.Object,
                mockLogger.Object,
                mockTokenProvider.Object
            );
        }

        [Fact]
        public async Task CreateL1StreamAsync_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var testRequest = new { Investments = new[] { "Perf-123", "Perf-345" } };
            var endpointUrl = "https://api.test.com/stream/level1";

            var expectedResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.OK,
                Subscriptions = new Subscription
                {
                    Realtime = new List<string> { "wss://stream1.test.com", "wss://stream2.test.com" }
                }
            };

            mockApiHelper
                .Setup(x => x.ProcessRequestAsync<StreamResponse>(
                    endpointUrl,
                    HttpMethod.Post,
                    It.IsAny<List<KeyValuePair<string, string>>>(),
                    testRequest))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await streamingApiClient.CreateL1StreamAsync(testRequest, endpointUrl);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Subscriptions.Should().NotBeNull();
            result.Subscriptions!.Realtime.Should().HaveCount(2);
            result.Subscriptions.Realtime.Should().Contain("wss://stream1.test.com");
            result.Subscriptions.Realtime.Should().Contain("wss://stream2.test.com");

            mockApiHelper.Verify(x => x.ProcessRequestAsync<StreamResponse>(
                endpointUrl,
                HttpMethod.Post,
                It.IsAny<List<KeyValuePair<string, string>>>(),
                testRequest), Times.Once);
        }

        [Fact]
        public async Task CreateL1StreamAsync_WithBadRequest_ReturnsBadRequestResponse()
        {
            // Arrange
            var testRequest = new { Investments = Array.Empty<string>() };
            var endpointUrl = "https://api.test.com/stream/level1";

            var expectedResponse = new StreamResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorCode = "INVALID_REQUEST",
                Message = "No investments provided"
            };

            mockApiHelper
                .Setup(x => x.ProcessRequestAsync<StreamResponse>(
                    endpointUrl,
                    HttpMethod.Post,
                    It.IsAny<List<KeyValuePair<string, string>>>(),
                    testRequest))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await streamingApiClient.CreateL1StreamAsync(testRequest, endpointUrl);

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.ErrorCode.Should().Be("INVALID_REQUEST");
            result.Message.Should().Be("No investments provided");
        }


        [Fact]
        public async Task CreateL1StreamAsync_WhenExceptionOccurs_LogsErrorAndThrows()
        {
            // Arrange
            var testRequest = new { Investments = new[] { "MSFT" } };
            var endpointUrl = "https://api.test.com/stream/level1";
            var expectedException = new HttpRequestException("Network error");

            mockApiHelper
                .Setup(x => x.ProcessRequestAsync<StreamResponse>(
                    It.IsAny<string>(),
                    It.IsAny<HttpMethod>(),
                    It.IsAny<List<KeyValuePair<string, string>>>(),
                    It.IsAny<object>()))
                .ThrowsAsync(expectedException);

            // Act
            Func<Task> act = async () => await streamingApiClient.CreateL1StreamAsync(testRequest, endpointUrl);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>()
                .WithMessage("Network error");

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error when attempting to request L1 Stream")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SubscribeAsync_WithImmediateCancellation_CompletesWithoutException()
        {
            // Arrange
            var webSocketUrl = "wss://test.com/stream";
            var messageReceived = false;

            Func<string, Task> onMessageAsync = async (message) =>
            {
                messageReceived = true;
                await Task.CompletedTask;
            };

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync(); // Cancel immediately before calling

            // Act
            await streamingApiClient.SubscribeAsync(webSocketUrl, onMessageAsync, cts.Token);

            // Assert
            messageReceived.Should().BeFalse();
        }

        [Fact]
        public async Task SubscribeAsync_LogsConnectionAttempt_WhenConnecting()
        {
            // Arrange
            var webSocketUrl = "wss://invalid.test.example.com/stream";
            Func<string, Task> onMessageAsync = async (message) => await Task.CompletedTask;

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(50);

            // Act
            try
            {
                await streamingApiClient.SubscribeAsync(webSocketUrl, onMessageAsync, cts.Token);
            }
            catch
            {
                // Expected - connection will fail or be cancelled
            }

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Connecting WebSocket to")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce(),
                "should log connection attempt");
        }


        [Fact]
        public async Task SubscribeAsync_WithFailedConnection_RetriesAndLogsWarning()
        {
            // Arrange
            var webSocketUrl = "wss://invalid-domain-that-does-not-exist.test/stream";
            Func<string, Task> onMessageAsync = async (message) => await Task.CompletedTask;

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(450); // Allow time for at least one retry attempt

            // Act
            await streamingApiClient.SubscribeAsync(webSocketUrl, onMessageAsync, cts.Token);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("WebSocket failed") && v.ToString()!.Contains("Reconnecting")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce(),
               "should log warning about failed connection and retry");
        }
    }
}
