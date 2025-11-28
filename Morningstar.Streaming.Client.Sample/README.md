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

### 4. How these services interact with our Streaming Api

The Morningstar Streaming Client follows a clear workflow to establish and maintain real-time data streams:

#### Overview

```
┌─────────────┐      ┌──────────────────┐      ┌─────────────────┐      ┌──────────────┐
│   Your App  │ ───> │  CanaryService   │ ───> │ StreamingApi    │ ───> │  WebSocket   │
│             │      │                  │      │     Client      │      │  Connection  │
└─────────────┘      └──────────────────┘      └─────────────────┘      └──────────────┘
```

#### Step-by-Step Process

**1. Create Subscription Request**
```csharp
var subscriptionRequest = new StartSubscriptionRequest
{
    Stream = new StreamRequest
    {
        Investments = [...],  // Securities to track
        EventTypes = [...]    // Event types to receive
    },
    DurationSeconds = 300
};
```

**2. CanaryService Creates the Stream**
- `ICanaryService.StartLevel1SubscriptionAsync()` is called
- Service validates the request and authenticates using your OAuth token
- Makes HTTP POST to the Streaming API endpoint to register the subscription
- Receives a `StreamResponse` containing the WebSocket URL and subscription ID

**3. StreamingApiClient Establishes WebSocket Connection**
- `IStreamingApiClient` automatically connects to the provided WebSocket URL
- Adds authorization headers using the `ITokenProvider`
- Implements automatic retry logic (up to 3 attempts) with exponential backoff
- Resets retry counter after each successful connection for long-running streams

**4. Message Processing Loop**
- Receives real-time market data events via WebSocket
- Handles server heartbeat messages automatically (sends acknowledgments)
- Monitors heartbeat timeout (15 seconds) - closes connection if server stops responding
- Routes data messages to your custom message handler
- Messages are logged to file if `LogMessages` is enabled in configuration

**5. Subscription Management**
- `CanaryService` tracks all active subscriptions
- Provides methods to stop individual subscriptions or all at once
- Gracefully closes WebSocket connections when stopped
- Cleans up resources and cancels background tasks

#### Key Services Explained

| Service | Purpose |
|---------|---------|
| **ICanaryService** | High-level orchestration - creates subscriptions, manages lifecycle |
| **IStreamingApiClient** | HTTP API communication - makes REST calls to create streams |
| **WebSocket Client** | Real-time data connection - receives live market data |
| **ITokenProvider** | Authentication - provides OAuth bearer tokens |
| **Counter Service** | (Optional) Tracks and logs message statistics |

#### Configuration Points

- **AppConfig**: Base URLs, OAuth endpoints, logging preferences
- **EndpointConfig**: Specific API endpoint paths
- **IOAuthProvider**: Your authentication implementation
- **CancellationToken**: Graceful shutdown control

This architecture ensures reliable, long-running connections with automatic error recovery and proper resource management.

#### OpenAPI Specification
****Full API documentation**** available at [https://streaming.morningstar.com/direct-web-services/swagger/index.html](https://streaming.morningstar.com/direct-web-services/swagger/index.html)

## Extending This Example

### Adding Your Own Logic

To create a subscription with real data, run the subscription code in `Program.cs` and provide:

1. **OAuth Credentials**: Update the `ExampleOAuthProvider` with your Morningstar API credentials or Implement your own OAuthProvider implementation
2. **Investment Identifiers**: Specify the securities you want to track
3. **Duration**: How long the subscription should run

#### Configuring OAuth Authentication

The sample includes an `ExampleOAuthProvider` that you can update with your Morningstar API credentials as a quick start test. 
It is recommended to implement your own OAuthProvider implementation to retrieve your Morningstar API credentials from a secure key vault or secrets manager.
The `ITokenProvider` service will use these credentials to automatically fetch and manage OAuth bearer tokens.

**Update ExampleOAuthProvider.cs:**

```csharp
public class ExampleOAuthProvider : IOAuthProvider
{
    public Task<OAuthSecret> GetOAuthSecretAsync()
    {
        // TODO: Implement your custom logic to retrieve credentials from a secure store
        // Examples: Azure Key Vault, AWS Secrets Manager, environment variables, etc.
        
        var secret = new OAuthSecret
        {
            UserName = "{YOUR_USERNAME}",  // Replace with your Morningstar API username
            Password = "{YOUR_PASSWORD}"   // Replace with your Morningstar API password
        };
        return Task.FromResult(secret);
    }
}
```

**Best Practices:**
- ✅ **Never hardcode credentials** in source code
- ✅ **Use secure storage**: Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, etc.
- ✅ **Environment-specific configs**: Different credentials for dev/staging/production
- ✅ The `ITokenProvider` service automatically handles token fetching and caching for you

**How it works:**
1. Your `IOAuthProvider` returns credentials via `GetOAuthSecretAsync()`
2. `ITokenProvider` uses these credentials to call the Morningstar OAuth endpoint
3. The resulting bearer token is cached and automatically refreshed as needed
4. All API and WebSocket requests include the valid token in Authorization headers

#### Creating Subscriptions

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

## Additional Resources

- See `..\README.md` for detailed API documentation
- Check the `Morningstar.Streaming.Domain` project for all available models and contracts
- **OpenAPI Specification**: Full API documentation available at [https://streaming.morningstar.com/direct-web-services/swagger/index.html](https://streaming.morningstar.com/direct-web-services/swagger/index.html)

## Support

For questions about this example or the Morningstar Streaming Client libraries, please contact your Morningstar support team.
