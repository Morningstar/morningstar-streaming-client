using Microsoft.Extensions.Logging;

namespace Morningstar.Streaming.Client.Services.WebSockets
{
    public interface IWebSocketLoggerFactory
    {
        ILogger GetLogger(Guid topicGuid);
    }
}
