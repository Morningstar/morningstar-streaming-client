namespace Morningstar.Streaming.Domain.Config
{
    public class EndpointConfig
    {
        public string Level1UrlAddress { get; set; } = string.Empty;
        public string Level1BypassUrlAddress { get; set; } = string.Empty;
        public string Level2UrlAddress { get; set; } = string.Empty;
        public string Level2BypassUrlAddress { get; set; } = string.Empty;
    }
}
