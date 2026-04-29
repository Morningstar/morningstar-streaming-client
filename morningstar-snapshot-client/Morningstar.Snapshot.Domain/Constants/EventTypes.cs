namespace Morningstar.Snapshot.Domain.Constants;

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


public enum EventType
{
    AggregateSummary = 0,
    Auction = 1,
    Close = 2,
    IndexTick = 3,
    LastPrice = 4,
    MarketByPrice = 5,
    MidPrice = 6,
    NAVPrice = 7,
    OHLPrice = 8,
    InstrumentPerformanceStatistics = 9,
    SettlementPrice = 10,
    SpreadStatistics = 11,
    Status = 12,
    TopOfBook = 13,
    TradePostMarket = 14,
    TradePreMarket = 15,
    TradeCancellation = 16,
    TradeCorrection = 17,
    Trade = 18,
    HeartBeat = 19,
    Financials = 20,
    Admin = 21,
    HeartBeatAcknowledged = 22,
}
