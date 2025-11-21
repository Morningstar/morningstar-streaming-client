namespace Morningstar.Streaming.Domain.Config
{
    public class AppConfig
    {
        public string StreamingApiBaseAddress { get; set; } = string.Empty;
        public string OAuthAddress { get; set; } = string.Empty;
        public string? OAuthSecretName { get; set; }
        public uint ConnectionStringTtl { get; set; }
        public bool LogMessages { get; set; }
        public string LogMessagesPath { get; set; } = "logs";
    }
}
