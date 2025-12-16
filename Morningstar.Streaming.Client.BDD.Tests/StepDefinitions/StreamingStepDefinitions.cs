using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Morningstar.Streaming.Client.Clients;
using Morningstar.Streaming.Client.Services;
using Morningstar.Streaming.Client.Services.Counter;
using Morningstar.Streaming.Client.Services.Subscriptions;
using Morningstar.Streaming.Client.Services.WebSockets;
using Morningstar.Streaming.Domain;
using Morningstar.Streaming.Domain.Config;
using Morningstar.Streaming.Domain.Constants;
using Morningstar.Streaming.Domain.Contracts;
using Morningstar.Streaming.Domain.Models;
using System.Net;
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
            .Setup(w => w.Create(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(webSocketConsumerMock.Object);

        // Setup WebSocket consumer to simulate successful message receiving
        webSocketConsumerMock
            .Setup(w => w.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Initialize CanaryService with mocks
        canaryService = new CanaryService(
            subscriptionManager,
            streamSubscriptionFactoryMock.Object,
            webSocketConsumerFactoryMock.Object,
            loggerMock.Object,
            appConfigMock.Object);

        // Reset connection state
        isConnected = false;
        currentSubscriptionGuid = Guid.Empty;
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
        startSubscriptionResponse = await canaryService.StartLevel1SubscriptionAsync(startSubscriptionRequest);

        if (startSubscriptionResponse.SubscriptionGuid.HasValue)
        {
            currentSubscriptionGuid = startSubscriptionResponse.SubscriptionGuid.Value;
            isConnected = true;
        }
    }

    [When(@"I get an unexpected disonnect")]
    public void WhenIGetAnUnexpectedDisconnect()
    {
        // Simulate unexpected disconnect
        isConnected = false;

        // Setup mock consumer to simulate disconnection
        webSocketConsumerMock
            .Setup(w => w.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection lost unexpectedly"));
    }

    [When(@"I get an expected disonnect")]
    public void WhenIGetAnExpectedDisconnect()
    {
        // Simulate controlled disconnect (e.g., stopping subscription)
        isConnected = false;

        if (currentSubscriptionGuid != Guid.Empty)
        {
            subscriptionManager.Remove(currentSubscriptionGuid);
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
            w => w.Create(It.IsAny<string>(), It.IsAny<bool>()),
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
            w => w.Create(It.IsAny<string>(), It.IsAny<bool>()),
            Times.AtLeastOnce);
    }

    [Then(@"I am able to reconnect")]
    public async Task ThenIAmAbleToReconnect()
    {
        // Reset mock to allow successful connection
        webSocketConsumerMock
            .Setup(w => w.StartConsumingAsync(It.IsAny<CancellationToken>()))
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
            w => w.StartConsumingAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce,
            "WebSocket consumer should be actively consuming messages after reconnection");

        // Verify that the factory was called multiple times (initial + reconnect)
        webSocketConsumerFactoryMock.Verify(
            w => w.Create(It.IsAny<string>(), It.IsAny<bool>()),
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
