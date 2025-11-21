namespace Morningstar.Streaming.Domain.Models
{
    public class LogFileOptions
    {
        public bool EnableFileLogging { get; set; }
        public string RollingFilePath { get; set; }
        public long FileSizeLimitBytes { get; set; }
        public string S3Bucket { get; set; }
        public string S3Folder { get; set; }
    }
}
