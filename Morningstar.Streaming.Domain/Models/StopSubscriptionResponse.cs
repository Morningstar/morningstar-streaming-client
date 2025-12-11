namespace Morningstar.Streaming.Domain.Models;

/// <summary>
/// Response model for stopping a subscription.
/// </summary>
public class StopSubscriptionResponse
{
    public bool Success { get; set; }

    public Guid? SubscriptionGuid { get; set; }

    public string? Message { get; set; }

    public string? ErrorCode { get; set; }
}
