using Morningstar.Streaming.Domain;

namespace Morningstar.Streaming.Client.Services.Subscriptions;

public class StreamSubscriptionResult
{
    public StreamResponse ApiResponse { get; set; }
    public List<string> WebSocketUrls { get; set; }
    public CancellationTokenSource CancellationTokenSource { get; set; }
}
