using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Morningstar.Streaming.Domain
{
    public class MessagePacketEnvelope
    {
        public string EventType { get; set; } = null!;
        public string? PerformanceId { get; set; }
        public long? PublishTime { get; set; }
        public long? AcknowledgedTime { get; set; }
        public long? SequenceNumber { get; set; }
        public JObject? Message { get; set; }

        /// <summary>
        /// Avro-derived JSON exposes the event type as a single-element array named "EventTypes".
        /// Map it onto the singular <see cref="EventType"/> so consumers only deal with one representation.
        /// Write-only: not serialized back out.
        /// </summary>
        [JsonProperty("EventTypes")]
        private List<string>? EventTypes
        {
            set
            {
                if (string.IsNullOrEmpty(EventType) && value is { Count: > 0 })
                {
                    EventType = value[0];
                }
            }
        }
    }
}
