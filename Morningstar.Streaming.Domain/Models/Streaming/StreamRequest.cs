namespace Morningstar.Streaming.Domain.Models.Streaming;

/// <summary>
/// Wraps the /direct-web-services/v1/streaming/level-1 endpoint request.
/// </summary>
public class StreamRequest
{
    public required List<Investment> Investments { get; set; }
    public required List<string> EventTypes { get; set; }
}

public class Investment
{
    public required string IdType { get; set; }
    public required List<string> Ids { get; set; }
}
