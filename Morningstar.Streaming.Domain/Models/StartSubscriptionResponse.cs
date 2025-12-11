namespace Morningstar.Streaming.Domain.Models;

public class StartSubscriptionResponse
{
    public Guid? SubscriptionGuid { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public StreamResponse ApiResponse { get; set; }
}
