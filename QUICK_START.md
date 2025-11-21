# Quick Start Guide - Morningstar Streaming Client

This guide will help you quickly integrate the Morningstar Streaming Client libraries into your .NET application.

## 📋 Prerequisites

- .NET 8.0 SDK or later
- Valid Morningstar Streaming API credentials

## 🚀 Quick Start (5 minutes)

### Step 1: Copy the Projects

Copy these three folders into your solution:
```
Morningstar.Streaming.Domain/
Morningstar.Streaming.Client/
Morningstar.Streaming.Client.Sample/  (optional - for reference)
```

### Step 2: Add Project References

In your `.csproj` file:
```xml
<ItemGroup>
  <ProjectReference Include="..\Morningstar.Streaming.Client\Morningstar.Streaming.Client.csproj" />
  <ProjectReference Include="..\Morningstar.Streaming.Domain\Morningstar.Streaming.Domain.csproj" />
</ItemGroup>
```

### Step 3: Install Required Packages

```bash
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Configuration.Json
```

### Step 4: Configure Services

**For Console Apps:**
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Morningstar.Streaming.Client.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register configuration
        services.Configure<AppConfig>(context.Configuration.GetSection("AppConfig"));
        services.Configure<EndpointConfig>(context.Configuration.GetSection("EndpointConfig"));
        
        // Register OAuth provider for your authentication
        services.AddSingleton<IOAuthProvider, ExampleOAuthProvider>();

        // Register Morningstar Streaming Client services - ONE LINE!
        services.AddStreamingServices();

        // If you want background counter logging, uncomment the following line:
        //services.AddStreamingHostedServices();
    })
    .Build();

await host.RunAsync();
```

**For ASP.NET Core Apps:**
```csharp
// In Program.cs
using Morningstar.Streaming.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register configuration
builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));
builder.Services.Configure<EndpointConfig>(builder.Configuration.GetSection("EndpointConfig"));

// Register OAuth provider for your authentication
builder.Services.AddSingleton<IOAuthProvider, ExampleOAuthProvider>();

// Register Morningstar Streaming Client services - ONE LINE!
builder.Services.AddStreamingServices();

// If you want background counter logging, uncomment the following line:
//builder.Services.AddStreamingHostedServices();

var app = builder.Build();
app.Run();
```

### Step 5: Configure appsettings.json

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

### Step 6: Use the Services

```csharp
using Morningstar.Streaming.Client.Services;
using Morningstar.Streaming.Domain.Contracts;

// Get the service from DI
var canaryService = serviceProvider.GetRequiredService<ICanaryService>();

// Create a subscription
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

// Start subscription
var response = await canaryService.StartLevel1SubscriptionAsync(request);

if (response.ApiResponse.StatusCode == System.Net.HttpStatusCode.OK)
{
    Console.WriteLine($"✅ Subscription started: {response.SubscriptionGuid}");
    
    // Later: Stop the subscription
    await canaryService.StopSubscriptionAsync(response.SubscriptionGuid.Value);
}
```

## 📚 What's Included

### Morningstar.Streaming.Domain
- **Data models** and contracts
- **Configuration POCOs**
- **No external dependencies** (pure domain layer)

### Morningstar.Streaming.Client
- **Business logic** for managing subscriptions
- **API clients** for communicating with Morningstar Streaming API
- **WebSocket consumers** for real-time data
- **Easy DI registration** via `AddStreamingServices()`

### Morningstar.Streaming.Client.Sample
- **Complete working example**
- Shows best practices
- Reference implementation

## 🎯 Common Use Cases

### Monitor Multiple Securities
```csharp
Investments = new List<Investments>
{
    new Investments
    { 
        IdType = "PerformanceId",
        Ids = new List<string> { "0P0000038R" ... }
    }
},
```

### Monitor Multiple Event Types
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

### Get Active Subscriptions
```csharp
var activeSubscriptions = canaryService.GetActiveSubscriptions();
foreach (var sub in activeSubscriptions)
{
    Console.WriteLine($"Subscription {sub.Guid}: {sub.WebSocketUrls.Count} connections");
}
```

### Stop a Specific Subscription
```csharp
bool stopped = await canaryService.StopSubscriptionAsync(subscriptionGuid);
```

## 🔧 Troubleshooting

| Problem | Solution |
|---------|----------|
| Services not resolving | Make sure you called `services.AddStreamingServices()` |
| Configuration is null | Check your `appsettings.json` is being copied to output and sections are registered |
| WebSocket connection fails | Verify access token is valid and API URL is correct |
| Build errors | Ensure all package versions match (use 9.0.9 for Microsoft.Extensions.*) |

## 📖 Additional Resources

- **Full API Documentation**: See `Morningstar.Streaming.Client\README.md`
- **Console Example Guide**: See `Morningstar.Streaming.Client.Sample\README.md`

## 💡 Tips

1. **Start with the Console Example** - It's a complete working implementation
2. **Use Dependency Injection** - Don't manually instantiate services
3. **Configure Logging** - Use Serilog or ILogger for better debugging
4. **Manage Token Lifecycle** - Handle token refresh/expiration
5. **Monitor Subscriptions** - Regularly check active subscriptions

## 🆘 Getting Help

If you encounter issues:

1. Check the README files in each project
2. Review the console example for working code
3. Verify your configuration matches the expected format
4. Ensure your access token has the correct permissions

## ⚡ Next Steps

After getting the basic example working:

1. Customize the subscription parameters
2. Add your own business logic on top
3. Implement custom message handlers
4. Add error handling and retry logic
5. Deploy to your target environment

---

**Ready to go?** Start with the `Morningstar.Streaming.Client.Sample` project to see everything in action!
