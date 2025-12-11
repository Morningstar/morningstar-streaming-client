using Microsoft.Extensions.Logging;

namespace Morningstar.Streaming.Client.Services.WebSockets;

/// <summary>
/// Factory for creating loggers specific to WebSocket subscription topics.
/// </summary>
public interface IWebSocketLoggerFactory
{
    /// <summary>
    /// Gets or creates a logger for a specific subscription topic.
    /// </summary>
    /// <param name="topicGuid">The unique identifier for the subscription topic</param>
    /// <returns>A logger instance for the specified topic</returns>
    ILogger GetLogger(Guid topicGuid);
}
