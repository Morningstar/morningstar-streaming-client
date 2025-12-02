using Newtonsoft.Json.Linq;

namespace Morningstar.Streaming.Domain;

public class MessagePacketEnvelope
{
    public string EventType { get; set; } = null!;
    public string? PerformanceId { get; set; }
    public long? PublishTime { get; set; }
    public long? AcknowledgedTime { get; set; }
    public long? SequenceNumber { get; set; }
    public JObject? Message { get; set; }
}
