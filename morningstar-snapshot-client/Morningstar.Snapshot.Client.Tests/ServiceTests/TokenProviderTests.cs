using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Morningstar.Snapshot.Client.Helpers;
using Morningstar.Snapshot.Client.Services.OAuthProvider;
using Morningstar.Snapshot.Client.Services.TokenProvider;
using Morningstar.Snapshot.Domain.Config;
using Morningstar.Snapshot.Domain.Models;

namespace Morningstar.Snapshot.Client.Tests.ServiceTests;

public class TokenProviderTests
{
    private readonly Mock<IApiHelper> apiHelperMock;
    private readonly Mock<IOAuthProvider> oAuthProviderMock;
    private readonly Mock<ILogger<TokenProvider>> loggerMock;
    private readonly IOptions<AppConfig> appConfig;
    private readonly TokenProvider tokenProvider;

    public TokenProviderTests()
    {
        apiHelperMock = new Mock<IApiHelper>();
        oAuthProviderMock = new Mock<IOAuthProvider>();
        loggerMock = new Mock<ILogger<TokenProvider>>();
        appConfig = Options.Create(new AppConfig
        {
            OAuthAddress = "https://oauth.test.com/token",
        });

        tokenProvider = new TokenProvider(loggerMock.Object, appConfig, apiHelperMock.Object, oAuthProviderMock.Object);
    }

    [Fact]
    public async Task CreateBearerTokenAsync_ReturnsBearerToken_WhenOAuthSucceeds()
    {
        // Arrange
        oAuthProviderMock
            .Setup(x => x.GetOAuthSecretAsync())
            .ReturnsAsync(new OAuthSecret { UserName = "user", Password = "pass" });

        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<OAuthToken>(
                It.IsAny<string>(),
                HttpMethod.Post,
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object?>()))
            .ReturnsAsync(new OAuthToken { Token_Type = "Bearer", Access_Token = "abc123nhef456ghi789jkl012" });

        // Act
        var result = await tokenProvider.CreateBearerTokenAsync();

        // Assert
        result.Should().Be("Bearer abc123nhef456ghi789jkl012");
    }

    [Fact]
    public async Task CreateBearerTokenAsync_CallsOAuthEndpoint_WithBasicAuthHeader()
    {
        // Arrange
        oAuthProviderMock
            .Setup(x => x.GetOAuthSecretAsync())
            .ReturnsAsync(new OAuthSecret { UserName = "myuser", Password = "mypass" });

        List<KeyValuePair<string, string>>? capturedHeaders = null;
        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<OAuthToken>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object>()))
            .Callback<string, HttpMethod, List<KeyValuePair<string, string>>?, object?>(
                (_, _, headers, _) => capturedHeaders = headers)
            .ReturnsAsync(new OAuthToken { Token_Type = "Bearer", Access_Token = "token" });

        // Act
        await tokenProvider.CreateBearerTokenAsync();

        // Assert
        var expectedEncoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("myuser:mypass"));
        capturedHeaders.Should().Contain(h => h.Key == "Authorization" && h.Value == $"Basic {expectedEncoded}");
    }

    [Fact]
    public async Task CreateBearerTokenAsync_ThrowsAndLogsError_WhenApiHelperThrows()
    {
        // Arrange
        oAuthProviderMock
            .Setup(x => x.GetOAuthSecretAsync())
            .ReturnsAsync(new OAuthSecret { UserName = "user", Password = "pass" });

        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<OAuthToken>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object?>()))
            .ThrowsAsync(new HttpRequestException("Auth server unreachable"));

        // Act
        var act = () => tokenProvider.CreateBearerTokenAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unexpected error during calling OAuth.");
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateBearerTokenAsync_PropagatesException_WhenOAuthProviderThrows()
    {
        // Arrange
        oAuthProviderMock
            .Setup(x => x.GetOAuthSecretAsync())
            .ThrowsAsync(new InvalidOperationException("Secret not found"));

        // Act
        var act = () => tokenProvider.CreateBearerTokenAsync();

        // Assert — propagates directly, not wrapped by the inner catch block
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Secret not found");
    }

    [Fact]
    public async Task CreateBearerTokenAsync_UsesOAuthAddressFromConfig()
    {
        // Arrange
        oAuthProviderMock
            .Setup(x => x.GetOAuthSecretAsync())
            .ReturnsAsync(new OAuthSecret { UserName = "user", Password = "pass" });

        string? capturedUrl = null;
        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<OAuthToken>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object>()))
            .Callback<string, HttpMethod, List<KeyValuePair<string, string>>?, object?>(
                (url, _, _, _) => capturedUrl = url)
            .ReturnsAsync(new OAuthToken { Token_Type = "Bearer", Access_Token = "tok" });

        // Act
        await tokenProvider.CreateBearerTokenAsync();

        // Assert
        capturedUrl.Should().Be("https://oauth.test.com/token");
    }

    [Fact]
    public async Task CreateBearerTokenAsync_UsesHttpPostMethod()
    {
        // Arrange
        oAuthProviderMock
            .Setup(x => x.GetOAuthSecretAsync())
            .ReturnsAsync(new OAuthSecret { UserName = "user", Password = "pass" });

        HttpMethod? capturedMethod = null;
        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<OAuthToken>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object>()))
            .Callback<string, HttpMethod, List<KeyValuePair<string, string>>?, object?>(
                (_, method, _, _) => capturedMethod = method)
            .ReturnsAsync(new OAuthToken { Token_Type = "Bearer", Access_Token = "tok" });

        // Act
        await tokenProvider.CreateBearerTokenAsync();

        // Assert
        capturedMethod.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task CreateBearerTokenAsync_LogsError_WhenApiHelperReturnsNullToken()
    {
        // Arrange
        oAuthProviderMock
            .Setup(x => x.GetOAuthSecretAsync())
            .ReturnsAsync(new OAuthSecret { UserName = "user", Password = "pass" });

        apiHelperMock.Setup(x => x.ProcessRequestAsync<OAuthToken>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object?>()))
            .ReturnsAsync((OAuthToken?)null);

        // Act
        var act = () => tokenProvider.CreateBearerTokenAsync();

        // Assert — NullReferenceException is caught internally and wrapped as InvalidOperationException.
        // LogError fires once for the null token warning, then again inside the catch block.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unexpected error during calling OAuth.");
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }
}
