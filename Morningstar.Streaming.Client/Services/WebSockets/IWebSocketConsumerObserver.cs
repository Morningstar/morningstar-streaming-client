namespace Morningstar.Streaming.Client.Services.WebSockets;

/// <summary>
/// Single consolidated observer for all of a WebSocketConsumer's lifecycle notifications.
/// Optional - assign <see cref="IWebSocketConsumer.Observer"/> to receive them; if left unset,
/// notifications are simply not observed. The library only reports raw facts - implementations
/// decide what (if anything) to do with them, e.g. record metrics.
/// </summary>
public interface IWebSocketConsumerObserver
{
    /// <summary>Raised once per Admin/Disconnect arbitration handover with its outcome.</summary>
    void OnArbitrationCompleted(ArbitrationOutcome outcome);

    /// <summary>Raised whenever this connection ends, with a classification ("Expected"/"Unexpected"/"RetriesExhausted").</summary>
    void OnDisconnected(Guid topicGuid, string? purpose, string webSocketUrl, string disconnectType);

    /// <summary>Raised whenever a reconnect attempt succeeds, with the classification of the disconnect that preceded it.</summary>
    void OnReconnected(Guid topicGuid, string? purpose, string webSocketUrl, string previousDisconnectType);
}
