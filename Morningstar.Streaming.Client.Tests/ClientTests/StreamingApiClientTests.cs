using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services.AvroBinaryDeserializer;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Constants;
using Newtonsoft.Json;

namespace Morningstar.Streaming.Client.Tests.ClientTests
{
    public class StreamingApiClientTests
    {
        private readonly Mock<IApiHelper> mockApiHelper;
        private readonly Mock<ITokenProvider> mockTokenProvider;
        private readonly Mock<ILogger<StreamingApiClient>> mockLogger;
        private readonly Mock<IAvroBinaryDeserializer> mockAvroBinaryDeserializer;
        private readonly StreamingApiClient streamingApiClient;

        public StreamingApiClientTests()
        {
            // Arrange - Initialize mocks
            mockApiHelper = new Mock<IApiHelper>();
            mockTokenProvider = new Mock<ITokenProvider>();
            mockLogger = new Mock<ILogger<StreamingApiClient>>();
            mockAvroBinaryDeserializer = new Mock<IAvroBinaryDeserializer>();

            // Setup default token provider behavior
            mockTokenProvider
                .Setup(x => x.CreateBearerTokenAsync())
                .ReturnsAsync("Bearer test-token-12345");

            // System Under Test
            streamingApiClient = new StreamingApiClient(
                mockApiHelper.Object,
                mockLogger.Object,
                mockTokenProvider.Object,
                mockAvroBinaryDeserializer.Object
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
            var topicGuid = Guid.NewGuid();
            var webSocketUrl = "wss://test.com/stream";
            var messageReceived = false;

            Func<string, Task> onMessageAsync = async (message) =>
            {
                messageReceived = true;
                await Task.CompletedTask;
            };

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync(); // Cancel immediately before calling

            var completed = new TaskCompletionSource<bool>();

            // Act
            var subscribeTask = streamingApiClient.SubscribeAsync(topicGuid, webSocketUrl, null, onMessageAsync, completed, cts.Token);

            await Task.WhenAny(completed.Task, subscribeTask);

            // Assert
            messageReceived.Should().BeFalse();
        }

        [Fact]
        public async Task SubscribeAsync_LogsConnectionAttempt_WhenConnecting()
        {
            // Arrange
            var topicGuid = Guid.NewGuid();
            var webSocketUrl = "wss://invalid.test.example.com/stream";
            Func<string, Task> onMessageAsync = async (message) => await Task.CompletedTask;

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(50);

            var completed = new TaskCompletionSource<bool>();

            // Act            
            var subscribeTask = streamingApiClient.SubscribeAsync(topicGuid, webSocketUrl, null, onMessageAsync, completed, cts.Token);

            await Task.WhenAny(completed.Task, subscribeTask);

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
            var topicGuid = Guid.NewGuid();
            var webSocketUrl = "wss://invalid-domain-that-does-not-exist.test/stream";
            Func<string, Task> onMessageAsync = async (message) => await Task.CompletedTask;

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(750); // Allow time for at least one retry attempt

            var completed = new TaskCompletionSource<bool>();

            // Act
            var subscribeTask = streamingApiClient.SubscribeAsync(topicGuid, webSocketUrl, null, onMessageAsync, completed, cts.Token);

            await Task.WhenAny(completed.Task, subscribeTask);

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

        [Fact]
        public void TryGetExpectedDisconnectType_WithAdminDisconnectEnvelope_ReturnsExpected()
        {
            var jsonMessage = """
                                {
                                    "EventType": "Admin",
                                    "Message": {
                                        "NoticeType": "Disconnect"
                                    }
                                }
                                """;

            var result = StreamingApiClient.TryGetExpectedDisconnectType(jsonMessage, out var disconnectType);

            result.Should().BeTrue();
            disconnectType.Should().Be("Expected");
        }

        [Fact]
        public void TryGetExpectedDisconnectType_WithAvroAdminDisconnectEnvelope_ReturnsExpected()
        {
            var jsonMessage = """
                                {
                                    "EventTypes": ["Admin"],
                                    "Admin": {
                                        "NoticeType": "Disconnect"
                                    }
                                }
                                """;

            var result = StreamingApiClient.TryGetExpectedDisconnectType(jsonMessage, out var disconnectType);

            result.Should().BeTrue();
            disconnectType.Should().Be("Expected");
        }

        [Fact]
        public void GetPendingDisconnectType_WithAdminDisconnectEnvelope_ReturnsExpected()
        {
            var jsonMessage = """
                            {
                                "EventType": "Admin",
                                "Message": {
                                "NoticeType": "Disconnect"
                                }
                            }
                            """;

            var disconnectType = StreamingApiClient.GetPendingDisconnectType(jsonMessage);

            disconnectType.Should().Be("Expected");
        }

        [Fact]
        public void GetPendingDisconnectType_WithOrdinaryMessage_ReturnsUnexpected()
        {
            var jsonMessage = """
                            {
                                "EventType": "Trade",
                                "PublishTime": 123456789
                            }
                            """;

            var disconnectType = StreamingApiClient.GetPendingDisconnectType(jsonMessage);

            disconnectType.Should().Be("Unexpected");
        }

        [Fact]
        public void GetUpdatedPendingDisconnectType_WithExpectedCurrentAndOrdinaryMessage_KeepsExpected()
        {
            var jsonMessage = """
                            {
                                "EventType": "Trade",
                                "PublishTime": 123456789
                            }
                            """;

            var disconnectType = StreamingApiClient.GetUpdatedPendingDisconnectType("Expected", jsonMessage);

            disconnectType.Should().Be("Expected");
        }

        [Fact]
        public void GetUpdatedPendingDisconnectType_WithUnexpectedCurrentAndAdminDisconnect_ReturnsExpected()
        {
            var jsonMessage = """
                            {
                                "EventType": "Admin",
                                "Message": {
                                    "NoticeType": "Disconnect"
                                }
                            }
                            """;

            var disconnectType = StreamingApiClient.GetUpdatedPendingDisconnectType("Unexpected", jsonMessage);

            disconnectType.Should().Be("Expected");
        }

        [Fact]
        public void ShouldArbitrate_WithAdminDisconnectEnvelopeAndArbitrateTrue_ReturnsTrue()
        {
            var jsonMessage = """
                            {
                                "EventType": "Admin",
                                "Message": {
                                    "NoticeType": "Disconnect",
                                    "Arbitrate": true
                                }
                            }
                            """;

            StreamingApiClient.ShouldArbitrate(jsonMessage, out _).Should().BeTrue();
        }

        [Fact]
        public void ShouldArbitrate_WithAdminDisconnectEnvelopeAndArbitrateFalse_ReturnsFalse()
        {
            var jsonMessage = """
                            {
                                "EventType": "Admin",
                                "Message": {
                                    "NoticeType": "Disconnect",
                                    "Arbitrate": false
                                }
                            }
                            """;

            StreamingApiClient.ShouldArbitrate(jsonMessage, out _).Should().BeFalse();
        }

        [Fact]
        public void ShouldArbitrate_WithAdminDisconnectEnvelopeAndNoArbitrateField_ReturnsFalse()
        {
            var jsonMessage = """
                            {
                                "EventType": "Admin",
                                "Message": {
                                    "NoticeType": "Disconnect"
                                }
                            }
                            """;

            StreamingApiClient.ShouldArbitrate(jsonMessage, out _).Should().BeFalse();
        }

        [Fact]
        public void ShouldArbitrate_WithAvroAdminDisconnectEnvelopeAndArbitrateTrue_ReturnsTrue()
        {
            var jsonMessage = """
                            {
                                "EventTypes": ["Admin"],
                                "Admin": {
                                    "NoticeType": "Disconnect",
                                    "Arbitrate": true
                                }
                            }
                            """;

            StreamingApiClient.ShouldArbitrate(jsonMessage, out _).Should().BeTrue();
        }

        [Fact]
        public void ShouldArbitrate_WithAvroAdminDisconnectEnvelopeAndNoArbitrateField_ReturnsFalse()
        {
            var jsonMessage = """
                            {
                                "EventTypes": ["Admin"],
                                "Admin": {
                                    "NoticeType": "Disconnect"
                                }
                            }
                            """;

            StreamingApiClient.ShouldArbitrate(jsonMessage, out _).Should().BeFalse();
        }

        [Fact]
        public void ShouldArbitrate_WithCamelCaseAdminDisconnectEnvelopeAndArbitrateTrue_ReturnsTrue()
        {
            // Real server payloads are camelCase, unlike the PascalCase used in the other tests above -
            // this exercises the case-insensitive property lookups.
            var jsonMessage = """
                            {
                                "eventType": "Admin",
                                "message": {
                                    "noticeType": "Disconnect",
                                    "arbitrate": true
                                }
                            }
                            """;

            StreamingApiClient.ShouldArbitrate(jsonMessage, out _).Should().BeTrue();
        }

        [Fact]
        public void ShouldArbitrate_WithOrdinaryDataMessage_ReturnsFalse()
        {
            var jsonMessage = """
                            {
                                "EventType": "Trade",
                                "PerformanceId": "0P0000038R",
                                "SequenceNumber": 42
                            }
                            """;

            StreamingApiClient.ShouldArbitrate(jsonMessage, out _).Should().BeFalse();
        }

        [Fact]
        public void ShouldArbitrate_WithInvalidJson_ReturnsFalse()
        {
            StreamingApiClient.ShouldArbitrate("not valid json", out _).Should().BeFalse();
        }

        [Fact]
        public void ShouldArbitrate_WithNoticeMinutes_ExtractsValue()
        {
            var jsonMessage = """
                            {
                                "EventType": "Admin",
                                "Message": {
                                    "NoticeType": "Disconnect",
                                    "Arbitrate": true,
                                    "NoticeMinutes": 3
                                }
                            }
                            """;

            StreamingApiClient.ShouldArbitrate(jsonMessage, out var noticeMinutes).Should().BeTrue();
            noticeMinutes.Should().Be(3);
        }

        [Fact]
        public void ShouldArbitrate_WithoutNoticeMinutes_ReturnsNull()
        {
            var jsonMessage = """
                            {
                                "EventType": "Admin",
                                "Message": {
                                    "NoticeType": "Disconnect",
                                    "Arbitrate": true
                                }
                            }
                            """;

            StreamingApiClient.ShouldArbitrate(jsonMessage, out var noticeMinutes).Should().BeTrue();
            noticeMinutes.Should().BeNull();
        }

        [Fact]
        public void IsAdminMessage_WithAdminEnvelope_ReturnsTrue()
        {
            var jsonMessage = """
                            {
                                "EventType": "Admin",
                                "Message": {
                                    "NoticeType": "Disconnect"
                                }
                            }
                            """;

            var messagePacket = JsonConvert.DeserializeObject<MessagePacketEnvelope>(jsonMessage)!;

            StreamingApiClient.IsAdminMessage(messagePacket).Should().BeTrue();
        }

        [Fact]
        public void IsAdminMessage_WithAvroAdminEnvelope_ReturnsTrue()
        {
            var jsonMessage = """
                            {
                                "EventTypes": ["Admin"],
                                "Admin": {
                                    "NoticeType": "Disconnect"
                                }
                            }
                            """;

            var messagePacket = JsonConvert.DeserializeObject<MessagePacketEnvelope>(jsonMessage)!;

            StreamingApiClient.IsAdminMessage(messagePacket).Should().BeTrue();
        }

        [Fact]
        public void IsAdminMessage_WithOrdinaryDataEnvelope_ReturnsFalse()
        {
            var jsonMessage = """
                            {
                                "EventType": "Trade",
                                "PerformanceId": "0P0000038R",
                                "SequenceNumber": 42
                            }
                            """;

            var messagePacket = JsonConvert.DeserializeObject<MessagePacketEnvelope>(jsonMessage)!;

            StreamingApiClient.IsAdminMessage(messagePacket).Should().BeFalse();
        }
    }
}
