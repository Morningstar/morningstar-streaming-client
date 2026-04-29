# Morningstar Streaming Client Library

## Overview

This library provides a reusable, framework-agnostic implementation for interacting with the Morningstar Streaming API. It can be used in **any .NET 8.0 application** including:
- Console applications
- WPF/WinForms desktop apps
- Blazor applications
- ASP.NET Core Web APIs
- Background services
- Azure Functions

## Key Features

- ✅ **WebSocket-based real-time data streaming**
- ✅ **Level 1 market data subscriptions**
- ✅ **Subscription lifecycle management**
- ✅ **Comprehensive logging support**
- ✅ **Dependency injection ready**
- ✅ **Configurable via appsettings.json**

## Dependencies

This library uses standard Microsoft.Extensions.* abstractions for:
- **Dependency Injection** (`Microsoft.Extensions.DependencyInjection.Abstractions`)
- **Configuration** (`Microsoft.Extensions.Options`)
- **Logging** (`Microsoft.Extensions.Logging.Abstractions`)
- **HTTP Client** (`Microsoft.Extensions.Http`)

These are industry-standard abstractions that work with any .NET application.

## Getting Started

### 1. Add NuGet References

Add the following NuGet packages to your project:

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
```

### 2. Reference the Libraries

Add project references to your `.csproj` file:

```xml
<ProjectReference Include="..\Morningstar.Streaming.Client\Morningstar.Streaming.Client.csproj" />
<ProjectReference Include="..\Morningstar.Streaming.Domain\Morningstar.Streaming.Domain.csproj" />
```

### 3. Configure Services

In your application startup, register the Morningstar Streaming Client services:

```csharp
using Morningstar.Streaming.Client.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

// Build your service collection
var services = new ServiceCollection();

// Add configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Register configuration sections
services.Configure<AppConfig>(configuration.GetSection("AppConfig"));
services.Configure<EndpointConfig>(configuration.GetSection("EndpointConfig"));

// Register your OAuth provider for your authentication
services.AddSingleton<IOAuthProvider, ExampleOAuthProvider>();

// Register Morningstar Streaming Client services
services.AddStreamingServices();

// Optional: Add hosted services (for background counter logging)
services.AddStreamingHostedServices();

var serviceProvider = services.BuildServiceProvider();
```

### 4. Configure appsettings.json

Create an `appsettings.json` file with the following structure:

```json
{
  "AppConfig": {
    "StreamingApiBaseAddress": "https://streaming.morningstar.com"
  },
  "EndpointConfig": {
    "Level1UrlAddress": "direct-web-services/v1/streaming/level-1"
  }
}
```

### 5. Use the Services

```csharp
using Morningstar.Streaming.Client.Services;
using Morningstar.Streaming.Domain.Contracts;

// Resolve the canary service
var canaryService = serviceProvider.GetRequiredService<ICanaryService>();

// Create a subscription request
var subscriptionRequest = new StartSubscriptionRequest
{
    Stream = new StreamRequest
    {
        Investments = new List<Investments>
        {
            new Investments
            { 
                IdType = "PerformanceId",
                Ids = new List<string> { "0P000003PE" }
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

// Start a subscription 
var response = await canaryService.StartLevel1SubscriptionAsync(accessToken, subscriptionRequest);

if (response.ApiResponse.StatusCode == System.Net.HttpStatusCode.OK)
{
    Console.WriteLine($"Subscription started! GUID: {response.SubscriptionGuid}");
    
    // Get active subscriptions
    var activeSubscriptions = canaryService.GetActiveSubscriptions();
    Console.WriteLine($"Active subscriptions: {activeSubscriptions.Count}");
    
    // Later: Stop the subscription
    if (response.SubscriptionGuid.HasValue)
    {
        await canaryService.StopSubscriptionAsync(response.SubscriptionGuid.Value);
    }
}
```

## Sample Applications

### Console Application Example

A complete working example is available in the `Morningstar.Streaming.Client.Sample` project. This demonstrates:
- Setting up dependency injection
- Configuring the application
- Creating and managing subscriptions
- Proper error handling and logging

To run the console example:

```bash
cd Morningstar.Streaming.Client.Sample
dotnet run
```

## Core Services

### ICanaryService

The main service for managing Morningstar Streaming API interactions:

- `StartLevel1SubscriptionAsync()` - Start a new Level 1 subscription
- `StopSubscriptionAsync()` - Stop an active subscription
- `GetActiveSubscriptions()` - Get list of all active subscriptions

### IStreamingApiClient

Low-level client for direct API communication:

- `CreateL1StreamAsync()` - Create a Level 1 stream
- `SubscribeAsync()` - Subscribe to WebSocket updates

### Supporting Services

- **ISubscriptionGroupManager** - Manages subscription lifecycle
- **IStreamSubscriptionFactory** - Creates stream subscriptions
- **IWebSocketConsumerFactory** - Creates WebSocket consumers
- **ICounterLogger** - Background service for logging metrics

## Architecture

```
┌─────────────────────────────────────────┐
│   Your Application                      │
│   (Console, Web API, Desktop, etc.)     │
└─────────────────┬───────────────────────┘
                  │
                  ↓
┌─────────────────────────────────────────┐
│   Morningstar.Streaming.Client          │
│   - Services (Business Logic)           │
│   - Clients (API Communication)         │
│   - Helpers (HTTP, Utilities)           │
└─────────────────┬───────────────────────┘
                  │
                  ↓
┌─────────────────────────────────────────┐
│   Morningstar.Streaming.Domain          │
│   - Models (Data Structures)            │
│   - Contracts (DTOs)                    │
│   - Config (Configuration POCOs)        │
└─────────────────────────────────────────┘
```

## Best Practices

1. **Always use dependency injection** - Don't manually instantiate services
2. **Configure logging** - Use Serilog or another logging provider
3. **Handle access tokens securely** - Never hardcode tokens
4. **Manage subscription lifecycle** - Always stop subscriptions when done
5. **Monitor active subscriptions** - Use `GetActiveSubscriptions()` regularly
6. **Handle errors gracefully** - Check `ApiResponse.StatusCode` before processing

## Troubleshooting

### Common Issues

**Problem**: Services not resolving  
**Solution**: Ensure you called `services.AddStreamingServices()`

**Problem**: Configuration values are null  
**Solution**: Check that configuration sections are properly registered with `services.Configure<T>()`

**Problem**: WebSocket connection fails  
**Solution**: Verify the access token is valid and the API base address is correct

## Support & Contributing

For questions or issues related to the Morningstar Streaming Client library, please contact your Morningstar support team.

## License

This library is provided for integration purposes with the Morningstar Streaming API.
