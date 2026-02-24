using System.Runtime.Serialization;

namespace Morningstar.Streaming.Domain.Contracts
{
    public class SubscriptionBaseRequest
    {
        /// <summary>
        /// Duration in seconds, or null for indefinite.
        /// </summary>
        [DataMember]
        public int? DurationSeconds { get; set; }

        /// <summary>
        /// The format of the streaming data. Supported values are "json" and "csv". Default is "json".
        /// </summary>
        [DataMember]
        public string StreamingFormat { get; set; } = Constants.StreamingFormat.Json;

        /// <summary>
        /// An optional string that describes the purpose of the subscription. This can be used for auditing and monitoring purposes.
        /// </summary>
        [DataMember]
        public string? Purpose { get; set; }
    }
}
