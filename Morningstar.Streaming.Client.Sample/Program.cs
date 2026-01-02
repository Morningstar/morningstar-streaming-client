using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Extensions;
using Morningstar.Streaming.Client.Sample.Services.OAuthProvider;
using Morningstar.Streaming.Client.Services;
using Morningstar.Streaming.Client.Services.OAuthProvider;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Constants;
using Morningstar.Streaming.Domain.Contracts;
using Serilog;

namespace Morningstar.Streaming.Client.Sample;

/// <summary>
/// This is a sample console application demonstrating how to use the 
/// Morningstar.Streaming.Client and Morningstar.Streaming.Domain libraries
/// in a standalone .NET application.
/// 
/// This example shows:
/// 1. How to configure the application using appsettings.json
/// 2. How to register all required services using dependency injection
/// 3. How to create and manage Level 1 subscriptions
/// 4. How to monitor active subscriptions
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("Morningstar Streaming Client - Sample Application");
        Console.WriteLine("=================================================");

        // Build the host with dependency injection and configuration
        var host = CreateHostBuilder(args).Build();
        await host.StartAsync();
        
        // Run the example
        await RunExampleAsync(host.Services);

        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();
    }

    /// <summary>
    /// Creates and configures the host builder with all necessary services
    /// </summary>
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/canary-console-.txt", rollingInterval: RollingInterval.Day))
            .ConfigureServices((context, services) =>
            {
                // Register configuration sections
                services.Configure<AppConfig>(context.Configuration.GetSection("AppConfig"));
                services.Configure<EndpointConfig>(context.Configuration.GetSection("EndpointConfig"));

                // Register example OAuth provider for your authentication
                services.AddSingleton<IOAuthProvider, ExampleOAuthProvider>();

                // Register all Morningstar Streaming Client services using the extension method
                services.AddStreamingServices();

                // If you want background counter logging, uncomment the following line:
                services.AddStreamingHostedServices();
            });

    /// <summary>
    /// Demonstrates how to use the Canary service to create and manage subscriptions
    /// </summary>
    static async Task RunExampleAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        var canaryService = services.GetRequiredService<ICanaryService>();
        var oAuthProvider = services.GetRequiredService<IOAuthProvider>();

        try
        {
            
            // Example: Start a new Level 1 subscription
            // Note: You'll need to update the login credentials in Services\oAuthProvider\ExampleOAuthProvider.cs to get a valid access token for this to work
            // This is just a demonstration of the Streaming API Subscription functionality            

            Console.WriteLine("--- Example: Starting a Level 1 Subscription ---");
            Console.WriteLine("To start a subscription, you need:");
            Console.WriteLine("1. A valid access token from your authentication provider");
            Console.WriteLine("2. A properly configured appsettings.json with API endpoints");
            Console.WriteLine("3. Investment identifiers to subscribe to");

            // Run the following code to start a subscription:

            var secret = await oAuthProvider.GetOAuthSecretAsync(); // Ensure OAuth secret is set up
            if (secret.UserName == "{YOUR_USERNAME}" || secret.Password == "{YOUR_PASSWORD}")
            {
                Console.WriteLine("Invalid OAuth credentials. Please update the \\OAuthProvider\\ExampleOAuthProvider.cs file with valid credentials.");
                logger.LogWarning("Please update the \\OAuthProvider\\ExampleOAuthProvider.cs file with valid credentials before running the subscription example.");
                return;
            }

            var subscriptionRequest = new StartSubscriptionRequest
            {
                Stream = new StreamRequest
                {
                    Investments = new List<Investments>
                    {
                        new Investments
                        {
                            IdType = "PerformanceId",
                            Ids = new List<string> { "0P0000038R", "0P000003X1", "0P0001HD8R" }
                        }
                    },
                    EventTypes = new[]
                    {
                        EventTypes.AggregateSummary,
                        EventTypes.Auction,
                        EventTypes.Close,
                        EventTypes.IndexTick,
                        EventTypes.InstrumentPerformanceStatistics,
                        EventTypes.LastPrice,
                        EventTypes.MidPrice,
                        EventTypes.NAVPrice,
                        EventTypes.OHLPrice,
                        EventTypes.SettlementPrice,
                        EventTypes.SpreadStatistics,
                        EventTypes.Status,
                        EventTypes.TopOfBook,
                        EventTypes.Trade,
                        EventTypes.TradeCancellation,
                        EventTypes.TradeCorrection
                    }
                },
                DurationSeconds = 300 // Run for 5 minutes
            };

            logger.LogInformation("Starting Level 1 subscription...");
            var response = await canaryService.StartLevel1SubscriptionAsync(subscriptionRequest);

            if (response.ApiResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                logger.LogInformation(
                    "Subscription started successfully! GUID: {Guid}, Started: {StartedAt}, Expires: {ExpiresAt}",
                    response.SubscriptionGuid,
                    response.StartedAt,
                    response.ExpiresAt);                

                var activeSubscriptions = canaryService.GetActiveSubscriptions();
                logger.LogInformation("Active subscriptions: {Count}", activeSubscriptions.Count);

                foreach (var sub in activeSubscriptions)
                {
                    logger.LogInformation(
                        "Subscription {Guid}: Started at {StartedAt}, WebSocket URLs: {Count}",
                        sub.Guid,
                        sub.StartedAt,
                        sub.WebSocketUrls.Count);
                }

                //Monitor the subscription
                await Task.Delay(TimeSpan.FromSeconds(120));

                //Stop the subscription
                if (response.SubscriptionGuid.HasValue)
                {
                    logger.LogInformation("Stopping subscription...");
                    var stopResult = await canaryService.StopSubscriptionAsync(response.SubscriptionGuid.Value);
                    
                    if (stopResult.Success)
                    {
                        logger.LogInformation("Subscription stopped successfully: {Message}", stopResult.Message);
                    }
                    else
                    {
                        logger.LogError("Failed to stop subscription. Error: {ErrorCode}, Message: {Message}", 
                            stopResult.ErrorCode, stopResult.Message);
                    }
                }
            }
            else
            {
                logger.LogError(
                    "Failed to start subscription. Status: {StatusCode}, ErrorCode: {ErrorCode}, Message: {Message}",
                    response.ApiResponse.StatusCode,
                    response.ApiResponse.ErrorCode,
                    response.ApiResponse.Message);
            }

            logger.LogInformation("Example completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running the example");
        }
    }
}
