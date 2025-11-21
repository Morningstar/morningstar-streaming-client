using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services.OAuthProvider;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Models;

namespace Morningstar.Streaming.Client.Tests.ServiceTests
{
    public class TokenProviderTests
    {
        private readonly Mock<ILogger<TokenProvider>> mockLogger;
        private readonly Mock<IOptions<AppConfig>> mockAppConfig;
        private readonly Mock<IApiHelper> mockApiHelper;
        private readonly Mock<IOAuthProvider> mockOAuthProvider;
        private readonly TokenProvider tokenProvider;

        public TokenProviderTests()
        {
            // Arrange - Initialize mocks
            mockLogger = new Mock<ILogger<TokenProvider>>();
            mockAppConfig = new Mock<IOptions<AppConfig>>();
            mockApiHelper = new Mock<IApiHelper>();
            mockOAuthProvider = new Mock<IOAuthProvider>();

            // Setup AppConfig with default values
            mockAppConfig.Setup(x => x.Value).Returns(new AppConfig
            {
                OAuthAddress = "https://oauth.test.com/token",
                StreamingApiBaseAddress = "https://api.test.com",
                ConnectionStringTtl = 300,
                LogMessages = false,
                LogMessagesPath = "logs"
            });

            // System Under Test
            tokenProvider = new TokenProvider(
                mockLogger.Object,
                mockAppConfig.Object,
                mockApiHelper.Object,
                mockOAuthProvider.Object
            );
        }

        [Fact]
        public async Task CreateBearerTokenAsync_WithValidCredentials_ReturnsFormattedBearerToken()
        {
            // Arrange
            var oAuthSecret = new OAuthSecret
            {
                UserName = "test-client-id",
                Password = "test-client-secret"
            };

            var oAuthToken = new OAuthToken
            {
                Access_Token = "abc123def456ghi789",
                Token_Type = "Bearer",
                Expires_In = 3600
            };

            mockOAuthProvider
                .Setup(x => x.GetOAuthSecretAsync())
                .ReturnsAsync(oAuthSecret);

            mockApiHelper
                .Setup(x => x.ProcessRequestAsync<OAuthToken>(
                    "https://oauth.test.com/token",
                    HttpMethod.Post,
                    It.IsAny<List<KeyValuePair<string, string>>>(),
                    null))
                .ReturnsAsync(oAuthToken);

            // Act
            var result = await tokenProvider.CreateBearerTokenAsync();

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Be("Bearer abc123def456ghi789");

            mockOAuthProvider.Verify(x => x.GetOAuthSecretAsync(), Times.Once);
            mockApiHelper.Verify(x => x.ProcessRequestAsync<OAuthToken>(
                "https://oauth.test.com/token",
                HttpMethod.Post,
                It.IsAny<List<KeyValuePair<string, string>>>(),
                null), Times.Once);
        }

        [Fact]
        public async Task CreateBearerTokenAsync_EncodesCredentialsInBase64()
        {
            // Arrange
            var oAuthSecret = new OAuthSecret
            {
                UserName = "my-client-id",
                Password = "my-client-secret"
            };

            var oAuthToken = new OAuthToken
            {
                Access_Token = "test-token",
                Token_Type = "Bearer",
                Expires_In = 3600
            };

            mockOAuthProvider
                .Setup(x => x.GetOAuthSecretAsync())
                .ReturnsAsync(oAuthSecret);

            List<KeyValuePair<string, string>>? capturedHeaders = null;

            mockApiHelper
                .Setup(x => x.ProcessRequestAsync<OAuthToken>(
                    It.IsAny<string>(),
                    It.IsAny<HttpMethod>(),
                    It.IsAny<List<KeyValuePair<string, string>>>(),
                    It.IsAny<object?>()))
                .Callback<string, HttpMethod, List<KeyValuePair<string, string>>, object?>(
                    (url, method, headers, body) => capturedHeaders = headers)
                .ReturnsAsync(oAuthToken);

            // Act
            await tokenProvider.CreateBearerTokenAsync();

            // Assert
            capturedHeaders.Should().NotBeNull();
            capturedHeaders.Should().ContainSingle(h => h.Key == "Authorization");

            var authHeader = capturedHeaders!.First(h => h.Key == "Authorization");
            authHeader.Value.Should().StartWith("Basic ");

            // Verify Base64 encoding
            var base64Part = authHeader.Value.Replace("Basic ", "");
            var decodedBytes = Convert.FromBase64String(base64Part);
            var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
            decodedString.Should().Be("my-client-id:my-client-secret");
        }
        

        [Fact]
        public async Task CreateBearerTokenAsync_WhenApiHelperThrowsException_LogsErrorAndThrows()
        {
            // Arrange
            var oAuthSecret = new OAuthSecret
            {
                UserName = "test-client",
                Password = "test-secret"
            };

            mockOAuthProvider
                .Setup(x => x.GetOAuthSecretAsync())
                .ReturnsAsync(oAuthSecret);

            var expectedException = new HttpRequestException("OAuth service unavailable");

            mockApiHelper
                .Setup(x => x.ProcessRequestAsync<OAuthToken>(
                    It.IsAny<string>(),
                    It.IsAny<HttpMethod>(),
                    It.IsAny<List<KeyValuePair<string, string>>>(),
                    It.IsAny<object?>()))
                .ThrowsAsync(expectedException);

            // Act
            Func<Task> act = async () => await tokenProvider.CreateBearerTokenAsync();

            // Assert
            var exception = await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Unexpected error during calling OAuth.");
            
            exception.And.InnerException.Should().BeOfType<HttpRequestException>()
                .Which.Message.Should().Be("OAuth service unavailable");

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error during calling OAuth")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateBearerTokenAsync_WhenOAuthProviderThrowsException_PropagatesException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Failed to retrieve OAuth secret");

            mockOAuthProvider
                .Setup(x => x.GetOAuthSecretAsync())
                .ThrowsAsync(expectedException);

            // Act
            Func<Task> act = async () => await tokenProvider.CreateBearerTokenAsync();

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Failed to retrieve OAuth secret");
        }

        [Fact]
        public async Task CreateBearerTokenAsync_CallsOAuthProviderBeforeApiHelper()
        {
            // Arrange
            var callOrder = new List<string>();

            var oAuthSecret = new OAuthSecret
            {
                UserName = "client",
                Password = "secret"
            };

            var oAuthToken = new OAuthToken
            {
                Access_Token = "token",
                Token_Type = "Bearer",
                Expires_In = 3600
            };

            mockOAuthProvider
                .Setup(x => x.GetOAuthSecretAsync())
                .Callback(() => callOrder.Add("OAuthProvider"))
                .ReturnsAsync(oAuthSecret);

            mockApiHelper
                .Setup(x => x.ProcessRequestAsync<OAuthToken>(
                    It.IsAny<string>(),
                    It.IsAny<HttpMethod>(),
                    It.IsAny<List<KeyValuePair<string, string>>>(),
                    It.IsAny<object?>()))
                .Callback(() => callOrder.Add("ApiHelper"))
                .ReturnsAsync(oAuthToken);

            // Act
            await tokenProvider.CreateBearerTokenAsync();

            // Assert
            callOrder.Should().HaveCount(2);
            callOrder[0].Should().Be("OAuthProvider");
            callOrder[1].Should().Be("ApiHelper");
        }
        
        [Fact]
        public async Task CreateBearerTokenAsync_WithSpecialCharactersInCredentials_EncodesCorrectly()
        {
            // Arrange
            var oAuthSecret = new OAuthSecret
            {
                UserName = "client@example.com",
                Password = "p@ssw0rd!#$%"
            };

            var oAuthToken = new OAuthToken
            {
                Access_Token = "token",
                Token_Type = "Bearer",
                Expires_In = 3600
            };

            mockOAuthProvider
                .Setup(x => x.GetOAuthSecretAsync())
                .ReturnsAsync(oAuthSecret);

            List<KeyValuePair<string, string>>? capturedHeaders = null;

            mockApiHelper
                .Setup(x => x.ProcessRequestAsync<OAuthToken>(
                    It.IsAny<string>(),
                    It.IsAny<HttpMethod>(),
                    It.IsAny<List<KeyValuePair<string, string>>>(),
                    It.IsAny<object?>()))
                .Callback<string, HttpMethod, List<KeyValuePair<string, string>>, object?>(
                    (url, method, headers, body) => capturedHeaders = headers)
                .ReturnsAsync(oAuthToken);

            // Act
            await tokenProvider.CreateBearerTokenAsync();

            // Assert
            capturedHeaders.Should().NotBeNull();
            var authHeader = capturedHeaders!.First(h => h.Key == "Authorization");
            var base64Part = authHeader.Value.Replace("Basic ", "");
            var decodedBytes = Convert.FromBase64String(base64Part);
            var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
            decodedString.Should().Be("client@example.com:p@ssw0rd!#$%");
        }
    }
}
