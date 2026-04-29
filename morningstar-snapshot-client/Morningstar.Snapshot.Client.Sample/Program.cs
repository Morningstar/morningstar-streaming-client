using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Morningstar.Snapshot.Client.Extensions;
using Morningstar.Snapshot.Client.Sample.Services.OAuthProvider;
using Morningstar.Snapshot.Client.Services;
using Morningstar.Snapshot.Client.Services.OAuthProvider;
using Morningstar.Snapshot.Domain.Config;
using Morningstar.Snapshot.Domain.Constants;
using Morningstar.Snapshot.Domain.Contracts;
using Serilog;

namespace Morningstar.Snapshot.Client.Sample;

/// <summary>
/// This is a sample console application demonstrating how to use the 
/// Morningstar.Snapshot.Client and Morningstar.Snapshot.Domain libraries
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
        Console.WriteLine("Morningstar Snapshot Client - Sample Application");
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


                // Register all Morningstar Snapshot Client services using the extension method
                services.AddSnapshotServices();
            });

    /// <summary>
    /// Demonstrates how to use the Canary service to request a snapshot. 
    /// </summary>
    static async Task RunExampleAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        var snapshotService = services.GetRequiredService<ISnapshotService>();
        var oAuthProvider = services.GetRequiredService<IOAuthProvider>();

        try
        {

            // Example: Request a snapshot
            // Note: You'll need to update the login credentials in Services\oAuthProvider\ExampleOAuthProvider.cs to get a valid access token for this to work
            // This is just a demonstration of the Snapshot API functionality            

            Console.WriteLine("--- Example: Requesting a Snapshot ---");
            Console.WriteLine("To request a snapshot, you need:");
            Console.WriteLine("1. A valid access token from your authentication provider");
            Console.WriteLine("2. A properly configured appsettings.json with API endpoints");
            Console.WriteLine("3. Investment identifiers to include in the snapshot request");

            // Run the following code to request a snapshot using the Snapshot API client. This will return a snapshot response with the requested data.:

            var secret = await oAuthProvider.GetOAuthSecretAsync(); // Ensure OAuth secret is set up
            if (secret.UserName == "{YOUR_USERNAME}" || secret.Password == "{YOUR_PASSWORD}")
            {
                Console.WriteLine("Invalid OAuth credentials. Please update the \\OAuthProvider\\ExampleOAuthProvider.cs file with valid credentials.");
                logger.LogWarning("Please update the \\OAuthProvider\\ExampleOAuthProvider.cs file with valid credentials before running the subscription example.");
                return;
            }

            var snapshotRequest = new SnapshotRequest
            {
                Investments = new InvestmentsRequest
                {
                    IdType = "PerformanceId",
                    Ids = new List<string> { "0P0000038R", "0P000003X1", "0P0001HD8R" }
                },
                EventTypes = new List<string>
                {
                    EventTypes.LastPrice,
                    EventTypes.TopOfBook,
                    EventTypes.Trade
                }
            };


            logger.LogInformation("Requesting snapshot with investments: {Investments} and event types: {EventTypes}",
                string.Join(", ", snapshotRequest.Investments.Ids),
                string.Join(", ", snapshotRequest.EventTypes));
            var response = await snapshotService.RequestSnapshotAsync(snapshotRequest);

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                // logger.LogError("Snapshot request failed with status code {StatusCode} and message: {Message}",
                //     response.StatusCode, response.Message);
            }

            // Print the snapshot response
            Console.WriteLine("\n=== Snapshot Response ===");
            var responseJson = Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine(responseJson);
            Console.WriteLine("=========================\n");

            logger.LogInformation("Example completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running the example");
        }
    }
}
