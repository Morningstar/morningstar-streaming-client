using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Morningstar.Streaming.Client.Services;
using Morningstar.Streaming.Client.Services.Telemetry;

namespace Morningstar.Streaming.Client.Sample.Services.Telemetry;

/// <summary>
/// Sample IHostedService that observes ICanaryService's subscription lifecycle events and
/// periodically logs aggregate disconnect/reconnect counts.
/// </summary>
public class WebSocketLifecycleMetricLogger : IHostedService, IDisposable
{
    private readonly ICanaryService canaryService;
    private readonly AtomicCounter disconnectionCounter = new();
    private readonly AtomicCounter reconnectionCounter = new();
    private Timer? timer;
    private readonly ILogger<WebSocketLifecycleMetricLogger> logger;
    private bool disposed;

    public WebSocketLifecycleMetricLogger(ICanaryService canaryService, ILogger<WebSocketLifecycleMetricLogger> logger)
    {
        this.canaryService = canaryService;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        canaryService.SubscriptionDisconnected += OnSubscriptionDisconnected;
        canaryService.SubscriptionReconnected += OnSubscriptionReconnected;
        timer = new Timer(LogAndCleanup, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }

    private void OnSubscriptionDisconnected(Guid subscriptionId, Guid topicGuid, string? purpose, string webSocketUrl, string disconnectType)
    {
        disconnectionCounter.Increment();
        logger.LogInformation(
            "Observed disconnect. SubscriptionId: {SubscriptionId}. TopicGuid: {TopicGuid}. Purpose: {Purpose}. DisconnectType: {DisconnectType}. WebSocketUrl: {WebSocketUrl}",
            subscriptionId, topicGuid, purpose, disconnectType, webSocketUrl);
    }

    private void OnSubscriptionReconnected(Guid subscriptionId, Guid topicGuid, string? purpose, string webSocketUrl, string previousDisconnectType)
    {
        reconnectionCounter.Increment();
        logger.LogInformation(
            "Observed reconnect. SubscriptionId: {SubscriptionId}. TopicGuid: {TopicGuid}. Purpose: {Purpose}. PreviousDisconnectType: {PreviousDisconnectType}. WebSocketUrl: {WebSocketUrl}",
            subscriptionId, topicGuid, purpose, previousDisconnectType, webSocketUrl);
    }

    internal void LogAndCleanup(object? state)
    {
        try
        {
            var disconnectionCount = disconnectionCounter.ResetAndGet();
            var reconnectionCount = reconnectionCounter.ResetAndGet();

            if (disconnectionCount > 0)
            {
                logger.LogInformation("[Counter] Total Disconnections: {Count}", disconnectionCount);
            }

            if (reconnectionCount > 0)
            {
                logger.LogInformation("[Counter] Total Reconnections: {Count}", reconnectionCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in disconnection counter logger cleanup cycle");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        canaryService.SubscriptionDisconnected -= OnSubscriptionDisconnected;
        canaryService.SubscriptionReconnected -= OnSubscriptionReconnected;
        timer?.Dispose();
        return Task.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                timer?.Dispose();
            }
            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
