# Morningstar Snapshot Client Library

## Overview

This library provides a reusable, framework-agnostic implementation for requesting on-demand market data snapshots from the Morningstar Snapshot API. It can be used in **any .NET 8.0 application** including:

- Console applications
- ASP.NET Core Web APIs
- Background services
- WPF/WinForms desktop apps
- Blazor applications
- Azure Functions

## Projects

| Project | Description |
|---------|-------------|
| `Morningstar.Snapshot.Client` | Core client library — services, HTTP client, DI registration |
| `Morningstar.Snapshot.Domain` | Domain models, contracts, config POCOs, and constants |
| `Morningstar.Snapshot.Client.Sample` | Console app demonstrating end-to-end usage |
| `Morningstar.Snapshot.Client.Tests` | xUnit unit tests for client and domain code |
| `Morningstar.Snapshot.Client.BDD.Tests` | SpecFlow BDD scenarios for end-to-end snapshot flows |

## Key Features

- ✅ **On-demand snapshot requests** for Level 1 market data
- ✅ **Polymorphic message deserialisation** via `IMessageConverter`
- ✅ **OAuth 2.0 authentication** with pluggable `IOAuthProvider`
- ✅ **Dependency injection ready** via `services.AddSnapshotServices()`
- ✅ **Configurable via `appsettings.json`**
- ✅ **Comprehensive logging support**

## Dependencies

This library uses standard `Microsoft.Extensions.*` abstractions:

| Package | Purpose |
|---------|---------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | Dependency injection |
| `Microsoft.Extensions.Options` | Configuration binding |
| `Microsoft.Extensions.Logging.Abstractions` | Logging |
| `Microsoft.Extensions.Http` | `HttpClient` factory |

## Getting Started

### 1. Reference the Libraries

Add project references to your `.csproj`:

```xml
<ProjectReference Include="..\Morningstar.Snapshot.Client\Morningstar.Snapshot.Client.csproj" />
<ProjectReference Include="..\Morningstar.Snapshot.Domain\Morningstar.Snapshot.Domain.csproj" />
```

### 2. Configure Services

```csharp
using Morningstar.Snapshot.Client.Extensions;
using Morningstar.Snapshot.Client.Services.OAuthProvider;

services.Configure<AppConfig>(configuration.GetSection("AppConfig"));
services.Configure<EndpointConfig>(configuration.GetSection("EndpointConfig"));

// Register your IOAuthProvider implementation
services.AddSingleton<IOAuthProvider, ExampleOAuthProvider>();

// Register all Snapshot Client services
services.AddSnapshotServices();
```

### 3. Configure `appsettings.json`

```json
{
  "AppConfig": {
    "SnapshotApiBaseAddress": "https://snapshot.morningstar.com",
    "OAuthAddress": "https://www.us-api.morningstar.com/token/oauth/",
    "LogMessages": false,
    "LogMessagesPath": "logs"
  },
  "EndpointConfig": {
    "Level1UrlAddress": "direct-web-services/snapshot/v1/level-1"
  }
}
```

### 4. Request a Snapshot

```csharp
using Morningstar.Snapshot.Client.Services;
using Morningstar.Snapshot.Domain.Constants;
using Morningstar.Snapshot.Domain.Contracts;
using System.Net;

var snapshotService = serviceProvider.GetRequiredService<ISnapshotService>();

var request = new SnapshotRequest
{
    Investments = new InvestmentsRequest
    {
        IdType = "PerformanceId",
        Ids = new List<string> { "0P0000038R", "0P000003X1" }
    },
    EventTypes = new List<string>
    {
        EventTypes.LastPrice,
        EventTypes.TopOfBook,
        EventTypes.Trade
    }
};

var response = await snapshotService.RequestSnapshotAsync(request);

if (response.StatusCode == HttpStatusCode.OK)
{
    // response.Data.Realtime — real-time instrument event data
    // response.Data.Delayed  — delayed instrument event data
    // response.MetaData.Messages — warnings (e.g. unrecognised investment IDs)
}
```

## Architecture

```
+--------------+    +-----------------+    +-----------------+    +------------------+    +------------------+
|   Your App   | -> | SnapshotService | -> | SnapshotFactory | -> | SnapshotApiClient| -> |  Snapshot API    |
|              |    | (orchestration) |    | (URL building)  |    | (HTTP + auth)    |    | (HTTP endpoints) |
+--------------+    +-----------------+    +-----------------+    +------------------+    +------------------+
```

## Core Services

| Component | Responsibility |
|-----------|----------------|
| `ISnapshotService` | Entry point — call `RequestSnapshotAsync(SnapshotRequest)` to retrieve a snapshot |
| `SnapshotFactory` | Constructs the endpoint URL from `AppConfig` and `EndpointConfig`, then delegates to `SnapshotApiClient` |
| `ISnapshotApiClient` | Attaches the bearer token, serialises the request body, and returns the raw `SnapshotResponse` |
| `ITokenProvider` | Fetches a bearer token from the OAuth endpoint on each request using `IOAuthProvider` credentials |
| `IOAuthProvider` | Supplies credentials — implement this interface to integrate your secret store |

## Authentication

Implement `IOAuthProvider` to provide credentials:

```csharp
public class MyOAuthProvider : IOAuthProvider
{
    public Task<OAuthSecret> GetOAuthSecretAsync()
    {
        // Retrieve from Key Vault, env vars, or another secure store
        return Task.FromResult(new OAuthSecret
        {
            UserName = Environment.GetEnvironmentVariable("MS_USERNAME")!,
            Password = Environment.GetEnvironmentVariable("MS_PASSWORD")!
        });
    }
}
```

Security best practices:
- Retrieve credentials from a secrets manager (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
- Prefer managed identities or short-lived credentials over static secrets
- Never hardcode credentials in source code or configuration files

## Sample Application

A complete working example is in `Morningstar.Snapshot.Client.Sample`. See its [README](Morningstar.Snapshot.Client.Sample/README.md) for full setup instructions.

```bash
cd Morningstar.Snapshot.Client.Sample
dotnet run
```

## Best Practices

1. **Use dependency injection** — never manually instantiate services
2. **Implement `IOAuthProvider` securely** — retrieve credentials from a secrets store
3. **Check `response.StatusCode`** before processing response data
4. **Check `response.MetaData.Messages`** to detect partial failures (e.g. invalid investment IDs)
5. **Configure logging** — use Serilog or another `Microsoft.Extensions.Logging` provider

## Troubleshooting

**Services not resolving**
Ensure `services.AddSnapshotServices()` is called and `IOAuthProvider` is registered.

**Configuration values are null**
Verify `appsettings.json` is copied to output and section names match (`AppConfig`, `EndpointConfig`).

**Authentication failed**
Confirm credentials are correct and `AppConfig:OAuthAddress` is reachable.

**Snapshot request returns non-200**
Check `response.MetaData.Messages` for details and verify `AppConfig:SnapshotApiBaseAddress` and `EndpointConfig:Level1UrlAddress` are set correctly.

## Support

For questions or issues, please contact your Morningstar support team.

## License

This library is provided for integration purposes with the Morningstar Snapshot API.
