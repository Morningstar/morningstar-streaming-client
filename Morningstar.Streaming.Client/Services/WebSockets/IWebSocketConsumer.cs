namespace Morningstar.Streaming.Client.Services.WebSockets
{
    /// <summary>
    /// Interface for consuming WebSocket messages from the Morningstar Streaming API.
    /// </summary>
    public interface IWebSocketConsumer
    {
        /// <summary>
        /// Starts consuming messages from the WebSocket connection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop consuming messages</param>
        Task StartConsumingAsync(CancellationToken cancellationToken = default);
    }
}
