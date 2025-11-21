namespace Morningstar.Streaming.Domain.Constants;

public static class EventTypes
{
    public const string AggregateSummary = "AggregateSummary";
    public const string Auction = "Auction";
    public const string Close = "Close";
    public const string IndexTick = "IndexTick";
    public const string LastPrice = "LastPrice";
    public const string MarketByPrice = "MarketByPrice";
    public const string MidPrice = "MidPrice";
    public const string NAVPrice = "NAVPrice";
    public const string OHLPrice = "OHLPrice";
    public const string InstrumentPerformanceStatistics = "InstrumentPerformanceStatistics";
    public const string SettlementPrice = "SettlementPrice";
    public const string SpreadStatistics = "SpreadStatistics";
    public const string Status = "Status";
    public const string TopOfBook = "TopOfBook";
    public const string TradePostMarket = "TradePostMarket";
    public const string TradePreMarket = "TradePreMarket";
    public const string TradeCancellation = "TradeCancellation";
    public const string TradeCorrection = "TradeCorrection";
    public const string Trade = "Trade";
    public const string HeartBeat = "HeartBeat";
    public const string Admin = "Admin";
    public const string HeartBeatAcknowledged = "HeartBeatAcknowledged";
}
