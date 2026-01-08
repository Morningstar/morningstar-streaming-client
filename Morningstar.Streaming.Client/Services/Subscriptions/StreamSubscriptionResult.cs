using Morningstar.Streaming.Domain;

namespace Morningstar.Streaming.Client.Services.Subscriptions;

public class StreamSubscriptionResult
{
    public StreamResponse ApiResponse { get; set; } = null!;
    public List<string> WebSocketUrls { get; set; } = null!;
    public CancellationTokenSource CancellationTokenSource { get; set; } = null!;
}
