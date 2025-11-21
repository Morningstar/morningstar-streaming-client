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
    }
}
