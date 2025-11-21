using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Helpers;
using Morningstar.Streaming.Client.Services.TokenProvider;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Constants;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace Morningstar.Streaming.Client.Clients
{
    public class StreamingApiClient : IStreamingApiClient
    {
        private readonly IApiHelper apiHelper;
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<StreamingApiClient> logger;
        private readonly TimeSpan heartbeatTimeout = TimeSpan.FromSeconds(15);
        private readonly TimeSpan heartbeatCheckInterval = TimeSpan.FromSeconds(5);

        public StreamingApiClient(IApiHelper apiHelper, ILogger<StreamingApiClient> logger, ITokenProvider tokenProvider)
        {
            this.apiHelper = apiHelper;
            this.tokenProvider = tokenProvider;
            this.logger = logger;
        }

        /// <summary>
        /// Generic method to create a Level 1 stream with any request type and endpoint.
        /// Consolidates the common logic for creating streams regardless of request type or endpoint.
        /// </summary>
        public async Task<StreamResponse> CreateL1StreamAsync<TRequest>(TRequest streamRequest, string endpointUrl) where TRequest : class
        {
            try
            {
                var headers = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Authorization", await tokenProvider.CreateBearerTokenAsync()),
                    new KeyValuePair<string, string>("Accept", "application/json")
                };

                return await apiHelper.ProcessRequestAsync<StreamResponse>(
                    endpointUrl,
                    HttpMethod.Post,
                    headers,
                    streamRequest
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error when attempting to request L1 Stream.");
                throw;
            }
        }

        public async Task SubscribeAsync(
        string webSocketUrl,
        Func<string, Task> onMessageAsync,
        CancellationToken cancellationToken = default)
        {
            await ConnectWithRetryAsync(webSocketUrl, onMessageAsync, cancellationToken);
        }

        private async Task ConnectWithRetryAsync(
        string webSocketUrl,
        Func<string, Task> onMessageAsync,
        CancellationToken cancellationToken)
        {
            int attempt = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                attempt++;

                try
                {
                    using var ws = await ConnectWebSocketAsync(webSocketUrl, cancellationToken);

                    logger.LogInformation("WebSocket connected on attempt {Attempt}.", attempt);

                    await StartReceiveLoopAsync(ws, onMessageAsync, cancellationToken);
                    
                    // Connection ended - loop will retry
                    logger.LogInformation("WebSocket disconnected. Attempting to reconnect...");
                }
                catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
                {
                    // Only exit if cancellation was explicitly requested
                    logger.LogInformation(ex, "Cancellation requested. Stopping WebSocket connection.");
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "WebSocket failed (attempt {Attempt}). Reconnecting...", attempt);

                    // Check if cancellation was requested before delaying
                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogInformation("Cancellation requested during reconnect delay. Stopping WebSocket connection.");
                        return;
                    }

                    var delaySeconds = Math.Min(Math.Pow(2, attempt), 30);
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    }
                    catch (OperationCanceledException delayEx)
                    {
                        // Cancellation during delay - exit gracefully
                        logger.LogInformation(delayEx, "Cancellation requested during reconnect delay. Stopping WebSocket connection.");
                        return;
                    }
                }
            }
        }

        private async Task<ClientWebSocket> ConnectWebSocketAsync(string url, CancellationToken cancellationToken)
        {
            var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("Authorization", await tokenProvider.CreateBearerTokenAsync());

            logger.LogInformation("Connecting WebSocket to {Url}", url);
            await ws.ConnectAsync(new Uri(url), cancellationToken);

            return ws;
        }

        private async Task StartReceiveLoopAsync(
        ClientWebSocket ws,
        Func<string, Task> onMessageAsync,
        CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            var lastHeartbeat = DateTime.UtcNow;

            var heartbeatTask = StartHeartbeatMonitorAsync(ws, () => lastHeartbeat, cancellationToken);

            while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result;

                try
                {
                    result = await ws.ReceiveAsync(buffer, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Receive failed, disconnecting.");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogWarning("Server closed WebSocket.");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if (message.Contains(EventTypes.HeartBeat, StringComparison.OrdinalIgnoreCase))
                    {
                        lastHeartbeat = DateTime.UtcNow;
                        await SendHeartbeatAckAsync(ws, cancellationToken);
                        continue;
                    }

                    await onMessageAsync(message);
                }
            }

            await heartbeatTask;
        }

        private async Task StartHeartbeatMonitorAsync(
            ClientWebSocket ws,
            Func<DateTime> getLastHeartbeat,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                await Task.Delay(heartbeatCheckInterval, cancellationToken);

                if (DateTime.UtcNow - getLastHeartbeat() > heartbeatTimeout)
                {
                    logger.LogWarning("Server heartbeat timeout. Closing WebSocket.");
                    if (ws.State is WebSocketState.Open or WebSocketState.CloseSent)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Heartbeat timeout", cancellationToken);
                    }
                    break;
                }
            }
        }

        private async Task SendHeartbeatAckAsync(ClientWebSocket ws, CancellationToken cancellationToken)
        {
            var acknowledgement = new MessagePacketEnvelope
            {
                EventType = EventTypes.HeartBeatAcknowledged,
                PublishTime = DateTimeHelper.NanosFromEpoch()
            };

            var json = JsonConvert.SerializeObject(acknowledgement);
            var bytes = Encoding.UTF8.GetBytes(json);

            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
        }

    }
}
