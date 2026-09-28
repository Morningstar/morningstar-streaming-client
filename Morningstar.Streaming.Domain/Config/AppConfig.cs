namespace Morningstar.Streaming.Domain.Config
{
    public class AppConfig
    {
        public string StreamingApiBaseAddress { get; set; } = string.Empty;
        public string AvroSchemaAddress { get; set; } = string.Empty;
        public string OAuthAddress { get; set; } = string.Empty;
        public string? OAuthSecretName { get; set; }
        public uint ConnectionStringTtl { get; set; }
        public bool LogMessages { get; set; }
        public string LogMessagesPath { get; set; } = "logs";

        // Fallback used when an Admin/Disconnect+Arbitrate notice doesn't carry its own NoticeMinutes.
        public int DefaultArbitrationRetirementMinutes { get; set; } = 5;
    }
}
