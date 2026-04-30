using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Morningstar.Snapshot.Client.Clients;
using Morningstar.Snapshot.Client.Services.Snapshot;
using Morningstar.Snapshot.Domain;
using Morningstar.Snapshot.Domain.Config;
using Morningstar.Snapshot.Domain.Constants;
using Morningstar.Snapshot.Domain.Contracts;
using System.Net;

namespace Morningstar.Snapshot.Client.Tests.ServiceTests;

public class SnapshotFactoryTests
{
    private readonly Mock<ISnapshotApiClient> snapshotApiClientMock;
    private readonly IOptions<AppConfig> appConfig;
    private readonly IOptions<EndpointConfig> endpointConfig;
    private readonly SnapshotFactory snapshotFactory;

    public SnapshotFactoryTests()
    {
        snapshotApiClientMock = new Mock<ISnapshotApiClient>();
        appConfig = Options.Create(new AppConfig
        {
            SnapshotApiBaseAddress = "https://api.test.com"
        });
        endpointConfig = Options.Create(new EndpointConfig
        {
            Level1UrlAddress = "snapshot/level1"
        });

        snapshotFactory = new SnapshotFactory(snapshotApiClientMock.Object, appConfig, endpointConfig);
    }

    [Fact]
    public async Task CreateRequestAsync_ReturnsSnapshotRequestResult_WhenApiClientSucceeds()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        var expectedResponse = new SnapshotResponse { StatusCode = HttpStatusCode.OK };

        snapshotApiClientMock
            .Setup(x => x.RequestSnapshotAsync(request, It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await snapshotFactory.CreateRequestAsync(request);

        // Assert
        result.ApiResponse.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CreateRequestAsync_CallsApiClient_WithCorrectEndpointUrl()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        const string expectedUrl = "https://api.test.com/snapshot/level1";

        snapshotApiClientMock
            .Setup(x => x.RequestSnapshotAsync(It.IsAny<SnapshotRequest>(), It.IsAny<string>()))
            .ReturnsAsync(new SnapshotResponse { StatusCode = HttpStatusCode.OK });

        // Act
        await snapshotFactory.CreateRequestAsync(request);

        // Assert
        snapshotApiClientMock.Verify(
            x => x.RequestSnapshotAsync(request, expectedUrl),
            Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_BuildsEndpointUrl_FromBaseAddressAndLevel1Address()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        string? capturedUrl = null;

        snapshotApiClientMock
            .Setup(x => x.RequestSnapshotAsync(It.IsAny<SnapshotRequest>(), It.IsAny<string>()))
            .Callback<SnapshotRequest, string>((_, url) => capturedUrl = url)
            .ReturnsAsync(new SnapshotResponse());

        // Act
        await snapshotFactory.CreateRequestAsync(request);

        // Assert
        capturedUrl.Should().Be("https://api.test.com/snapshot/level1");
    }

    [Fact]
    public async Task CreateRequestAsync_Throws_WhenApiClientThrows()
    {
        // Arrange
        var request = BuildSnapshotRequest();

        snapshotApiClientMock
            .Setup(x => x.RequestSnapshotAsync(It.IsAny<SnapshotRequest>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        // Act
        var act = () => snapshotFactory.CreateRequestAsync(request);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("API unavailable");
    }

    [Fact]
    public async Task CreateRequestAsync_CallsApiClient_ExactlyOnce()
    {
        // Arrange
        var request = BuildSnapshotRequest();

        snapshotApiClientMock
            .Setup(x => x.RequestSnapshotAsync(It.IsAny<SnapshotRequest>(), It.IsAny<string>()))
            .ReturnsAsync(new SnapshotResponse { StatusCode = HttpStatusCode.OK });

        // Act
        await snapshotFactory.CreateRequestAsync(request);

        // Assert
        snapshotApiClientMock.Verify(
            x => x.RequestSnapshotAsync(request, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRequestAsync_SetsApiResponse_OnResult()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        var expectedResponse = new SnapshotResponse { StatusCode = HttpStatusCode.OK };

        snapshotApiClientMock
            .Setup(x => x.RequestSnapshotAsync(It.IsAny<SnapshotRequest>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await snapshotFactory.CreateRequestAsync(request);

        // Assert
        result.ApiResponse.Should().BeSameAs(expectedResponse);
    }

    [Fact]
    public async Task CreateRequestAsync_BuildsEndpointUrl_UsingForwardSlashSeparator()
    {
        // Arrange
        var customAppConfig = Options.Create(new AppConfig
        {
            SnapshotApiBaseAddress = "https://api.custom.com"
        });
        var customEndpointConfig = Options.Create(new EndpointConfig
        {
            Level1UrlAddress = "v2/level1"
        });
        var customsnapshotFactory = new SnapshotFactory(snapshotApiClientMock.Object, customAppConfig, customEndpointConfig);

        string? capturedUrl = null;
        snapshotApiClientMock
            .Setup(x => x.RequestSnapshotAsync(It.IsAny<SnapshotRequest>(), It.IsAny<string>()))
            .Callback<SnapshotRequest, string>((_, url) => capturedUrl = url)
            .ReturnsAsync(new SnapshotResponse());

        // Act
        await customsnapshotFactory.CreateRequestAsync(BuildSnapshotRequest());

        // Assert
        capturedUrl.Should().Be("https://api.custom.com/v2/level1");
    }

    private static SnapshotRequest BuildSnapshotRequest() =>
        new()
        {
            Investments = new InvestmentsRequest
            {
                IdType = "PerformanceId",
                Ids = new List<string> { "0P0000038R" }
            },
            EventTypes = [EventTypes.LastPrice]
        };
}
