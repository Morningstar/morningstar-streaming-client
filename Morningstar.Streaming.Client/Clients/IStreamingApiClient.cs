using Morningstar.Streaming.Domain;

namespace Morningstar.Streaming.Client.Clients;

public interface IStreamingApiClient
{
    /// <summary>
    /// Generic method to create a Level 1 stream with any request type and endpoint.
    /// This allows for flexible endpoint configuration without exposing specific implementation details.
    /// </summary>
    Task<StreamResponse> CreateL1StreamAsync<TRequest>(TRequest streamRequest, string endpointUrl) where TRequest : class;

    /// <summary>
    /// Subscribes to a WebSocket stream for real-time market data.
    /// Automatically handles connection, reconnection (up to 3 attempts), heartbeat monitoring, and message processing.
    /// </summary>
    /// <param name="webSocketUrl">The WebSocket URL to connect to</param>
    /// <param name="onMessageAsync">Callback function to process incoming messages</param>
    /// <param name="cancellationToken">Cancellation token to stop the subscription</param>
    Task SubscribeAsync(string webSocketUrl, Func<string, Task> onMessageAsync, CancellationToken cancellationToken = default);
}
