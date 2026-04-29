using Microsoft.Extensions.DependencyInjection;
using Morningstar.Snapshot.Client.Clients;
using Morningstar.Snapshot.Client.Helpers;
using Morningstar.Snapshot.Client.Services;
using Morningstar.Snapshot.Client.Services.Snapshot;
using Morningstar.Snapshot.Client.Services.TokenProvider;

namespace Morningstar.Snapshot.Client.Extensions;

/// <summary>
/// Extension methods for registering Morningstar Snapshot Client services.
/// This allows the client library to be used in any .NET application (Console, WPF, Web, etc.)
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Morningstar Snapshot Client services including:
    /// - Snapshot Service (main orchestration service)
    /// - Snapshot API Client
    /// - Subscription management
    /// - WebSocket consumers
    /// - Optional telemetry hooks (register your own ICounterLogger/ILatencyLogger implementations)
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSnapshotServices(this IServiceCollection services)
    {
        // Core application services
        services.AddSingleton<ISnapshotService, SnapshotService>();
        services.AddSingleton<ITokenProvider, TokenProvider>();

        // HTTP client and API client
        services.AddHttpClient<IApiHelper, ApiHelper>();
        services.AddSingleton<ISnapshotApiClient, SnapshotApiClient>();

        // Register snapshot request services
        services.AddSingleton<ISnapshotRequestFactory, SnapshotFactory>();

        return services;
    }
}
