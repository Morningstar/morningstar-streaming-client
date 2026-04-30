using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Morningstar.Snapshot.Client.Services;
using Morningstar.Snapshot.Client.Services.Snapshot;
using Morningstar.Snapshot.Client.Services.Subscriptions;
using Morningstar.Snapshot.Domain;
using Morningstar.Snapshot.Domain.Constants;
using Morningstar.Snapshot.Domain.Contracts;
using System.Net;

namespace Morningstar.Snapshot.Client.Tests.ServiceTests;

public class SnapshotServiceTests
{
    private readonly Mock<ISnapshotRequestFactory> snapshotRequestFactoryMock;
    private readonly Mock<ILogger<SnapshotService>> loggerMock;
    private readonly SnapshotService snapshotService;

    public SnapshotServiceTests()
    {
        snapshotRequestFactoryMock = new Mock<ISnapshotRequestFactory>();
        loggerMock = new Mock<ILogger<SnapshotService>>();

        snapshotService = new SnapshotService(snapshotRequestFactoryMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task RequestSnapshotAsync_ReturnsApiResponse_WhenFactorySucceeds()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        var expectedResponse = new SnapshotResponse { StatusCode = HttpStatusCode.OK };

        snapshotRequestFactoryMock
            .Setup(x => x.CreateRequestAsync(request))
            .ReturnsAsync(new SnapshotRequestResult { ApiResponse = expectedResponse });

        // Act
        var result = await snapshotService.RequestSnapshotAsync(request);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RequestSnapshotAsync_CallsFactory_WithCorrectRequest()
    {
        // Arrange
        var request = BuildSnapshotRequest();

        snapshotRequestFactoryMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<SnapshotRequest>()))
            .ReturnsAsync(new SnapshotRequestResult { ApiResponse = new SnapshotResponse { StatusCode = HttpStatusCode.OK } });

        // Act
        await snapshotService.RequestSnapshotAsync(request);

        // Assert
        snapshotRequestFactoryMock.Verify(x => x.CreateRequestAsync(request), Times.Once);
    }

    [Fact]
    public async Task RequestSnapshotAsync_ReturnsNonOkResponse_WhenFactoryReturnsError()
    {
        // Arrange
        var request = BuildSnapshotRequest();
        var errorResponse = new SnapshotResponse { StatusCode = HttpStatusCode.Unauthorized };

        snapshotRequestFactoryMock
            .Setup(x => x.CreateRequestAsync(request))
            .ReturnsAsync(new SnapshotRequestResult { ApiResponse = errorResponse });

        // Act
        var result = await snapshotService.RequestSnapshotAsync(request);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestSnapshotAsync_Throws_WhenFactoryThrows()
    {
        // Arrange
        var request = BuildSnapshotRequest();

        snapshotRequestFactoryMock
            .Setup(x => x.CreateRequestAsync(request))
            .ThrowsAsync(new InvalidOperationException("Factory error"));

        // Act
        var act = () => snapshotService.RequestSnapshotAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Factory error");
    }

    [Fact]
    public async Task RequestSnapshotAsync_LogsInformation_WhenStatusCodeIsOk()
    {
        // Arrange
        var request = BuildSnapshotRequest();

        snapshotRequestFactoryMock
            .Setup(x => x.CreateRequestAsync(request))
            .ReturnsAsync(new SnapshotRequestResult { ApiResponse = new SnapshotResponse { StatusCode = HttpStatusCode.OK } });

        // Act
        await snapshotService.RequestSnapshotAsync(request);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestSnapshotAsync_DoesNotLogInformation_WhenStatusCodeIsNonOk()
    {
        // Arrange
        var request = BuildSnapshotRequest();

        snapshotRequestFactoryMock
            .Setup(x => x.CreateRequestAsync(request))
            .ReturnsAsync(new SnapshotRequestResult { ApiResponse = new SnapshotResponse { StatusCode = HttpStatusCode.BadRequest } });

        // Act
        await snapshotService.RequestSnapshotAsync(request);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task RequestSnapshotAsync_ReturnsResponse_ForVariousNonOkStatusCodes(HttpStatusCode statusCode)
    {
        // Arrange
        var request = BuildSnapshotRequest();

        snapshotRequestFactoryMock
            .Setup(x => x.CreateRequestAsync(request))
            .ReturnsAsync(new SnapshotRequestResult { ApiResponse = new SnapshotResponse { StatusCode = statusCode } });

        // Act
        var result = await snapshotService.RequestSnapshotAsync(request);

        // Assert
        result.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task RequestSnapshotAsync_CallsFactory_ExactlyOnce()
    {
        // Arrange
        var request = BuildSnapshotRequest();

        snapshotRequestFactoryMock
            .Setup(x => x.CreateRequestAsync(It.IsAny<SnapshotRequest>()))
            .ReturnsAsync(new SnapshotRequestResult { ApiResponse = new SnapshotResponse { StatusCode = HttpStatusCode.OK } });

        // Act
        await snapshotService.RequestSnapshotAsync(request);

        // Assert
        snapshotRequestFactoryMock.Verify(x => x.CreateRequestAsync(request), Times.Once);
    }

    private static SnapshotRequest BuildSnapshotRequest() =>
        new()
        {
            Investments = new InvestmentsRequest
            {
                IdType = "PerformanceId",
                Ids = ["0P0000038R", "0P000003X1"]
            },
            EventTypes = [EventTypes.LastPrice, EventTypes.Trade]
        };
}
