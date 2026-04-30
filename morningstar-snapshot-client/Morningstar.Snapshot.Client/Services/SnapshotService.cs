using Microsoft.Extensions.Logging;
using Morningstar.Snapshot.Client.Services.Snapshot;
using Morningstar.Snapshot.Client.Services.Subscriptions;
using Morningstar.Snapshot.Domain;
using Morningstar.Snapshot.Domain.Contracts;
using System.Net;

namespace Morningstar.Snapshot.Client.Services;

/// <summary>
/// Base implementation of the Snapshot service for managing Morningstar Streaming API subscriptions.
/// </summary>
public class SnapshotService : ISnapshotService
{
    protected readonly ISnapshotRequestFactory snapshotRequestFactory;
    protected readonly ILogger logger;

    public SnapshotService(
        ISnapshotRequestFactory snapshotRequestFactory,
        ILogger<SnapshotService> logger)
    {
        this.snapshotRequestFactory = snapshotRequestFactory;
        this.logger = logger;
    }
    public Task<SnapshotResponse> RequestSnapshotAsync(SnapshotRequest request) =>
        StartRequestSnapshotAsync(request, snapshotRequestFactory.CreateRequestAsync);


    protected virtual async Task<SnapshotResponse> StartRequestSnapshotAsync<TRequest>(
                TRequest req,
                Func<TRequest, Task<SnapshotRequestResult>> createFunc)
                where TRequest : class
    {
        var snapshotResult = await createFunc(req);

        if (snapshotResult.ApiResponse.StatusCode == HttpStatusCode.OK)
        {
            logger.LogInformation("Snapshot request succeeded with status code {StatusCode}", snapshotResult.ApiResponse.StatusCode);
        }

        return snapshotResult.ApiResponse;
    }
}
