using Microsoft.Extensions.Logging;
using Morningstar.Snapshot.Client.Helpers;
using Morningstar.Snapshot.Client.Services.TokenProvider;
using Morningstar.Snapshot.Domain;
using System.Net.WebSockets;

namespace Morningstar.Snapshot.Client.Clients;

public class SnapshotApiClient : ISnapshotApiClient
{
    private readonly IApiHelper apiHelper;
    private readonly ITokenProvider tokenProvider;
    private readonly ILogger<SnapshotApiClient> logger;

    private readonly record struct IncomingMessage(WebSocketMessageType MessageType, byte[] Payload, long ReceivedAtMillis);

    private readonly record struct TelemetryItem(WebSocketMessageType MessageType, string jsonMessage, long ReceivedAtMillis);

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
                new KeyValuePair<string, string>("Authorization", await tokenProvider.CreateBearerTokenAsync()),
                new KeyValuePair<string, string>("Accept", "application/json")
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
