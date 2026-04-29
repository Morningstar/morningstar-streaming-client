using Morningstar.Snapshot.Client.Services.Subscriptions;
using Morningstar.Snapshot.Domain.Contracts;

namespace Morningstar.Snapshot.Client.Services.Snapshot;

/// <summary>
/// Factory interface for creating snapshot requests.
/// </summary>
public interface ISnapshotRequestFactory
{
    /// <summary>
    /// Creates a snapshot request using the standard endpoint.
    /// </summary>
    Task<SnapshotRequestResult> CreateRequestAsync(SnapshotRequest req);
}
