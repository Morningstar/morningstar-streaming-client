using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Morningstar.Snapshot.Client.Clients;
using Morningstar.Snapshot.Client.Helpers;
using Morningstar.Snapshot.Client.Services.TokenProvider;
using Morningstar.Snapshot.Domain;
using Morningstar.Snapshot.Domain.Contracts;
using System.Net;

namespace Morningstar.Snapshot.Client.Tests.ClientTests;

public class SnapshotApiClientTests
{
    private readonly Mock<IApiHelper> apiHelperMock;
    private readonly Mock<ITokenProvider> tokenProviderMock;
    private readonly Mock<ILogger<SnapshotApiClient>> loggerMock;
    private readonly SnapshotApiClient sut;

    public SnapshotApiClientTests()
    {
        apiHelperMock = new Mock<IApiHelper>();
        tokenProviderMock = new Mock<ITokenProvider>();
        loggerMock = new Mock<ILogger<SnapshotApiClient>>();

        sut = new SnapshotApiClient(apiHelperMock.Object, loggerMock.Object, tokenProviderMock.Object);
    }

    [Fact]
    public async Task RequestSnapshotAsync_ReturnsSnapshotResponse_WhenApiCallSucceeds()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        var expectedResponse = new SnapshotResponse { StatusCode = HttpStatusCode.OK };
        const string endpointUrl = "https://api.example.com/snapshot";

        tokenProviderMock
            .Setup(x => x.CreateBearerTokenAsync())
            .ReturnsAsync("Bearer test-token");

        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<SnapshotResponse>(
                endpointUrl,
                HttpMethod.Post,
                It.IsAny<List<KeyValuePair<string, string>>>(),
                request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await sut.RequestSnapshotAsync(request, endpointUrl);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RequestSnapshotAsync_SendsAuthorizationHeader_WithBearerToken()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        const string endpointUrl = "https://api.example.com/snapshot";
        const string bearerToken = "Bearer my-access-token";

        tokenProviderMock
            .Setup(x => x.CreateBearerTokenAsync())
            .ReturnsAsync(bearerToken);

        List<KeyValuePair<string, string>>? capturedHeaders = null;
        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<SnapshotResponse>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object>()))
            .Callback<string, HttpMethod, List<KeyValuePair<string, string>>?, object?>(
                (_, _, headers, _) => capturedHeaders = headers)
            .ReturnsAsync(new SnapshotResponse());

        // Act
        await sut.RequestSnapshotAsync(request, endpointUrl);

        // Assert
        capturedHeaders.Should().NotBeNull();
        capturedHeaders.Should().Contain(h => h.Key == "Authorization" && h.Value == bearerToken);
        capturedHeaders.Should().Contain(h => h.Key == "Accept" && h.Value == "application/json");
    }

    [Fact]
    public async Task RequestSnapshotAsync_ThrowsAndLogs_WhenApiHelperThrows()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        const string endpointUrl = "https://api.example.com/snapshot";

        tokenProviderMock
            .Setup(x => x.CreateBearerTokenAsync())
            .ReturnsAsync("Bearer test-token");

        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<SnapshotResponse>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act
        var act = () => sut.RequestSnapshotAsync(request, endpointUrl);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("Connection failed");
    }

    [Fact]
    public async Task RequestSnapshotAsync_ThrowsAndLogs_WhenTokenProviderThrows()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        const string endpointUrl = "https://api.example.com/snapshot";

        tokenProviderMock
            .Setup(x => x.CreateBearerTokenAsync())
            .ThrowsAsync(new InvalidOperationException("OAuth error"));

        // Act
        var act = () => sut.RequestSnapshotAsync(request, endpointUrl);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("OAuth error");
    }

    [Fact]
    public async Task RequestSnapshotAsync_LogsError_WhenApiHelperThrows()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        const string endpointUrl = "https://api.example.com/snapshot";

        tokenProviderMock
            .Setup(x => x.CreateBearerTokenAsync())
            .ReturnsAsync("Bearer test-token");

        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<SnapshotResponse>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act
        await Assert.ThrowsAsync<HttpRequestException>(() => sut.RequestSnapshotAsync(request, endpointUrl));

        // Assert
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
    public async Task RequestSnapshotAsync_LogsError_WhenTokenProviderThrows()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        const string endpointUrl = "https://api.example.com/snapshot";

        tokenProviderMock
            .Setup(x => x.CreateBearerTokenAsync())
            .ThrowsAsync(new InvalidOperationException("OAuth error"));

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RequestSnapshotAsync(request, endpointUrl));

        // Assert
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
    public async Task RequestSnapshotAsync_UsesHttpPostMethod()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        const string endpointUrl = "https://api.example.com/snapshot";
        HttpMethod? capturedMethod = null;

        tokenProviderMock
            .Setup(x => x.CreateBearerTokenAsync())
            .ReturnsAsync("Bearer test-token");

        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<SnapshotResponse>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object>()))
            .Callback<string, HttpMethod, List<KeyValuePair<string, string>>?, object?>(
                (_, method, _, _) => capturedMethod = method)
            .ReturnsAsync(new SnapshotResponse());

        // Act
        await sut.RequestSnapshotAsync(request, endpointUrl);

        // Assert
        capturedMethod.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task RequestSnapshotAsync_PassesRequestBody_ToApiHelper()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        const string endpointUrl = "https://api.example.com/snapshot";
        object? capturedBody = null;

        tokenProviderMock
            .Setup(x => x.CreateBearerTokenAsync())
            .ReturnsAsync("Bearer test-token");

        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<SnapshotResponse>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object>()))
            .Callback<string, HttpMethod, List<KeyValuePair<string, string>>?, object?>(
                (_, _, _, body) => capturedBody = body)
            .ReturnsAsync(new SnapshotResponse());

        // Act
        await sut.RequestSnapshotAsync(request, endpointUrl);

        // Assert
        capturedBody.Should().BeSameAs(request);
    }

    [Fact]
    public async Task RequestSnapshotAsync_PassesEndpointUrl_ToApiHelper()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        const string endpointUrl = "https://api.example.com/snapshot";
        string? capturedUrl = null;

        tokenProviderMock
            .Setup(x => x.CreateBearerTokenAsync())
            .ReturnsAsync("Bearer test-token");

        apiHelperMock
            .Setup(x => x.ProcessRequestAsync<SnapshotResponse>(
                It.IsAny<string>(),
                It.IsAny<HttpMethod>(),
                It.IsAny<List<KeyValuePair<string, string>>>(),
                It.IsAny<object>()))
            .Callback<string, HttpMethod, List<KeyValuePair<string, string>>?, object?>(
                (url, _, _, _) => capturedUrl = url)
            .ReturnsAsync(new SnapshotResponse());

        // Act
        await sut.RequestSnapshotAsync(request, endpointUrl);

        // Assert
        capturedUrl.Should().Be(endpointUrl);
    }

    private static SnapshotRequest BuildSnapshotRequest() =>
        new()
        {
            Investments = new InvestmentsRequest
            {
                IdType = "PerformanceId",
                Ids = new List<string> { "0P0000038R" }
            },
            EventTypes = new List<string> { "LastPrice" }
        };
}
