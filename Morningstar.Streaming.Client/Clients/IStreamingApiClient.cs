using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Domain;

namespace Morningstar.Streaming.Client.Clients
{
    public interface IStreamingApiClient
    {
        /// <summary>
        /// Generic method to create a Level 1 stream with any request type and endpoint.
        /// This allows for flexible endpoint configuration without exposing specific implementation details.
        /// </summary>
        Task<StreamResponse> CreateL1StreamAsync<TRequest>(TRequest streamRequest, string endpointUrl) where TRequest : class;

        /// <summary>
        /// Generic method to create a Level 2 stream with any request type and endpoint.
        /// This allows for flexible endpoint configuration without exposing specific implementation details.
        /// </summary>
        Task<StreamResponse> CreateL2StreamAsync<TRequest>(TRequest streamRequest, string endpointUrl) where TRequest : class;

        /// <summary>
        /// Subscribes to a WebSocket stream for real-time market data.
        /// Automatically handles connection, reconnection (up to 3 attempts), heartbeat monitoring, and message processing.
        /// </summary>
        /// <param name="subscriptionId">Unique identifier for the subscription</param>
        /// <param name="webSocketUrl">The WebSocket URL to connect to</param>
        /// <param name="purpose">Optional purpose or description for the connection</param>
        /// <param name="onMessageAsync">Callback function to process incoming messages</param>
        /// <param name="connected">TaskCompletionSource to signal when subscription is connected</param>
        /// <param name="cancellationToken">Cancellation token to stop the subscription</param>
        Task SubscribeAsync(
            Guid subscriptionId,
            string webSocketUrl,
            string? purpose,
            Func<string, Task> onMessageAsync,
            TaskCompletionSource<bool> connected,
            CancellationToken cancellationToken = default);

        Task SubscribeAsync(
            Guid subscriptionId,
            string webSocketUrl,
            string? purpose,
            Func<string, Task> onMessageAsync,
            TaskCompletionSource<bool> connected,
            CancellationToken cancellationToken,
            ICounterLogger? counterLogger,
            ILatencyLogger? latencyLogger,
            ISequenceLogger? sequenceLogger);

        /// <summary>
        /// Overload that additionally supports proactive arbitration: reacting to an Admin/Disconnect
        /// notice before the server closes the connection, and deliberately retiring a connection with
        /// a graceful close instead of an abort.
        /// </summary>
        /// <param name="onDisconnectNoticeReceived">
        /// Invoked once, as soon as an Admin/Disconnect notice is observed on the connection -
        /// before the server actually closes it, with the notice's NoticeMinutes value (null if not
        /// specified). Callers can use this to proactively establish a replacement connection.
        /// </param>
        /// <param name="gracefulCloseToken">
        /// When cancelled, the current connection is closed with a normal WebSocket close
        /// handshake (instead of being aborted) and the subscription is not retried afterward.
        /// Use this to retire a connection deliberately (e.g. after a replacement has taken over)
        /// without affecting <paramref name="cancellationToken"/>, which still governs the whole subscription.
        /// </param>
        /// <param name="onDisconnected">Invoked with a classification ("Expected"/"Unexpected") whenever this connection ends. The library only reports the classification - callers decide what (if anything) to record.</param>
        /// <param name="onReconnected">Invoked with the classification of the disconnect that preceded it, whenever a reconnect attempt succeeds.</param>
        Task SubscribeAsync(
            Guid subscriptionId,
            string webSocketUrl,
            string? purpose,
            Func<string, Task> onMessageAsync,
            TaskCompletionSource<bool> connected,
            CancellationToken cancellationToken,
            ICounterLogger? counterLogger,
            ILatencyLogger? latencyLogger,
            ISequenceLogger? sequenceLogger,
            Action<int?> onDisconnectNoticeReceived,
            CancellationToken gracefulCloseToken,
            Action<string> onDisconnected,
            Action<string> onReconnected);
    }
}
