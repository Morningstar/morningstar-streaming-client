namespace Morningstar.Streaming.Client.Services.WebSockets
{
    /// <summary>
    /// Interface for consuming WebSocket messages from the Morningstar Streaming API.
    /// </summary>
    public interface IWebSocketConsumer
    {
        /// <summary>Raised once per Admin/Disconnect arbitration handover with its outcome.</summary>
        event Action<ArbitrationOutcome>? ArbitrationCompleted;

        /// <summary>
        /// Starts consuming messages from the WebSocket connection.
        /// </summary>
        /// <param name="connectedTcs">TaskCompletionSource to signal when connection is established</param>
        /// <param name="cancellationToken">Cancellation token to stop consuming messages</param>
        Task StartConsumingAsync(TaskCompletionSource<bool> connectedTcs, CancellationToken cancellationToken = default);
    }
}
