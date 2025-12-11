using ILogger = Microsoft.Extensions.Logging.ILogger;
using Serilog;
using Serilog.Extensions.Logging;
using System.Collections.Concurrent;
using Morningstar.Streaming.Domain.Config;
using Microsoft.Extensions.Options;

namespace Morningstar.Streaming.Client.Services.WebSockets;

public class WebSocketLoggerFactory : IWebSocketLoggerFactory
{
    private readonly AppConfig appConfig;
    private readonly ConcurrentDictionary<Guid, ILogger> LoggerCache = new();

    public WebSocketLoggerFactory(IOptions<AppConfig> appConfig)
    {
        this.appConfig = appConfig.Value;
    }

    public ILogger GetLogger(Guid topicGuid)
    {
        return LoggerCache.GetOrAdd(topicGuid, guid =>
        {
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.File
                (
                    $"{appConfig.LogMessagesPath}/ws-subscription-{guid}.txt",
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 100_000_000,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 100,
                    flushToDiskInterval: TimeSpan.FromSeconds(2)
                )
                .CreateLogger();

            return new SerilogLoggerFactory(serilogLogger).CreateLogger("WebSocketSubscription");
        });
    }
}
