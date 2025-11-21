namespace Morningstar.Streaming.Client.Services.WebSockets
{
    public interface IWebSocketConsumer
    {
        Task StartConsumingAsync(CancellationToken cancellationToken = default);
    }
}
