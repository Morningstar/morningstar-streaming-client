# Morningstar Streaming Client - Sample Application

This is a complete working example demonstrating how to use the **Morningstar.Streaming.Client** and **Morningstar.Streaming.Domain** libraries in a standalone console application.

## What This Example Demonstrates

- ✅ Setting up dependency injection in a console app
- ✅ Configuring services using `appsettings.json`
- ✅ Registering Morningstar Streaming Client services
- ✅ Working with the `ICanaryService` interface
- ✅ Creating and managing Level 1 subscriptions
- ✅ Proper logging with Serilog
- ✅ Error handling best practices

## Prerequisites

- .NET 8.0 SDK or later
- Valid Morningstar Streaming API credentials and access token
- Configured `appsettings.json` with your API endpoints

## Getting Started

### 1. Configure appsettings.json

Update the `appsettings.json` file with your actual Morningstar Streaming API configuration:

```json
{
  "AppConfig": {
    "StreamingApiBaseAddress": "https://streaming.morningstar.com",
    "OAuthAddress": "https://www.us-api.morningstar.com/token/oauth/",
    "LogMessages": false,
    "LogMessagesPath": "logs"
  },
  "EndpointConfig": {
    "Level1UrlAddress": "direct-web-services/v2/streaming/level-1"
  }
}
```

### 2. Build the Project

```bash
dotnet build
```

### 3. Run the Application

```bash
dotnet run
```

## Understanding the Code

### Main Components

#### Program.cs

The entry point demonstrates:

1. **Host Builder Setup**
   ```csharp
   Host.CreateDefaultBuilder(args)
       .UseSerilog(...)
       .ConfigureServices((context, services) => {
          //Service Registration....
       })
   ```

2. **Service Registration**
   ```csharp
    // Register configuration sections
    services.Configure<AppConfig>(context.Configuration.GetSection("AppConfig"));
    services.Configure<EndpointConfig>(context.Configuration.GetSection("EndpointConfig"));

    // Register OAuth provider for your authentication
    services.AddSingleton<IOAuthProvider, ExampleOAuthProvider>();

    // Register all Morningstar Streaming Client services using the extension method
    services.AddStreamingServices();                
  
    // If you want background counter logging, uncomment the following line:
    //services.AddStreamingHostedServices();
   ```

3. **Using the Canary Service**
   ```csharp
   var canaryService = services.GetRequiredService<ICanaryService>();
   var response = await canaryService.StartLevel1SubscriptionAsync(request);
   ```

## Extending This Example

### Adding Your Own Logic

To create a subscription with real data, uncomment the subscription code in `Program.cs` and provide:

1. **Access Token**: extend or replace the ExampleOAuthProvider implementation
2. **Investment Identifiers**: Specify the securities you want to track
3. **Duration**: How long the subscription should run

Example:

```csharp
var subscriptionRequest = new StartSubscriptionRequest
{
    var subscriptionRequest = new StartSubscriptionRequest
    {
        Stream = new StreamRequest
        {
            Investments = new List<Investments>
            {
                new Investments
                { 
                    IdType = "PerformanceId",
                    Ids = new List<string> { "0P0000038R" }
                }
            },
            EventTypes = new []
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
};

var response = await canaryService.StartLevel1SubscriptionAsync(
    accessToken, 
    subscriptionRequest);
```

### Processing WebSocket Messages

The library automatically handles WebSocket connections. To process messages:

1. Subscribe to the appropriate events
2. Implement custom message handlers
3. Use the `IWebSocketConsumer` interface for advanced scenarios

### Adding Background Processing

To run subscriptions in the background:

```csharp
services.Configure<AppConfig>(context.Configuration.GetSection("AppConfig"));
services.Configure<EndpointConfig>(context.Configuration.GetSection("EndpointConfig"));

// Register example OAuth provider for your authentication
services.AddSingleton<IOAuthProvider, ExampleOAuthProvider>();

// Register all Morningstar Streaming Client services using the extension method
services.AddStreamingServices();                

// If you want background counter logging, uncomment the following line:
services.AddStreamingHostedServices();
```

This adds the `CounterLogger` as a background service that logs metrics.

## Common Use Cases

### 1. Request Multiple Performance Ids

```csharp
streamRequest = new StreamRequest
{
    Investments = new List<Investments>
    {
        new Investments
        { 
            IdType = "PerformanceId",
            Ids = new List<string> { "0P000003PE" }
        }
    },
},
```

### 2. Request Multiple Event Types
```csharp
EventTypes = new []
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
```

### 2. Long-Running Subscriptions

```csharp
var request = new StartSubscriptionRequest
{
    Stream = streamRequest,
    DurationSeconds = 3600 // 1 hour
};
```

### 3. Managing Multiple Subscriptions

```csharp
var sub1 = await canaryService.StartLevel1SubscriptionAsync(token, request1);
var sub2 = await canaryService.StartLevel1SubscriptionAsync(token, request2);

// Check all active subscriptions
var active = canaryService.GetActiveSubscriptions();
Console.WriteLine($"Active: {active.Count}");

// Stop specific subscription
await canaryService.StopSubscriptionAsync(sub1.SubscriptionGuid.Value);
```

## Logging

The example uses Serilog with:
- **Console output**: Real-time logging to the console
- **File output**: Logs saved to `{LogMessagesPath}/ws-subscription-{guid}.txt`

Configure logging in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    }
  }
}

"AppConfig": {
    "LogMessages": false,
    "LogMessagesPath": "logs"
  }
```

## Troubleshooting

### "Configuration section not found"
- Ensure `appsettings.json` is being copied to the output directory
- Check that section names match exactly (case-sensitive)

### "Unable to resolve service"
- Verify `services.AddSingleton<IOAuthProvider, {YourOAuthProviderImplementation}>();` is registered
- Verify `services.AddStreamingServices()` is called
- Check that all required configuration sections are registered

### "Authentication failed"
- Validate your access token is current and not expired
- Check that the token has the correct permissions/scopes

## Next Steps

After understanding this example, you can:

1. **Integrate into your own application** - Copy the service registration pattern
2. **Customize the data processing** - Add your own WebSocket message handlers
3. **Add business logic** - Build features on top of the subscription data
4. **Create different application types** - Use in WPF, Blazor, or other .NET apps

## Additional Resources

- See `Morningstar.Streaming.Client\README.md` for detailed API documentation
- Check the `Morningstar.Streaming.Domain` project for all available models and contracts

## Support

For questions about this example or the Morningstar Streaming Client libraries, please contact your Morningstar support team.
