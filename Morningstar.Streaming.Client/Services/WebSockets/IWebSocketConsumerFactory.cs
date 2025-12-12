namespace Morningstar.Streaming.Client.Services.WebSockets
{
    /// <summary>
    /// Factory for creating WebSocket consumer instances.
    /// </summary>
    public interface IWebSocketConsumerFactory
    {
        /// <summary>
        /// Creates a new WebSocket consumer for the specified URL.
        /// </summary>
        /// <param name="wsUrl">The WebSocket URL to connect to</param>
        /// <param name="logToFile">Whether to log messages to a file</param>
        /// <returns>A new instance of IWebSocketConsumer</returns>
        IWebSocketConsumer Create(string wsUrl, bool logToFile);
    }
}
