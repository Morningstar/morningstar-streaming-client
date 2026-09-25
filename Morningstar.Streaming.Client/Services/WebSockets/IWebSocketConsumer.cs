namespace Morningstar.Streaming.Client.Services.WebSockets
{
    /// <summary>
    /// Interface for consuming WebSocket messages from the Morningstar Streaming API.
    /// </summary>
    public interface IWebSocketConsumer
    {
        /// <summary>Consolidated observer for arbitration/disconnect/reconnect/consumer-ended notifications. Optional - leave unset to not observe them.</summary>
        IWebSocketConsumerObserver? Observer { get; set; }

        /// <summary>
        /// Starts consuming messages from the WebSocket connection.
        /// </summary>
        /// <param name="connectedTcs">TaskCompletionSource to signal when connection is established</param>
        /// <param name="cancellationToken">Cancellation token to stop consuming messages</param>
        Task StartConsumingAsync(TaskCompletionSource<bool> connectedTcs, CancellationToken cancellationToken = default);
    }
}
