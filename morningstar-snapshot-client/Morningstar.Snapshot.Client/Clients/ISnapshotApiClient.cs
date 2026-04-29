using Morningstar.Snapshot.Domain;

namespace Morningstar.Snapshot.Client.Clients;

public interface ISnapshotApiClient
{
    /// <summary>
    /// Generic method to post a Level 1 snapshot request with any request type and endpoint.
    /// This allows for flexible endpoint configuration without exposing specific implementation details.
    /// </summary>
    Task<SnapshotResponse> RequestSnapshotAsync<TRequest>(TRequest streamRequest, string endpointUrl) where TRequest : class;

}
