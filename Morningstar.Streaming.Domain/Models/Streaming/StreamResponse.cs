using System.Net;

namespace Morningstar.Streaming.Domain
{
    public class StreamResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string? ErrorCode { get; set; }
        public object? Message { get; set; }
        public string? Schema { get; set; }
        public Subscription? Subscriptions { get; set; }
        public MetaData? MetaData { get; set; }
    }

    public class Subscription
    {
        public List<string>? Realtime { get; set; }
        public List<string>? Delayed { get; set; }
    }

    public class MetaData
    {
        public required string RequestId { get; set; }
        public required string Time { get; set; }
        public List<Message>? Messages { get; set; }
    }

    public class Message
    {
        public required List<InvestmentMessage> Investments { get; set; }
        public required string Type { get; set; }
    }

    public class InvestmentMessage
    {
        public required string Id { get; set; }
        public required string IdType { get; set; }
        public required string Status { get; set; }
        public required string ErrorCode { get; set; }
    }
}
