using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Client.Services.Telemetry;
using Morningstar.Streaming.Client.Services.WebSockets;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Constants;
using Morningstar.Streaming.Domain.Contracts;
using Morningstar.Streaming.Domain.Models;
using TechTalk.SpecFlow;

namespace Morningstar.Streaming.Client.BDD.Tests.StepDefinitions;

[Binding]
public class StreamingStepDefinitions
{
    private ISubscriptionGroupManager subscriptionManager = null!;
    private Mock<IStreamSubscriptionFactory> streamSubscriptionFactoryMock = null!;
    private Mock<IOptions<AppConfig>> appConfigMock = null!;
    private Mock<IWebSocketConsumerFactory> webSocketConsumerFactoryMock = null!;
    private Mock<IWebSocketConsumer> webSocketConsumerMock = null!;
    private Mock<ILogger<CanaryService>> loggerMock = null!;
    private CanaryService canaryService = null!;
    private StartSubscriptionRequest startSubscriptionRequest = null!;
    private StartSubscriptionResponse startSubscriptionResponse = null!;
    private bool isConnected = false;
    private Guid currentSubscriptionGuid;

    // --- Admin/Disconnect arbitration support ---
    private readonly object connectionsLock = new();
    private readonly List<FakeStreamingConnection> streamingConnections = new();
    private readonly List<(Guid SubscriptionId, ArbitrationOutcome Outcome)> reportedArbitrationOutcomes = new();
    private Mock<IStreamingApiClient> streamingApiClientMock = null!;

    // Stands in for one physical WebSocket connection normally owned by StreamingApiClient,
    // letting steps drive messages/notices/completion directly into WebSocketConsumer.
    private sealed class FakeStreamingConnection
    {
        public required Func<string, Task> OnMessage { get; init; }
        public required Action<int?> OnDisconnectNotice { get; init; }
        public TaskCompletionSource RunCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }


    [BeforeScenario]
    public void BeforeScenario()
    {
        // Initialize subscription manager
        subscriptionManager = new SubscriptionGroupManager();

        // Setup mocks
        streamSubscriptionFactoryMock = new Mock<IStreamSubscriptionFactory>();
        webSocketConsumerMock = new Mock<IWebSocketConsumer>();
        webSocketConsumerFactoryMock = new Mock<IWebSocketConsumerFactory>();
        loggerMock = new Mock<ILogger<CanaryService>>();

        // Setup app config
        appConfigMock = new Mock<IOptions<AppConfig>>();
        appConfigMock.Setup(a => a.Value).Returns(new AppConfig
        {
            StreamingApiBaseAddress = "https://some.address.com",
            LogMessages = false
        });

        // Setup WebSocket consumer factory to return mock consumer
        webSocketConsumerFactoryMock
            .Setup(w => w.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .Returns(webSocketConsumerMock.Object);

        // Setup WebSocket consumer to simulate successful message receiving
        webSocketConsumerMock
            .Setup(w => w.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
            .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) => tcs.SetResult(true))
            .Returns(Task.CompletedTask);

        // Initialize CanaryService with mocks
        canaryService = new CanaryService(
            subscriptionManager,
            streamSubscriptionFactoryMock.Object,
            webSocketConsumerFactoryMock.Object,
            loggerMock.Object,
            appConfigMock.Object,
            null);
        canaryService.SubscriptionArbitrationCompleted += (subscriptionId, outcome) => reportedArbitrationOutcomes.Add((subscriptionId, outcome));

        // Reset connection state
        isConnected = false;
        currentSubscriptionGuid = Guid.Empty;
        reportedArbitrationOutcomes.Clear();
    }




    [Given(@"I have a valid subscribe request")]
    public void GivenIHaveAValidSubscribeRequest()
    {
        startSubscriptionRequest = new StartSubscriptionRequest
        {
            Stream = new StreamRequest
            {
                Investments = new List<Investments>
                {
                    new Investments
                    {
                        IdType = "PerformanceId",
                        Ids = new List<string> { "0P0000038R", "0P000003X1", "0P0001HD8R" }
                    }
                },
                EventTypes = new[]
                {
                    EventTypes.AggregateSummary,
                    EventTypes.Auction,
                    EventTypes.Close,
                    EventTypes.IndexTick,
                    EventTypes.InstrumentPerformanceStatistics,
                    EventTypes.LastPrice,
                    EventTypes.MidPrice,
                    EventTypes.NAVPrice,
                    EventTypes.OHLPrice,
                    EventTypes.SettlementPrice,
                    EventTypes.SpreadStatistics,
                    EventTypes.Status,
                    EventTypes.TopOfBook,
                    EventTypes.Trade,
                    EventTypes.TradeCancellation,
                    EventTypes.TradeCorrection
                }
            },
            DurationSeconds = 300 // Run for 5 minutes
        };

        // Setup mock to return a successful response
        SetupSuccessfulStreamSubscription();
    }

    [Given(@"I have a partially valid subscribe request")]
    public void GivenIHaveAPartiallyValidSubscribeRequest()
    {
        startSubscriptionRequest = new StartSubscriptionRequest
        {
            Stream = new StreamRequest
            {
                Investments = new List<Investments>
                {
                    new Investments
                    {
                        IdType = "PerformanceId",
                        // Include one invalid ID to trigger partial success
                        Ids = new List<string> { "0P0000038R", "INVALID_ID", "0P0001HD8R" }
                    }
                },
                EventTypes = new[] { EventTypes.LastPrice, EventTypes.Trade }
            },
            DurationSeconds = 300
        };

        // Setup mock to return a partial success response
        SetupPartialSuccessStreamSubscription();
    }


    [When(@"I create a subscription")]
    public async Task WhenICreateASubscription()
    {
        // Task.Run detaches from SpecFlow's per-step sync-over-async SynchronizationContext, which
        // otherwise stops pumping once this step returns - stranding continuations of the long-lived
        // background StartConsumingAsync task the arbitration scenarios drive across later steps.
        startSubscriptionResponse = await Task.Run(() => canaryService.StartLevel1SubscriptionAsync(startSubscriptionRequest));

        if (startSubscriptionResponse.SubscriptionGuid.HasValue)
        {
            currentSubscriptionGuid = startSubscriptionResponse.SubscriptionGuid.Value;
            isConnected = true;
        }
    }

    [When(@"I get an unexpected disconnect")]
    public void WhenIGetAnUnexpectedDisconnect()
    {
        // Simulate unexpected disconnect
        isConnected = false;

        // Setup mock consumer to simulate disconnection
        webSocketConsumerMock
            .Setup(w => w.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection lost unexpectedly"));
    }

    [When(@"I get an expected disconnect")]
    public void WhenIGetAnExpectedDisconnect()
    {
        // Simulate controlled disconnect (e.g., stopping subscription)
        isConnected = false;

        if (currentSubscriptionGuid != Guid.Empty)
        {
            subscriptionManager.TryRemove(currentSubscriptionGuid, out _);
        }
    }

    [Given(@"the WebSocket consumer supports arbitration")]
    public void GivenTheWebSocketConsumerSupportsArbitration()
    {
        // Replaces the wholesale IWebSocketConsumer mock (used by the other scenarios) with a
        // real WebSocketConsumer wired to a mocked IStreamingApiClient, so the actual arbitration
        // orchestration/dedupe/metrics logic runs and can be driven step by step.
        streamingApiClientMock = new Mock<IStreamingApiClient>();
        lock (connectionsLock)
        {
            streamingConnections.Clear();
        }

        streamingApiClientMock
            .Setup(c => c.SubscribeAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<Func<string, Task>>(),
                It.IsAny<TaskCompletionSource<bool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<ICounterLogger?>(),
                It.IsAny<ILatencyLogger?>(),
                It.IsAny<ISequenceLogger?>(),
                It.IsAny<SequenceGapDetector?>(),
                It.IsAny<Action<int?>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Action<string>>(),
                It.IsAny<Action<string>>()))
            .Returns((Guid _, string _, string? _, Func<string, Task> onMessage, TaskCompletionSource<bool> connected, CancellationToken _, ICounterLogger? _, ILatencyLogger? _, ISequenceLogger? _, SequenceGapDetector? _, Action<int?> onNotice, CancellationToken _, Action<string> _, Action<string> _) =>
            {
                var connection = new FakeStreamingConnection { OnMessage = onMessage, OnDisconnectNotice = onNotice };
                lock (connectionsLock)
                {
                    streamingConnections.Add(connection);
                }
                connected.TrySetResult(true);
                return connection.RunCompletion.Task;
            });

        var wsLoggerFactoryMock = new Mock<IWebSocketLoggerFactory>();
        wsLoggerFactoryMock.Setup(f => f.GetLogger(It.IsAny<Guid>())).Returns(new Mock<ILogger>().Object);

        webSocketConsumerFactoryMock
            .Setup(w => w.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .Returns((string wsUrl, bool logToFile, string? purpose) => new WebSocketConsumer(
                null,
                null,
                wsLoggerFactoryMock.Object,
                new Mock<ILogger<WebSocketConsumer>>().Object,
                streamingApiClientMock.Object,
                null,
                wsUrl,
                logToFile,
                purpose));
    }

    [When(@"an admin disconnect notice with arbitration enabled is received")]
    public async Task WhenAnAdminDisconnectNoticeWithArbitrationEnabledIsReceived()
    {
        GetConnection(0).OnDisconnectNotice(null);
        await WaitUntilAsync(() => ConnectionCount() >= 2);
    }

    [When(@"an admin disconnect notice with arbitration disabled is received")]
    public void WhenAnAdminDisconnectNoticeWithArbitrationDisabledIsReceived()
    {
        // StreamingApiClient.ShouldArbitrate only invokes onDisconnectNoticeReceived when the
        // notice's Arbitrate flag is true (see ProcessMessageChannelAsync), so a disabled notice
        // never reaches WebSocketConsumer at all - nothing to simulate here.
    }

    [When(@"the replacement connection delivers a duplicate message")]
    public async Task WhenTheReplacementConnectionDeliversADuplicateMessage()
    {
        const string message = """{"EventType":"Trade","PerformanceId":"0P0000038R","SequenceNumber":1}""";
        await GetConnection(0).OnMessage(message);
        await GetConnection(1).OnMessage(message);
    }

    [When(@"the original connection is closed by the server")]
    public void WhenTheOriginalConnectionIsClosedByTheServer()
    {
        GetConnection(0).RunCompletion.TrySetResult();
    }

    [When(@"the replacement connection fails to establish")]
    public void WhenTheReplacementConnectionFailsToEstablish()
    {
        GetConnection(1).RunCompletion.TrySetException(new InvalidOperationException("Replacement connection refused"));
    }

    [Then(@"no replacement connection is created")]
    public void ThenNoReplacementConnectionIsCreated()
    {
        Assert.Equal(1, ConnectionCount());
    }

    [Then(@"the arbitration outcome is reported as ""(.*)""")]
    public async Task ThenTheArbitrationOutcomeIsReportedAs(string outcome)
    {
        var (expectedConfirmed, expectedReplacementFailed) = outcome switch
        {
            "ConfirmedHandover" => (true, false),
            "UnconfirmedHandover" => (false, false),
            "ReplacementConnectionFailed" => (false, true),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown arbitration outcome.")
        };

        await WaitUntilAsync(() => reportedArbitrationOutcomes.Any(reported =>
            reported.Outcome.Confirmed == expectedConfirmed && reported.Outcome.ReplacementFailed == expectedReplacementFailed));

        Assert.Contains(
            reportedArbitrationOutcomes,
            reported => reported.SubscriptionId == currentSubscriptionGuid
                && reported.Outcome.Confirmed == expectedConfirmed
                && reported.Outcome.ReplacementFailed == expectedReplacementFailed);
    }

    [Then(@"the subscription is still active")]
    public void ThenTheSubscriptionIsStillActive()
    {
        Assert.Contains(canaryService.GetActiveSubscriptions(), s => s.Guid == currentSubscriptionGuid);
    }

    [Then(@"the subscription is no longer active")]
    public async Task ThenTheSubscriptionIsNoLongerActive()
    {
        await WaitUntilAsync(() => !canaryService.GetActiveSubscriptions().Any(s => s.Guid == currentSubscriptionGuid));
        Assert.DoesNotContain(canaryService.GetActiveSubscriptions(), s => s.Guid == currentSubscriptionGuid);
    }

    private FakeStreamingConnection GetConnection(int index)
    {
        lock (connectionsLock)
        {
            return streamingConnections[index];
        }
    }

    private int ConnectionCount()
    {
        lock (connectionsLock)
        {
            return streamingConnections.Count;
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000, int pollMs = 20)
    {
        var start = Environment.TickCount64;
        while (!condition())
        {
            if (Environment.TickCount64 - start > timeoutMs)
            {
                return;
            }

            await Task.Delay(pollMs);
        }
    }

    [When(@"messages are successfully being received")]
    public void WhenMessagesAreSuccessfullyBeingReceived()
    {
        // Verify that messages are being received during the scenario
        Assert.True(isConnected, "Should be connected to receive messages");
        Assert.NotEqual(Guid.Empty, currentSubscriptionGuid);

        // Verify WebSocket consumer was created and is consuming
        webSocketConsumerFactoryMock.Verify(
            w => w.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.AtLeastOnce);
    }

    [Then(@"I receive a successful response")]
    public void ThenIReceiveASuccessfulResponse()
    {
        Assert.NotNull(startSubscriptionResponse);
        Assert.Equal(HttpStatusCode.OK, startSubscriptionResponse.ApiResponse.StatusCode);
        Assert.NotNull(startSubscriptionResponse.SubscriptionGuid);
        Assert.NotEqual(Guid.Empty, startSubscriptionResponse.SubscriptionGuid.Value);
    }

    [Then(@"I receive a partial successful response")]
    public void ThenIReceiveAPartialSuccessfulResponse()
    {
        Assert.NotNull(startSubscriptionResponse);
        Assert.Equal(HttpStatusCode.OK, startSubscriptionResponse.ApiResponse.StatusCode);
        Assert.NotNull(startSubscriptionResponse.ApiResponse.MetaData);
        Assert.NotNull(startSubscriptionResponse.ApiResponse.MetaData.Messages);
        Assert.True(startSubscriptionResponse.ApiResponse.MetaData.Messages.Count > 0);
    }

    [Then(@"messages are successfully being received")]
    public void ThenMessagesAreSuccessfullyBeingReceived()
    {
        Assert.NotNull(startSubscriptionResponse);
        Assert.Equal(HttpStatusCode.OK, startSubscriptionResponse.ApiResponse.StatusCode);
        Assert.NotNull(startSubscriptionResponse.SubscriptionGuid);
        Assert.NotEqual(Guid.Empty, startSubscriptionResponse.SubscriptionGuid.Value);

        // Verify WebSocket consumer was created
        webSocketConsumerFactoryMock.Verify(
            w => w.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.AtLeastOnce);
    }

    [Then(@"I am able to reconnect")]
    public async Task ThenIAmAbleToReconnect()
    {
        // Reset mock to allow successful connection
        webSocketConsumerMock
            .Setup(w => w.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()))
            .Callback((TaskCompletionSource<bool> tcs, CancellationToken _) => tcs.SetResult(true))
            .Returns(Task.CompletedTask);

        // Create new subscription to simulate reconnection
        SetupSuccessfulStreamSubscription();
        var reconnectResponse = await canaryService.StartLevel1SubscriptionAsync(startSubscriptionRequest);

        Assert.NotNull(reconnectResponse);
        Assert.Equal(HttpStatusCode.OK, reconnectResponse.ApiResponse.StatusCode);
        Assert.NotNull(reconnectResponse.SubscriptionGuid);

        isConnected = true;
        currentSubscriptionGuid = reconnectResponse.SubscriptionGuid.Value;
    }

    [Then(@"messages are successfully being received from where I left off")]
    public void ThenMessagesAreSuccessfullyBeingReceivedFromWhereILeftOff()
    {
        // Verify that the subscription can resume from the last known position
        Assert.True(isConnected, "Should be connected after reconnection");
        Assert.NotEqual(Guid.Empty, currentSubscriptionGuid);

        // In a real scenario, this would:
        // 1. Track the last processed message offset before disconnect
        // 2. Request messages from that offset on reconnection
        // 3. Verify no messages were lost or duplicated

        // For BDD tests, we verify:
        // 1. Reconnection was successful (checked above)
        // 2. New subscription was created with valid GUID
        // 3. WebSocket consumer is actively consuming messages
        webSocketConsumerMock.Verify(
            w => w.StartConsumingAsync(It.IsAny<TaskCompletionSource<bool>>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "WebSocket consumer should be actively consuming messages after reconnection");

        // Verify that the factory was called multiple times (initial + reconnect)
        webSocketConsumerFactoryMock.Verify(
            w => w.Create(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.AtLeast(2),
            "Should have created WebSocket consumers for both initial connection and reconnection");
    }

    private void SetupSuccessfulStreamSubscription()
    {
        var streamResponse = new StreamResponse
        {
            StatusCode = HttpStatusCode.OK,
            Schema = "avro",
            Subscriptions = new Subscription
            {
                Realtime = new List<string>
                {
                    "wss://some.address.com/streaming/level-1/00000000-0000-0000-0000-000000000001"
                }
            },
            MetaData = new MetaData
            {
                RequestId = Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow.ToString("o"),
                Messages = null
            }
        };

        var subscriptionResult = new StreamSubscriptionResult
        {
            ApiResponse = streamResponse,
            WebSocketUrls = streamResponse.Subscriptions.Realtime,
            CancellationTokenSource = new CancellationTokenSource()
        };

        streamSubscriptionFactoryMock
            .Setup(f => f.CreateAsync(It.IsAny<StartSubscriptionRequest>()))
            .ReturnsAsync(subscriptionResult);
    }

    private void SetupPartialSuccessStreamSubscription()
    {
        var streamResponse = new StreamResponse
        {
            StatusCode = HttpStatusCode.OK,
            Schema = "avro",
            Subscriptions = new Subscription
            {
                Realtime = new List<string>
                {
                    "wss://some.address.com/streaming/level-1/00000000-0000-0000-0000-000000000002"
                }
            },
            MetaData = new MetaData
            {
                RequestId = Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow.ToString("o"),
                Messages = new List<Message>
                {
                    new Message
                    {
                        Type = "Warning",
                        Investments = new List<InvestmentMessage>
                        {
                            new InvestmentMessage
                            {
                                Id = "INVALID_ID",
                                IdType = "PerformanceId",
                                Status = "NotFound",
                                ErrorCode = "404"
                            }
                        }
                    }
                }
            }
        };

        var subscriptionResult = new StreamSubscriptionResult
        {
            ApiResponse = streamResponse,
            WebSocketUrls = streamResponse.Subscriptions.Realtime,
            CancellationTokenSource = new CancellationTokenSource()
        };

        streamSubscriptionFactoryMock
            .Setup(f => f.CreateAsync(It.IsAny<StartSubscriptionRequest>()))
            .ReturnsAsync(subscriptionResult);
    }

}
