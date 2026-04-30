using System.ComponentModel;
using com.morningstar.rtd.datatypes;
using Newtonsoft.Json;

namespace Morningstar.Snapshot.Domain.Models;

[DisplayName("MessagePacket")]
public class MessagePacket<T> where T : IMessage
{
    public EventType EventType { get; set; }
    public string? PerformanceId { get; set; }
    public long? PublishTime { get; set; }
    public long? AcknowledgedTime { get; set; }
    public long? SequenceNumber { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public T Message { get; set; }
}

[DisplayName("AggregateSummary")]
public class AggregateSummaryMessage : IMessage
{
    public int? TradeCount { get; set; }
    public long? CumulativeVolume { get; set; }
    public double? VWAP { get; set; }
    public int? Openinterest { get; set; }
    public long? OpeninterestDateTime { get; set; }
    public double? Turnover { get; set; }
}

[DisplayName("Auction")]
public class AuctionMessage : IMessage
{
    public double? IndicativeAuctionPrice { get; set; }
    public int? IndicativeAuctionSize { get; set; }
    public long? ImbalanceSize { get; set; }
    public string? ImbalanceSide { get; set; }
    public string? AuctionType { get; set; }
    public int? AuctionDate { get; set; }
}

[DisplayName("Close")]
public class CloseMessage : IMessage
{
    public double? ClosePrice { get; set; }
    public long? ClosePriceDateTime { get; set; }
    public double? UnadjustedPreviousClosePrice { get; set; }
    public double? VendorProvidedAdjustedPreviousClosePrice { get; set; }
}

[DisplayName("IndexTick")]
public class IndexTickMessage : IMessage
{
    public long? PriceCalculationDateTime { get; set; }
}

[DisplayName("LastPrice")]
public class LastPriceMessage : IMessage
{
    public double? Price { get; set; }
    public int? Size { get; set; }
    public long? PricePublishDateTime { get; set; }
}

[DisplayName("MarketByPrice")]
public class MarketByPriceMessage : IMessage
{
    public int? RankLevel { get; set; }
    public string? Side { get; set; }
    public double? Price { get; set; }
    public int? Volume { get; set; }
    public int? NumberOfOrders { get; set; }
    public long? Timestamp { get; set; }
}

[DisplayName("MidPrice")]
public class MidPriceMessage : IMessage
{
    public double? Price { get; set; }
    public double? PriceHigh { get; set; }
    public double? PriceLow { get; set; }
    public double? PriceClose { get; set; }
    public long? DateTime { get; set; }
}

[DisplayName("NAVPrice")]
public class NAVPriceMessage : IMessage
{
    public double? ExchangeNAV { get; set; }
    public long? NAVDateTime { get; set; }
}

[DisplayName("OHLPrice")]
public class OHLPriceMessage : IMessage
{
    public double? OpenPrice { get; set; }
    public long? OpenPriceDateTime { get; set; }
    public double? HighPrice { get; set; }
    public long? HighPriceDateTime { get; set; }
    public double? LowPrice { get; set; }
    public long? LowPriceDateTime { get; set; }
}

[DisplayName("InstrumentPerformanceStatistics")]
public class InstrumentPerformanceStatisticsMessage : IMessage
{
    public double? NetChange { get; set; }
    public double? PercentChange { get; set; }
    public double? PreMarketNetChange { get; set; }
    public double? PreMarketPercentChange { get; set; }
    public double? PostMarketNetChange { get; set; }
    public double? PostMarketPercentChange { get; set; }
}

[DisplayName("SettlementPrice")]
public class SettlementPriceMessage : IMessage
{
    public double? FinalSettlementPrice { get; set; }
    public double? TodaysSettlementPrice { get; set; }
    public int? SettlementPriceType { get; set; }
    public long? SettlementPriceDateTime { get; set; }
    public int? SettlementPriceCalculationDate { get; set; }
    public double? PreviousSettlementPrice { get; set; }
    public string? SettlementPriceMethod { get; set; }
}

[DisplayName("SpreadStatistics")]
public class SpreadStatisticsMessage : IMessage
{
    public double? BidAskSpread { get; set; }
    public double? DailyAverageSpreadPercentage { get; set; }
    public double? DailyAverageSpread { get; set; }
}

[DisplayName("Status")]
public class StatusMessage : IMessage
{
    public string? InstrumentPhase { get; set; }
    public long? InstrumentPhaseDateTime { get; set; }
    public int? TradingStatus { get; set; }
    public long? TradingStatusDateTime { get; set; }
}

[DisplayName("TopOfBook")]
public class TopOfBookMessage : IMessage
{
    public double? AskPrice { get; set; }
    public int? AskSize { get; set; }
    public long? AskPriceDateTime { get; set; }
    public int? AskConditionFlag { get; set; }
    public int? AskExchange { get; set; }
    public double? BidPrice { get; set; }
    public int? BidSize { get; set; }
    public long? BidPriceDateTime { get; set; }
    public int? BidConditionFlag { get; set; }
    public int? BidExchange { get; set; }
    public double? AskPriceClose { get; set; }
    public double? BidPriceClose { get; set; }
}

public class TradePostMarket : IMessage
{
    public double? Price { get; set; }
    public int? Size { get; set; }
    public int? CumulativeVolume { get; set; }
    public int? Count { get; set; }
    public long? DateTime { get; set; }
}

public class TradePreMarket : IMessage
{
    public double? Price { get; set; }
    public int? Size { get; set; }
    public int? CumulativeVolume { get; set; }
    public int? Count { get; set; }
    public long? DateTime { get; set; }
}

public class TradeRegulatory
{
    public string? MMTFlagTransactionCategory { get; set; }
    public string? MMTFlagMarketMechanism { get; set; }
    public string? MMTFlagTradingMode { get; set; }
    public string? MMTFlagDuplicativeTradeIndicator { get; set; }
    public string? MMTFlagPublicationMode { get; set; }
}

[DisplayName("TradeCancellation")]
public class TradeCancellationMessage : IMessage
{
    public string? OriginalTradeID { get; set; }
    public long? DateTime { get; set; }
}

[DisplayName("TradeCorrection")]
public class TradeCorrectionMessage : IMessage
{
    public double? CorrectedTradePrice { get; set; }
    public int? CorrectedTradeSize { get; set; }
    public string? OriginalTradeID { get; set; }
    public long? DateTime { get; set; }
    public string? CorrectedTradeConditions { get; set; }
}

public class TradePrice
{
    public double? Price { get; set; }
    public int? Size { get; set; }
    public long? ExecutionDateTime { get; set; }
    public string? Conditions { get; set; }
    public string? IdentificationCode { get; set; }
    public long? PublishDateTime { get; set; }
    public string? ExecutionVenue { get; set; }
    public string? ExecutionCurrency { get; set; }
}

[DisplayName("Trade")]
public class TradeMessage : IMessage
{
    public TradePostMarket TradePostMarket { get; set; }
    public TradePreMarket TradePreMarket { get; set; }
    public TradeRegulatory TradeRegulatory { get; set; }
    public TradePrice TradePrice { get; set; }
}

[DisplayName("Financials")]
public class FinancialsMessage : IMessage
{
    public double? IntradayPriceToBookRatio { get; set; }
}

[DisplayName("Admin")]
public class AdminMessage : IMessage
{
    public string? Notice { get; set; }
    public string? Description { get; set; }
    public NoticeType? NoticeType { get; set; }
    public int? NoticeMinutes { get; set; }
    public long? NoticeDateTime { get; set; }
    public bool? Arbitrate { get; set; }
}
