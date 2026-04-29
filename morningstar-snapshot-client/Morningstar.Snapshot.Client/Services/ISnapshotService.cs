using Morningstar.Snapshot.Domain;
using Morningstar.Snapshot.Domain.Contracts;

namespace Morningstar.Snapshot.Client.Services;

/// <summary>
/// Main service interface for managing Morningstar Snapshot API requests.
/// This is the primary interface that clients should use for requesting snapshots.
/// </summary>
public interface ISnapshotService
{
    /// <summary>
    /// Requests a snapshot of market data for the specified investments and event types.
    /// </summary>
    /// <param name="request">The snapshot request containing investment identifiers and event types</param>
    /// <param name="endpointUrl">The API endpoint URL for the snapshot request</param>
    /// <returns>Snapshot response with the requested data</returns>
    Task<SnapshotResponse> RequestSnapshotAsync(SnapshotRequest request);
}
