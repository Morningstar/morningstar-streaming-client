using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services;
using Morningstar.Streaming.Client.Services.AvroBinaryDeserializer;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Client.Services.WebSockets;
using Morningstar.Streaming.Domain.Config;

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
    /// - Optional telemetry hooks (register your own ICounterLogger/ILatencyLogger implementations)
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddStreamingServices(this IServiceCollection services)
    {
        // Core application services
        services.AddSingleton<ICanaryService>(serviceProvider => new CanaryService(
            serviceProvider.GetRequiredService<ISubscriptionGroupManager>(),
            serviceProvider.GetRequiredService<IStreamSubscriptionFactory>(),
            serviceProvider.GetRequiredService<IWebSocketConsumerFactory>(),
            serviceProvider.GetRequiredService<ILogger<CanaryService>>(),
            serviceProvider.GetRequiredService<IOptions<AppConfig>>(),
            serviceProvider.GetService<IObservableMetric<IMetric>>()));
        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddSingleton<IAvroBinaryDeserializer, AvroBinaryDeserializer>();

        // HTTP client and API client
        services.AddHttpClient<IApiHelper, ApiHelper>();
        services.AddSingleton<IStreamingApiClient>(serviceProvider => new StreamingApiClient(
            serviceProvider.GetRequiredService<IApiHelper>(),
            serviceProvider.GetRequiredService<ILogger<StreamingApiClient>>(),
            serviceProvider.GetRequiredService<ITokenProvider>(),
            serviceProvider.GetRequiredService<IAvroBinaryDeserializer>(),
            serviceProvider.GetService<IObservableMetric<IMetric>>()));

        // Subscription services
        services.AddSingleton<IStreamSubscriptionFactory, StreamSubscriptionFactory>();
        services.AddSingleton<ISubscriptionGroupManager, SubscriptionGroupManager>();

        // WebSocket services
        services.AddSingleton<IWebSocketConsumerFactory, WebSocketConsumerFactory>();
        services.AddSingleton<IWebSocketLoggerFactory, WebSocketLoggerFactory>();

        return services;
    }
}
