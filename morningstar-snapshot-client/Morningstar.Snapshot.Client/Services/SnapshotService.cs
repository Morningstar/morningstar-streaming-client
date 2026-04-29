using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morningstar.Snapshot.Client.Services.Snapshot;
using Morningstar.Snapshot.Client.Services.Subscriptions;
using Morningstar.Snapshot.Domain;
using Morningstar.Snapshot.Domain.Config;
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
    protected readonly bool logMessages;

    public SnapshotService(
        ISnapshotRequestFactory snapshotRequestFactory,
        ILogger<SnapshotService> logger,
        IOptions<AppConfig> appConfig)
    {
        this.snapshotRequestFactory = snapshotRequestFactory;
        this.logger = logger;
        logMessages = appConfig.Value.LogMessages;
    }
    public Task<SnapshotResponse> RequestSnapshotAsync(SnapshotRequest request) =>
        StartRequestSnapshotAsync(request, snapshotRequestFactory.CreateRequestAsync);


    protected virtual async Task<SnapshotResponse> StartRequestSnapshotAsync<TRequest>(
                TRequest req,
                Func<TRequest, Task<SnapshotRequestResult>> createFunc)
                where TRequest : class
    {
        var snapshotResult = await createFunc(req);

        if (snapshotResult.ApiResponse.StatusCode != HttpStatusCode.OK)
        {
            // logger.LogError("Snapshot request failed with status code {StatusCode} and message: {Message}", snapshotResult.ApiResponse.StatusCode, snapshotResult.ApiResponse.Message);
        }
        else
        {
            logger.LogInformation("Snapshot request succeeded with status code {StatusCode}", snapshotResult.ApiResponse.StatusCode);
        }

        return snapshotResult.ApiResponse;
    }
}
