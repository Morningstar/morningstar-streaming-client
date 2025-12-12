using System.Runtime.Serialization;

namespace Morningstar.Streaming.Domain.Contracts
{
    [DataContract]
    public class StartSubscriptionRequest : SubscriptionBaseRequest
    {
        [DataMember]
        public StreamRequest Stream { get; set; }
    }
}
