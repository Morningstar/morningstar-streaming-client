using Microsoft.Extensions.DependencyInjection;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services;
using Morningstar.Streaming.Client.Services.Counter;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Client.Services.WebSockets;

namespace Morningstar.Streaming.Client.Extensions;

/// <summary>
/// Extension methods for registering Morningstar Streaming Client services.
/// This allows the client library to be used in any .NET application (Console, WPF, Web, etc.)
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Morningstar Streaming Client services including:
    /// - Canary Service (main orchestration service)
    /// - Streaming API Client
    /// - Subscription management
    /// - WebSocket consumers
    /// - Counter logging
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddStreamingServices(this IServiceCollection services)
    {
        // Core application services
        services.AddSingleton<ICanaryService, CanaryService>();
        services.AddSingleton<ICounterLogger, CounterLogger>();
        services.AddSingleton<ITokenProvider, TokenProvider>();

        // HTTP client and API client
        services.AddHttpClient<IApiHelper, ApiHelper>();
        services.AddSingleton<IStreamingApiClient, StreamingApiClient>();

        // Subscription services
        services.AddSingleton<IStreamSubscriptionFactory, StreamSubscriptionFactory>();
        services.AddSingleton<ISubscriptionGroupManager, SubscriptionGroupManager>();

        // WebSocket services
        services.AddSingleton<IWebSocketConsumerFactory, WebSocketConsumerFactory>();
        services.AddSingleton<IWebSocketLoggerFactory, WebSocketLoggerFactory>();

        return services;
    }

    /// <summary>
    /// Registers the CounterLogger as a hosted service (for applications that support IHostedService).
    /// Optional - only needed if you want automatic counter logging in the background.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddStreamingHostedServices(this IServiceCollection services)
    {
        services.AddHostedService(sp => (CounterLogger)sp.GetRequiredService<ICounterLogger>());
        return services;
    }
}
