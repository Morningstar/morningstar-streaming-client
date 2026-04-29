using Microsoft.Extensions.Options;
using Morningstar.Snapshot.Client.Clients;
using Morningstar.Snapshot.Client.Services.Subscriptions;
using Morningstar.Snapshot.Domain;
using Morningstar.Snapshot.Domain.Config;
using Morningstar.Snapshot.Domain.Contracts;

namespace Morningstar.Snapshot.Client.Services.Snapshot;

/// <summary>
/// Default implementation of snapshot request factory.
/// Creates snapshot requests using the standard Level 1 streaming endpoint.
/// </summary>
public class SnapshotFactory : ISnapshotRequestFactory
{
    protected readonly ISnapshotApiClient snapshotApiClient;
    protected readonly AppConfig appConfig;
    protected readonly EndpointConfig endpointConfig;

    public SnapshotFactory(ISnapshotApiClient snapshotApiClient, IOptions<AppConfig> appConfig, IOptions<EndpointConfig> endpointConfig)
    {
        this.snapshotApiClient = snapshotApiClient;
        this.appConfig = appConfig.Value;
        this.endpointConfig = endpointConfig.Value;
    }

    public Task<SnapshotRequestResult> CreateRequestAsync(SnapshotRequest req)
    {
        var endpointUrl = $"{appConfig.SnapshotApiBaseAddress}/{endpointConfig.Level1UrlAddress}";
        return CreateInternalAsync(req, (request) =>
            snapshotApiClient.RequestSnapshotAsync(request, endpointUrl));
    }

    /// <summary>
    /// Protected method for requesting a snapshot subscription using the provided API call function. 
    /// Derived classes can use this to implement alternative subscription mechanisms.
    /// </summary>
    protected virtual async Task<SnapshotRequestResult> CreateInternalAsync<TReq>(
        TReq req,
        Func<TReq, Task<SnapshotResponse>> streamApiCallAsync)
        where TReq : class
    {
        var response = await streamApiCallAsync(req);

        // var cts = (req as dynamic).DurationSeconds != null
        //     ? new CancellationTokenSource(TimeSpan.FromSeconds((int)(req as dynamic).DurationSeconds))
        //     : new CancellationTokenSource();


        return new SnapshotRequestResult
        {
            ApiResponse = response!,
            CancellationTokenSource = null // Placeholder for potential future cancellation support,
        };
    }
}
