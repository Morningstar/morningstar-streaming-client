using FluentAssertions;
using Morningstar.Snapshot.Domain.Models;
using Newtonsoft.Json;

namespace Morningstar.Snapshot.Client.Tests.HelperTests;

public class IMessageConverterTests
{
    private readonly IMessageConverter sut = new();

    [Fact]
    public void CanConvert_ReturnsTrue_ForIMessageType()
    {
        sut.CanConvert(typeof(IMessage)).Should().BeTrue();
    }

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(string))]
    [InlineData(typeof(LastPriceMessage))]
    [InlineData(typeof(TopOfBookMessage))]
    public void CanConvert_ReturnsFalse_ForNonIMessageType(Type type)
    {
        sut.CanConvert(type).Should().BeFalse();
    }

    [Fact]
    public void CanWrite_ReturnsFalse()
    {
        sut.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void ReadJson_ReturnsNull_WhenJsonIsEmptyObject()
    {
        var result = Deserialize("{}");

        result.Should().BeNull();
    }

    [Fact]
    public void ReadJson_ReturnsNull_WhenNoPropertiesMatchAnyKnownType()
    {
        var result = Deserialize("""{"unknownField": 1, "anotherUnknown": "x"}""");

        result.Should().BeNull();
    }

    public static IEnumerable<object[]> MessageTypeResolutionCases() =>
    [
        ["""{"price": 1.0, "size": 10, "pricePublishDateTime": 1000}""",           typeof(LastPriceMessage)],
        ["""{"askPrice": 10.0, "bidPrice": 9.0, "askSize": 5, "bidSize": 5}""",    typeof(TopOfBookMessage)],
        ["""{"rankLevel": 1, "side": "B", "volume": 100, "numberOfOrders": 3}""",  typeof(MarketByPriceMessage)],
        ["""{"tradePrice": {}, "tradePostMarket": {}, "tradePreMarket": {}}""",     typeof(TradeMessage)],
        ["""{"openPrice": 1.0, "highPrice": 2.0, "lowPrice": 0.5}""",              typeof(OHLPriceMessage)],
        ["""{"priceLow": 1.0, "priceHigh": 2.0, "priceClose": 1.5}""",             typeof(MidPriceMessage)],
        ["""{"closePrice": 1.0, "closePriceDateTime": 1000}""",                     typeof(CloseMessage)],
        ["""{"tradeCount": 5, "cumulativeVolume": 1000, "vwap": 1.5}""",           typeof(AggregateSummaryMessage)],
        ["""{"indicativeAuctionPrice": 1.0, "imbalanceSize": 100}""",              typeof(AuctionMessage)],
        ["""{"instrumentPhase": "Open", "tradingStatus": 1}""",                    typeof(StatusMessage)],
        ["""{"finalSettlementPrice": 1.0, "settlementPriceType": 1}""",            typeof(SettlementPriceMessage)],
        ["""{"bidAskSpread": 0.1, "dailyAverageSpreadPercentage": 0.05}""",        typeof(SpreadStatisticsMessage)],
        ["""{"exchangeNAV": 1.0, "navDateTime": 1000}""",                          typeof(NAVPriceMessage)],
        ["""{"netChange": 0.5, "percentChange": 1.2}""",                            typeof(InstrumentPerformanceStatisticsMessage)],
        ["""{"intradayPriceToBookRatio": 1.5}""",                                  typeof(FinancialsMessage)],
        ["""{"notice": "msg", "description": "desc"}""",                            typeof(AdminMessage)],
        ["""{"originalTradeID": "T001"}""",                                         typeof(TradeCancellationMessage)],
        ["""{"correctedTradePrice": 1.0, "correctedTradeSize": 10}""",             typeof(TradeCorrectionMessage)],
        ["""{"priceCalculationDateTime": 1000}""",                                 typeof(IndexTickMessage)],
    ];

    [Theory]
    [MemberData(nameof(MessageTypeResolutionCases))]
    public void ReadJson_ReturnsCorrectConcreteType_ForEachMessageType(string json, Type expectedType)
    {
        var result = Deserialize(json);

        result.Should().NotBeNull();
        result.Should().BeOfType(expectedType);
    }

    [Fact]
    public void ReadJson_DeserializesPropertyValues_ForLastPriceMessage()
    {
        var result = Deserialize("""{"price": 123.45, "size": 100, "pricePublishDateTime": 999}""");

        var message = result.Should().BeOfType<LastPriceMessage>().Subject;
        message.Price.Should().Be(123.45);
        message.Size.Should().Be(100);
        message.PricePublishDateTime.Should().Be(999);
    }

    [Fact]
    public void ReadJson_ReturnsHighestScoringType_WhenMultipleTypesShareProperties()
    {
        // "price" scores LastPrice, but the other 3 properties all score TopOfBook
        var result = Deserialize("""{"price": 1.0, "askPrice": 10.0, "bidPrice": 9.0, "askSize": 5}""");

        result.Should().BeOfType<TopOfBookMessage>();
    }

    [Fact]
    public void ReadJson_IsCaseInsensitive_ForPropertyNames()
    {
        var result = Deserialize("""{"PRICE": 1.0, "SIZE": 10, "PRICEPUBLISHDATETIME": 1000}""");

        result.Should().BeOfType<LastPriceMessage>();
    }

    private static IMessage? Deserialize(string json) =>
        JsonConvert.DeserializeObject<IMessage>(json, new IMessageConverter());
}
