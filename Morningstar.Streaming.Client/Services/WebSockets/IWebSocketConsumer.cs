namespace Morningstar.Streaming.Client.Services.WebSockets
{
    /// <summary>
    /// Interface for consuming WebSocket messages from the Morningstar Streaming API.
    /// </summary>
    public interface IWebSocketConsumer
    {
        /// <summary>Raised once per Admin/Disconnect arbitration handover with its outcome.</summary>
        event Action<ArbitrationOutcome>? ArbitrationCompleted;

        /// <summary>Raised when this consumer ends permanently without a replacement connection taking over (e.g. retries exhausted, or a replacement failed to establish).</summary>
        event Action<Guid, string?, string>? ConsumerEndedWithoutReplacement;

        /// <summary>Raised whenever this connection ends, with a classification ("Expected"/"Unexpected"). The library only reports the classification - callers decide what (if anything) to record.</summary>
        event Action<Guid, string?, string, string>? Disconnected;

        /// <summary>Raised whenever a reconnect attempt succeeds, with the classification of the disconnect that preceded it.</summary>
        event Action<Guid, string?, string, string>? Reconnected;

        /// <summary>
        /// Starts consuming messages from the WebSocket connection.
        /// </summary>
        /// <param name="connectedTcs">TaskCompletionSource to signal when connection is established</param>
        /// <param name="cancellationToken">Cancellation token to stop consuming messages</param>
        Task StartConsumingAsync(TaskCompletionSource<bool> connectedTcs, CancellationToken cancellationToken = default);
    }
}
