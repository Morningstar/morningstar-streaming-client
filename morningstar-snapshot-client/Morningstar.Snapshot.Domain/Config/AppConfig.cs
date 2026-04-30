namespace Morningstar.Snapshot.Domain.Config;

public class AppConfig
{
    public string SnapshotApiBaseAddress { get; set; } = string.Empty;
    public string OAuthAddress { get; set; } = string.Empty;
    public bool LogMessages { get; set; }
}
