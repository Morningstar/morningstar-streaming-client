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
    //[DataPointId(DataPointIds.TradeCount)]
    public int? TradeCount { get; set; }

    //[DataPointId(DataPointIds.MDMarketPriceCumulativeVolume)]
    public long? CumulativeVolume { get; set; }

    //[DataPointId(DataPointIds.VWAP)]
    public double? VWAP { get; set; }

    //[DataPointId(DataPointIds.OpenInterest)]
    public int? Openinterest { get; set; }

    //[DataPointId(DataPointIds.OpenInterestDateTime)]
    public long? OpeninterestDateTime { get; set; }

    //[DataPointId(DataPointIds.Turnover)]
    public double? Turnover { get; set; }
}

[DisplayName("Auction")]
public class AuctionMessage : IMessage
{
    //[DataPointId(DataPointIds.IndicativeAuctionPrice)]
    public double? IndicativeAuctionPrice { get; set; }

    //[DataPointId(DataPointIds.IndicativeAuctionSize)]
    public int? IndicativeAuctionSize { get; set; }

    //[DataPointId(DataPointIds.ImbalanceSize)]
    public long? ImbalanceSize { get; set; }

    //[DataPointId(DataPointIds.ImbalanceSide)]
    public string? ImbalanceSide { get; set; }

    //[DataPointId(DataPointIds.AuctionType)]
    public string? AuctionType { get; set; }

    //[DataPointId(DataPointIds.AuctionDate)]
    public int? AuctionDate { get; set; }
}

[DisplayName("Close")]
public class CloseMessage : IMessage
{
    //[DataPointId(DataPointIds.MDClosePrice)]
    public double? ClosePrice { get; set; }
    //[DataPointId(DataPointIds.MDMarketPricePublishDate)]
    public long? ClosePriceDateTime { get; set; }

    //[DataPointId(DataPointIds.UnadjustedPreviousClosePrice)]
    public double? UnadjustedPreviousClosePrice { get; set; }

    //[DataPointId(DataPointIds.VendorProvidedAdjustedPreviousClosePrice)]
    public double? VendorProvidedAdjustedPreviousClosePrice { get; set; }
}

[DisplayName("IndexTick")]
public class IndexTickMessage : IMessage
{

    //[DataPointId(DataPointIds.IndexPriceCalculationDateTime)]
    public long? PriceCalculationDateTime { get; set; }
}

[DisplayName("LastPrice")]
public class LastPriceMessage : IMessage
{
    //[DataPointId(DataPointIds.LastPrice)]
    public double? Price { get; set; }

    //[DataPointId(DataPointIds.LastSize)]
    public int? Size { get; set; }

    //[DataPointId(DataPointIds.LastPricePublishDateTime)]
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
    //[DataPointId(DataPointIds.MidPrice)]
    public double? Price { get; set; }

    //[DataPointId(DataPointIds.MidPriceHigh)]
    public double? PriceHigh { get; set; }

    //[DataPointId(DataPointIds.MidPriceLow)]
    public double? PriceLow { get; set; }

    //[DataPointId(DataPointIds.MidPriceClose)]
    public double? PriceClose { get; set; }

    //[DataPointId(DataPointIds.MidDateTime)]
    public long? DateTime { get; set; }
}

[DisplayName("NAVPrice")]
public class NAVPriceMessage : IMessage
{
    //[DataPointId(DataPointIds.MDExchangeNAV)]
    public double? ExchangeNAV { get; set; }

    //[DataPointId(DataPointIds.NAVDateTime)]
    public long? NAVDateTime { get; set; }
}

[DisplayName("OHLPrice")]
public class OHLPriceMessage : IMessage
{
    //[DataPointId(DataPointIds.MDOpenPrice)]
    public double? OpenPrice { get; set; }

    //[DataPointId(DataPointIds.OpenPriceDateTime)]
    public long? OpenPriceDateTime { get; set; }

    //[DataPointId(DataPointIds.MDHighPrice)]
    public double? HighPrice { get; set; }

    //[DataPointId(DataPointIds.HighPriceDateTime)]
    public long? HighPriceDateTime { get; set; }

    //[DataPointId(DataPointIds.MDLowPrice)]
    public double? LowPrice { get; set; }

    //[DataPointId(DataPointIds.LowPriceDateTime)]
    public long? LowPriceDateTime { get; set; }
}

[DisplayName("InstrumentPerformanceStatistics")]
public class InstrumentPerformanceStatisticsMessage : IMessage
{
    //[DataPointId(DataPointIds.NetChange)]
    public double? NetChange { get; set; }

    //[DataPointId(DataPointIds.PercentChange)]
    public double? PercentChange { get; set; }

    //[DataPointId(DataPointIds.PreMarketNetChange)]
    public double? PreMarketNetChange { get; set; }

    //[DataPointId(DataPointIds.PreMarketPercentChange)]
    public double? PreMarketPercentChange { get; set; }

    //[DataPointId(DataPointIds.PostMarketNetChange)]
    public double? PostMarketNetChange { get; set; }

    //[DataPointId(DataPointIds.PostMarketPercentChange)]
    public double? PostMarketPercentChange { get; set; }
}

[DisplayName("SettlementPrice")]
public class SettlementPriceMessage : IMessage
{
    //[DataPointId(DataPointIds.FinalSettlementPrice)]
    public double? FinalSettlementPrice { get; set; }

    //[DataPointId(DataPointIds.TodaysSettlementPrice)]
    public double? TodaysSettlementPrice { get; set; }

    //[DataPointId(DataPointIds.SettlementPriceType)]
    public int? SettlementPriceType { get; set; }

    //[DataPointId(DataPointIds.SettlementPriceDateTime)]
    public long? SettlementPriceDateTime { get; set; }

    //[DataPointId(DataPointIds.SettlementPriceCalculationDate)]
    public int? SettlementPriceCalculationDate { get; set; }

    //[DataPointId(DataPointIds.PreviousSettlementPrice)]
    public double? PreviousSettlementPrice { get; set; }

    //[DataPointId(DataPointIds.SettlementPriceMethod)]
    public string? SettlementPriceMethod { get; set; }
}

[DisplayName("SpreadStatistics")]
public class SpreadStatisticsMessage : IMessage
{
    //[DataPointId(DataPointIds.BidAskSpread)]
    public double? BidAskSpread { get; set; }

    //[DataPointId(DataPointIds.DailyAverageSpreadPercentage)]
    public double? DailyAverageSpreadPercentage { get; set; }

    //[DataPointId(DataPointIds.DailyAverageSpread)]
    public double? DailyAverageSpread { get; set; }
}

[DisplayName("Status")]
public class StatusMessage : IMessage
{
    //[DataPointId(DataPointIds.InstrumentPhase)]
    public string? InstrumentPhase { get; set; }

    //[DataPointId(DataPointIds.InstrumentPhaseDateTime)]
    public long? InstrumentPhaseDateTime { get; set; }

    //[DataPointId(DataPointIds.TradingStatus)]
    public int? TradingStatus { get; set; }

    //[DataPointId(DataPointIds.TradingStatusDateTime)]
    public long? TradingStatusDateTime { get; set; }
}

[DisplayName("TopOfBook")]
public class TopOfBookMessage : IMessage
{
    //[DataPointId(DataPointIds.AskPrice)]
    public double? AskPrice { get; set; }

    //[DataPointId(DataPointIds.AskSize)]
    public int? AskSize { get; set; }

    //[DataPointId(DataPointIds.AskPriceDateTime)]
    public long? AskPriceDateTime { get; set; }

    //[DataPointId(DataPointIds.AskConditionFlag)]
    public int? AskConditionFlag { get; set; }

    //[DataPointId(DataPointIds.AskExchange)]
    public int? AskExchange { get; set; }

    //[DataPointId(DataPointIds.BidPrice)]
    public double? BidPrice { get; set; }

    //[DataPointId(DataPointIds.BidSize)]
    public int? BidSize { get; set; }

    //[DataPointId(DataPointIds.BidPriceDateTime)]
    public long? BidPriceDateTime { get; set; }

    //[DataPointId(DataPointIds.BidConditionFlag)]
    public int? BidConditionFlag { get; set; }

    //[DataPointId(DataPointIds.BidExchange)]
    public int? BidExchange { get; set; }

    //[DataPointId(DataPointIds.MDAskPriceClose)]
    public double? AskPriceClose { get; set; }

    //[DataPointId(DataPointIds.MDBidPriceClose)]
    public double? BidPriceClose { get; set; }
}

public class TradePostMarket : IMessage
{
    //[DataPointId(DataPointIds.PostMarketTradePrice)]
    public double? Price { get; set; }

    //[DataPointId(DataPointIds.PostMarketTradeSize)]
    public int? Size { get; set; }

    //[DataPointId(DataPointIds.PostMarketCumulativeVolume)]
    public int? CumulativeVolume { get; set; }

    //[DataPointId(DataPointIds.PostMarketTradeCount)]
    public int? Count { get; set; }

    //[DataPointId(DataPointIds.PostMarketTradeDateTime)]
    public long? DateTime { get; set; }
}

public class TradePreMarket : IMessage
{
    //[DataPointId(DataPointIds.PreMarketTradePrice)]
    public double? Price { get; set; }

    //[DataPointId(DataPointIds.PreMarketTradeSize)]
    public int? Size { get; set; }

    //[DataPointId(DataPointIds.PreMarketCumulativeVolume)]
    public int? CumulativeVolume { get; set; }

    //[DataPointId(DataPointIds.PreMarketTradeCount)]
    public int? Count { get; set; }

    //[DataPointId(DataPointIds.PreMarketTradePriceDateTime)]
    public long? DateTime { get; set; }
}

public class TradeRegulatory
{
    //[DataPointId(DataPointIds.MMTFlagTransactionCategory)]
    public string? MMTFlagTransactionCategory { get; set; }

    //[DataPointId(DataPointIds.MMTFlagMarketMechanism)]
    public string? MMTFlagMarketMechanism { get; set; }

    //[DataPointId(DataPointIds.MMTFlagTradingMode)]
    public string? MMTFlagTradingMode { get; set; }

    //[DataPointId(DataPointIds.MMTFlagDuplicativeTradeIndicator)]
    public string? MMTFlagDuplicativeTradeIndicator { get; set; }

    //[DataPointId(DataPointIds.MMTFlagPublicationMode)]
    public string? MMTFlagPublicationMode { get; set; }
}

[DisplayName("TradeCancellation")]
public class TradeCancellationMessage : IMessage
{
    //[DataPointId(DataPointIds.TradeCancelOriginalTradeID)]
    public string? OriginalTradeID { get; set; }

    //[DataPointId(DataPointIds.TradeCancellationDateTime)]
    public long? DateTime { get; set; }
}

[DisplayName("TradeCorrection")]
public class TradeCorrectionMessage : IMessage
{
    //[DataPointId(DataPointIds.CorrectedTradePrice)]
    public double? CorrectedTradePrice { get; set; }

    //[DataPointId(DataPointIds.CorrectedTradeSize)]
    public int? CorrectedTradeSize { get; set; }

    //[DataPointId(DataPointIds.TradeCorrectionOriginalTradeID)]
    public string? OriginalTradeID { get; set; }

    //[DataPointId(DataPointIds.TradeCorrectionDateTime)]
    public long? DateTime { get; set; }

    //[DataPointId(DataPointIds.CorrectedTradeConditions)]
    public string? CorrectedTradeConditions { get; set; }
}

public class TradePrice
{
    //[DataPointId(DataPointIds.TradePrice)]
    public double? Price { get; set; }

    //[DataPointId(DataPointIds.TradeSize)]
    public int? Size { get; set; }

    //[DataPointId(DataPointIds.TradeExecutionDateTime)]
    public long? ExecutionDateTime { get; set; }

    //[DataPointId(DataPointIds.TradeConditions)]
    public string? Conditions { get; set; }

    //[DataPointId(DataPointIds.TradeIdentificationCode)]
    public string? IdentificationCode { get; set; }

    //[DataPointId(DataPointIds.TradePublishDateTime)]
    public long? PublishDateTime { get; set; }

    //[DataPointId(DataPointIds.TradeExecutionVenue)]
    public string? ExecutionVenue { get; set; }

    //[DataPointId(DataPointIds.TradeExecutionCurrency)]
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
    //[DataPointId(DataPointIds.PBRatio)]
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

