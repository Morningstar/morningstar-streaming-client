namespace Morningstar.Streaming.Domain.Models
{
    public class LogFileOptions
    {
        public bool EnableFileLogging { get; set; }
        public string RollingFilePath { get; set; } = null!;
        public long FileSizeLimitBytes { get; set; }
    }
}
