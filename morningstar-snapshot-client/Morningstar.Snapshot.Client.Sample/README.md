# Morningstar Snapshot Client - Sample Application

This sample demonstrates how to use the `Morningstar.Snapshot.Client` and `Morningstar.Snapshot.Domain` libraries in a small console application to request on-demand snapshot data from Morningstar APIs.

## What This Example Demonstrates

- ✅ Setting up dependency injection in a console app
- ✅ Configuring services using `appsettings.json`
- ✅ Registering Snapshot client services with the DI container
- ✅ Using an `IOAuthProvider` to supply credentials and `ITokenProvider` to obtain bearer tokens
- ✅ Requesting snapshot data and reading `SnapshotResponse`
- ✅ Proper logging with Serilog
- ✅ Error handling and cancellation best practices

## Prerequisites

- .NET 8.0 SDK or later
- Valid Morningstar API credentials and access to the Snapshot endpoints
- Configured `appsettings.json` with API and endpoint configuration

## Getting Started

### 1. Configure `appsettings.json`

Update the `appsettings.json` file with your Snapshot API configuration. Example keys used by the sample:

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

### 2. Build the Project

```bash
dotnet build
```

### 3. Run the Application

```bash
dotnet run
```

### 4. How these services interact with the Snapshot API

The Morningstar Snapshot client uses a simple layered architecture to perform one‑off snapshot requests and surface parsed results.

#### Architecture Overview

```
┌─────────────┐    ┌──────────────────┐    ┌─────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│   Your App  │──> │ SnapshotService  │──> │SnapshotFactory  │──> │SnapshotApiClient │──> │  Snapshot API    │
│             │    │ (orchestration)  │    │ (URL building)  │    │ (HTTP + auth)    │    │ (HTTP endpoints) │
└─────────────┘    └──────────────────┘    └─────────────────┘    └──────────────────┘    └──────────────────┘
```

#### Workflow

1. Build a snapshot request
  - Describe the investments/identifiers you need (for example, `PerformanceId`, `Ticker`)
  - Configure any request options such as fields, locales, or data variants required by your use case

2. Configure authentication
  - Implement `IOAuthProvider` to supply your credentials
  - `ITokenProvider` is wired internally and called automatically by `SnapshotApiClient` on each request

3. Execute the snapshot request
  - Call `snapshotService.RequestSnapshotAsync(request)` with your `SnapshotRequest`
  - Authentication is handled internally — `SnapshotApiClient` fetches the bearer token via `ITokenProvider` on each call
  - Handle exceptions as appropriate

4. Inspect and process results
  - Check `response.StatusCode == HttpStatusCode.OK` for success
  - Read `response.Data.Realtime` or `response.Data.Delayed` for instrument event data
  - Check `response.MetaData.Messages` for any warnings (e.g. unrecognised investment IDs)

#### Key Components

| Component | Responsibility |
|-----------|----------------|
| `SnapshotService` | High-level API used by your application to request snapshot data |
| `SnapshotApiClient` | Low-level HTTP client that attaches the bearer token, serializes the request, and returns the raw `SnapshotResponse` |
| `SnapshotFactory` | Constructs the endpoint URL from `AppConfig` and `EndpointConfig`, then delegates to `SnapshotApiClient` |
| `ITokenProvider` | Fetches a bearer token from the OAuth endpoint on each request via `IOAuthProvider` credentials |
| `IOAuthProvider` | Supplies credentials to the token provider (sample `ExampleOAuthProvider`) |

#### What the Library Handles for You

- ✅ Authentication: bearer token fetching on each request via `IOAuthProvider`
- ✅ HTTP client configuration and endpoint wiring
- ✅ Request/response mapping into `Morningstar.Snapshot.Domain` models
- ✅ Logging and error propagation for diagnostics

## Extending This Example

### Adding Your Own Logic

To request snapshot data, update `Program.cs` or call the `SnapshotService` from your own code. Provide:

1. `IOAuthProvider` implementation that returns your credentials
2. The identifiers you want to snapshot (e.g., PerformanceId, Ticker)

#### Configuring OAuth Authentication

The sample includes an `ExampleOAuthProvider` that you can update with your Morningstar API credentials as a quick-start test. For production use, implement a secure `IOAuthProvider` that retrieves credentials from a secrets store (for example: Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) or environment variables. Avoid hardcoding secrets in source control or configuration files.

`ITokenProvider` consumes the `IOAuthProvider` to obtain the raw credentials and will automatically fetch, cache, and refresh bearer tokens as needed. Typical responsibilities:

- `IOAuthProvider`: returns an `OAuthSecret` with credentials (or client assertion) from a secure source
- `ITokenProvider`: exchanges credentials for a bearer token on each request

Example quick-start implementation (replace with secure retrieval in real deployments):

### Update ExampleOAuthProvider.cs:

```csharp
public class ExampleOAuthProvider : IOAuthProvider
{
    public Task<OAuthSecret> GetOAuthSecretAsync()
    {
        // Replace with secure secret retrieval (Key Vault, env vars, etc.)
        return Task.FromResult(new OAuthSecret
        {
            UserName = "{YOUR_USERNAME}",
            Password = "{YOUR_PASSWORD}"
        });
    }
}
```

### Registration example (DI):

```csharp
services.AddSingleton<IOAuthProvider, ExampleOAuthProvider>();
services.AddSnapshotServices(); // registers ITokenProvider and SnapshotService
```

Security best practices:

- Use a secrets manager and give the app a minimal identity to read secrets
- Prefer managed identities or short-lived credentials over static secrets
- Log non-sensitive events only; never write secrets to logs

#### Requesting a Snapshot

The sample exposes a `SnapshotService` you can call to request snapshot data. A typical call looks like:

```csharp
var snapshotRequest = new SnapshotRequest
{
    Investments = new InvestmentsRequest
    {
        IdType = "PerformanceId",
        Ids = new List<string> { "0P0000038R" }
    },
    EventTypes = new List<string> { EventTypes.LastPrice }
};

var response = await snapshotService.RequestSnapshotAsync(snapshotRequest);

if (response.StatusCode == HttpStatusCode.OK)
{
    // inspect response.Data.Realtime or response.Data.Delayed
    // check response.MetaData.Messages for any warnings
}
```

#### Request Multiple Event Types

All supported event type constants are defined in `Morningstar.Snapshot.Domain.Constants.EventTypes`:

```csharp
EventTypes = new List<string>
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

## Logging

The sample configures Serilog to log to console and optionally to files. Configure logging in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    }
  },
  "AppConfig": {
    "LogMessages": false,
    "LogMessagesPath": "logs"
  }
}
```

If `AppConfig:LogMessages` is true the sample writes returned payloads to `{LogMessagesPath}` for later inspection.

## Troubleshooting

### "Configuration section not found"
- Ensure `appsettings.json` is copied to output and section names are correct.

### "Unable to resolve service"
- Verify registration in `ServiceCollectionExtensions` (e.g., `services.AddSnapshotServices()` or similar) and that `ExampleOAuthProvider` is registered:

```csharp
services.AddSingleton<IOAuthProvider, ExampleOAuthProvider>();
services.AddSnapshotServices();
```

### "Authentication failed"
- Validate your credentials and that the OAuth endpoint configured in `AppConfig:OAuthAddress` is reachable.

## Next Steps

1. Integrate the snapshot request flow into your application using the same DI pattern
2. Add domain-specific mapping and processing of snapshot results
3. Securely store credentials and rotate them regularly

## Additional Resources

- See `..\README.md` for the top-level project documentation
- Check the `Morningstar.Snapshot.Domain` project for available models and contracts
- An OpenAPI spec for the Snapshot endpoints is not yet available but will be provided in a future release. Once available, use it to verify request/response payloads and explore the full range of supported operations

## Support

For questions about this example or the Morningstar Snapshot Client libraries, please contact your Morningstar support team.

