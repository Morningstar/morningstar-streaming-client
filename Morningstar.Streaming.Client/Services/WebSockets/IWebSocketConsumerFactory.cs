namespace Morningstar.Streaming.Client.Services.WebSockets
{
    public interface IWebSocketConsumerFactory
    {
        IWebSocketConsumer Create(string wsUrl, bool logToFile);
    }
}
