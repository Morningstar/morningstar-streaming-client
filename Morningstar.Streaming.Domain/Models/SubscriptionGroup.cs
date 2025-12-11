namespace Morningstar.Streaming.Domain.Models;

public class SubscriptionGroup
{
    public Guid Guid { get; set; }
    public required List<string> WebSocketUrls { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public required CancellationTokenSource CancellationTokenSource { get; set; }
}

public class SubscriptionGroupView
{
    public Guid Guid { get; set; }
    public required List<string> WebSocketUrls { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
