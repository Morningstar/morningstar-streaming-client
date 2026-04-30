using FluentAssertions;
using Morningstar.Snapshot.Client.Helpers;
using Morningstar.Snapshot.Domain;
using Morningstar.Snapshot.Domain.Constants;
using Morningstar.Snapshot.Domain.Models;
using System.Net;
using System.Text;

namespace Morningstar.Snapshot.Client.Tests.HelperTests;

public class ApiHelperTests
{
    private readonly MockHttpMessageHandler httpMessageHandler;
    private readonly HttpClient httpClient;
    private readonly ApiHelper sut;

    public ApiHelperTests()
    {
        httpMessageHandler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        httpClient = new HttpClient(httpMessageHandler);
        sut = new ApiHelper(httpClient);
    }

    [Fact]
    public void Constructor_SetsUserAgent_OnHttpClient()
    {
        // Assert
        var userAgent = string.Join(" ", httpClient.DefaultRequestHeaders.UserAgent.Select(u => u.Product!.Name));
        userAgent.Should().Be("Snapshot Client");
    }

    [Fact]
    public async Task ProcessRequestAsync_UsesCorrectHttpMethod()
    {
        // Act
        await sut.ProcessRequestAsync<OAuthToken>("https://api.example.com", HttpMethod.Post, null, null);

        // Assert
        httpMessageHandler.CapturedRequest!.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task ProcessRequestAsync_SendsRequestToCorrectUri()
    {
        // Arrange
        const string uri = "https://api.example.com/token";

        // Act
        await sut.ProcessRequestAsync<OAuthToken>(uri, HttpMethod.Post, null, null);

        // Assert
        httpMessageHandler.CapturedRequest!.RequestUri!.ToString().Should().Be(uri);
    }

    [Fact]
    public async Task ProcessRequestAsync_AddsAllHeaders_ToRequest()
    {
        // Arrange
        var headers = new List<KeyValuePair<string, string>>
        {
            new("Authorization", "Bearer test-token"),
            new("Accept", "application/json")
        };

        // Act
        await sut.ProcessRequestAsync<OAuthToken>("https://api.example.com", HttpMethod.Get, headers, null);

        // Assert
        var requestHeaders = httpMessageHandler.CapturedRequest!.Headers;
        requestHeaders.Should().Contain(h => h.Key == "Authorization" &&
            h.Value.Contains("Bearer test-token"));
        requestHeaders.Should().Contain(h => h.Key == "Accept" &&
            h.Value.Contains("application/json"));
    }

    [Fact]
    public async Task ProcessRequestAsync_DoesNotThrow_WhenHeadersIsNull()
    {
        // Act
        var act = () => sut.ProcessRequestAsync<OAuthToken>("https://api.example.com", HttpMethod.Post, null, null);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessRequestAsync_SetsNoContent_WhenModelIsNull()
    {
        // Act
        await sut.ProcessRequestAsync<OAuthToken>("https://api.example.com", HttpMethod.Post, null, null);

        // Assert
        httpMessageHandler.CapturedRequest!.Content.Should().BeNull();
    }

    [Fact]
    public async Task ProcessRequestAsync_SetsJsonContent_WhenModelIsProvided()
    {
        // Arrange
        var model = new { Name = "test", Value = 42 };

        // Act
        await sut.ProcessRequestAsync<OAuthToken>("https://api.example.com", HttpMethod.Post, null, model);

        // Assert
        var content = httpMessageHandler.CapturedRequest!.Content;
        content.Should().NotBeNull();
        content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        content.Headers.ContentType.CharSet.Should().Be("utf-8");
    }

    [Fact]
    public async Task ProcessRequestAsync_SerializesModelBody_AsJson()
    {
        // Arrange
        var model = new { Name = "test", Value = 42 };

        // Act
        await sut.ProcessRequestAsync<OAuthToken>("https://api.example.com", HttpMethod.Post, null, model);

        // Assert
        var body = await httpMessageHandler.CapturedRequest!.Content!.ReadAsStringAsync();
        body.Should().Contain("\"Name\"");
        body.Should().Contain("\"test\"");
        body.Should().Contain("\"Value\"");
        body.Should().Contain("42");
    }

    [Fact]
    public async Task ProcessRequestAsync_DeserializesResponse_IntoExpectedType()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(
                "{\"access_token\":\"abc123\",\"token_type\":\"Bearer\"}",
                Encoding.UTF8,
                "application/json")
        });
        var client = new ApiHelper(new HttpClient(handler));

        // Act
        var result = await client.ProcessRequestAsync<OAuthToken>(
            "https://auth.example.com/token", HttpMethod.Post, null, null);

        // Assert
        result.Should().NotBeNull();
        result.Access_Token.Should().Be("abc123");
        result.Token_Type.Should().Be("Bearer");
    }

    [Fact]
    public async Task ProcessRequestAsync_DeserializesSnapshotResponse_WithIMessageConverter()
    {
        // Arrange
        var json = $$"""
            {
              "statusCode": 200,
              "data": {
                "realtime": [
                  {
                    "performanceId": "0P0000038R",
                    "events": {
                      "{{EventTypes.LastPrice}}": {
                        "sequenceNumber": 1,
                        "message": { "price": 123.45, "size": 100, "pricePublishDateTime": 1000 }
                      }
                    }
                  }
                ],
                "delayed": []
              },
              "metaData": { "requestId": "req-1", "time": "2024-01-01T00:00:00Z", "messages": [] }
            }
            """;

        var handler = new MockHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = new ApiHelper(new HttpClient(handler));

        // Act
        var result = await client.ProcessRequestAsync<SnapshotResponse>(
            "https://api.example.com/snapshot", HttpMethod.Post, null, null);

        // Assert
        result.Should().NotBeNull();
        result.Data.Realtime.Should().ContainSingle()
            .Which.PerformanceId.Should().Be("0P0000038R");
        result.Data.Realtime[0].Events[EventTypes.LastPrice].Message
            .Should().BeOfType<LastPriceMessage>()
            .Which.Price.Should().Be(123.45);
    }

    [Fact]
    public async Task ProcessRequestAsync_ThrowsHttpRequestException_WhenRequestFails()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpRequestException("Network error"));
        var client = new ApiHelper(new HttpClient(handler));

        // Act
        var act = () => client.ProcessRequestAsync<OAuthToken>(
            "https://api.example.com", HttpMethod.Post, null, null);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("Network error");
    }
}

/// <summary>
/// Test double for HttpMessageHandler that captures the outbound request
/// and returns a preconfigured response.
/// </summary>
internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage? response;
    private readonly Exception? exception;

    public HttpRequestMessage? CapturedRequest { get; private set; }

    public MockHttpMessageHandler(HttpResponseMessage response)
        => this.response = response;

    public MockHttpMessageHandler(Exception exception)
        => this.exception = exception;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CapturedRequest = request;

        if (exception is not null)
            throw exception;

        return Task.FromResult(response!);
    }
}
