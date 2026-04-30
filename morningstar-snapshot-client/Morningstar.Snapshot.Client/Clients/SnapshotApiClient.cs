using Microsoft.Extensions.Logging;
using Morningstar.Snapshot.Client.Helpers;
using Morningstar.Snapshot.Client.Services.TokenProvider;
using Morningstar.Snapshot.Domain;

namespace Morningstar.Snapshot.Client.Clients;

public class SnapshotApiClient : ISnapshotApiClient
{
    private readonly IApiHelper apiHelper;
    private readonly ITokenProvider tokenProvider;
    private readonly ILogger<SnapshotApiClient> logger;

    public SnapshotApiClient(
        IApiHelper apiHelper,
        ILogger<SnapshotApiClient> logger,
        ITokenProvider tokenProvider)
    {
        this.apiHelper = apiHelper;
        this.tokenProvider = tokenProvider;
        this.logger = logger;
    }
    public async Task<SnapshotResponse> RequestSnapshotAsync<TRequest>(TRequest streamRequest, string endpointUrl) where TRequest : class
    {
        try
        {
            var headers = new List<KeyValuePair<string, string>>
            {
                new("Authorization", await tokenProvider.CreateBearerTokenAsync()),
                new("Accept", "application/json")
            };

            return await apiHelper.ProcessRequestAsync<SnapshotResponse>(
                endpointUrl,
                HttpMethod.Post,
                headers,
                streamRequest
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error when attempting to request snapshot.");
            throw;
        }
    }
}
