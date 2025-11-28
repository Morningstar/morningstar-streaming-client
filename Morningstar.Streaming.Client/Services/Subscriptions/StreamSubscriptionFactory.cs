using Microsoft.Extensions.Options;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Contracts;

namespace Morningstar.Streaming.Client.Services.Subscriptions;

/// <summary>
/// Default implementation of stream subscription factory.
/// Creates subscriptions using the standard Level 1 streaming endpoint.
/// </summary>
public class StreamSubscriptionFactory : IStreamSubscriptionFactory
{
    protected readonly IStreamingApiClient streamingApiClient;
    protected readonly AppConfig appConfig;
    protected readonly EndpointConfig endpointConfig;

    public StreamSubscriptionFactory(IStreamingApiClient streamingApiClient, IOptions<AppConfig> appConfig, IOptions<EndpointConfig> endpointConfig)
    {
        this.streamingApiClient = streamingApiClient;
        this.appConfig = appConfig.Value;
        this.endpointConfig = endpointConfig.Value;
    }

    public Task<StreamSubscriptionResult> CreateAsync(StartSubscriptionRequest req)
    {
        var endpointUrl = $"{appConfig.StreamingApiBaseAddress}/{endpointConfig.Level1UrlAddress}";
        return CreateInternalAsync(req, (request) =>
            streamingApiClient.CreateL1StreamAsync(request.Stream, endpointUrl));
    }

    /// <summary>
    /// Protected method for creating subscriptions with a custom stream API call.
    /// Derived classes can use this to implement alternative subscription mechanisms.
    /// </summary>
    protected virtual async Task<StreamSubscriptionResult> CreateInternalAsync<TReq>(
        TReq req,
        Func<TReq, Task<StreamResponse>> streamApiCallAsync)
        where TReq : class
    {
        var response = await streamApiCallAsync(req);

        var cts = (req as dynamic).DurationSeconds != null
            ? new CancellationTokenSource(TimeSpan.FromSeconds((int)(req as dynamic).DurationSeconds))
            : new CancellationTokenSource();

        var wsUrls = new List<string>();
        wsUrls.AddRange(response?.Subscriptions?.Realtime ?? Enumerable.Empty<string>());
        wsUrls.AddRange(response?.Subscriptions?.Delayed ?? Enumerable.Empty<string>());

        return new StreamSubscriptionResult
        {
            ApiResponse = response!,
            CancellationTokenSource = cts,
            WebSocketUrls = wsUrls
        };
    }
}
