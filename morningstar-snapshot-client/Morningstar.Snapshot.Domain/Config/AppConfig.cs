namespace Morningstar.Snapshot.Domain.Config;

public class AppConfig
{
    public string SnapshotApiBaseAddress { get; set; } = string.Empty;
    public string AvroSchemaAddress { get; set; } = string.Empty;
    public string OAuthAddress { get; set; } = string.Empty;
    public string? OAuthSecretName { get; set; }
    public uint ConnectionStringTtl { get; set; }
    public bool LogMessages { get; set; }
    public string LogMessagesPath { get; set; } = "logs";
}
