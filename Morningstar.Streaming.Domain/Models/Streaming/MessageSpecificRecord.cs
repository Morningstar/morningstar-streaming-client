using System.CodeDom.Compiler;
using Avro;
using Avro.Specific;

namespace com.morningstar.rtd.datatypes
{
    public enum EventType
    {
        Unknown,
        AggregateSummary,
        Auction,
        Close,
        Financials,
        IndexTick,
        LastPrice,
        MarketByPrice,
        MidPrice,
        NAVPrice,
        OHLPrice,
        InstrumentPerformanceStatistics,
        SettlementPrice,
        SpreadStatistics,
        Status,
        TopOfBook,
        TradePostMarket,
        TradePreMarket,
        TradeCancellation,
        TradeCorrection,
        Trade,
        Terminated,
        Admin,
        HeartBeat
    }

    public enum NoticeType
    {
        Unknown,
        Disconnect
    }
}

namespace com.morningstar.rtd.data.polarisetl
{
    using com.morningstar.rtd.datatypes;

    // Top-level Message implements ISpecificRecord (keeps main big schema)
    [GeneratedCode("avrogen", "1.12.0+8c27801dc8d42ccc00997f25c0b8f45f8d4a233e")]
    public class Message : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"Message\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"InstrumentId\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"PerformanceId\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"SequenceNumber\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"ContextId\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"EventTypes\",\"default\":null,\"type\":[\"null\",{\"type\":\"array\",\"items\":{\"type\":\"enum\",\"name\":\"EventType\",\"namespace\":\"com.morningstar.rtd.datatypes\",\"symbols\":[\"Unknown\",\"AggregateSummary\",\"Auction\",\"Close\",\"Financials\",\"IndexTick\",\"LastPrice\",\"MarketByPrice\",\"MidPrice\",\"NAVPrice\",\"OHLPrice\",\"InstrumentPerformanceStatistics\",\"SettlementPrice\",\"SpreadStatistics\",\"Status\",\"TopOfBook\",\"TradePostMarket\",\"TradePreMarket\",\"TradeCancellation\",\"TradeCorrection\",\"Trade\",\"Terminated\",\"Admin\",\"HeartBeat\"],\"default\":\"Unknown\"}}]},{\"name\":\"AggregateSummary\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"AggregateSummary\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"TradeCount\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"CumulativeVolume\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"VWAP\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"ImpliedVolatility\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"OpenInterest\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"OpenInterestDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"Turnover\",\"default\":null,\"type\":[\"null\",\"double\"]}]}]},{\"name\":\"Auction\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"Auction\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"IndicativeAuctionPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"IndicativeAuctionSize\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"ImbalanceSize\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"ImbalanceSide\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"AuctionType\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"AuctionDate\",\"default\":null,\"type\":[\"null\",\"int\"]}]}]},{\"name\":\"Close\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"Close\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"ClosePrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"ClosePriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"UnadjustedPreviousClosePrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"VendorProvidedAdjustedPreviousClosePrice\",\"default\":null,\"type\":[\"null\",\"double\"]}]}]},{\"name\":\"Financials\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"Financials\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"PriceBookRatio\",\"default\":null,\"type\":[\"null\",\"double\"]}]}]},{\"name\":\"IndexTick\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"IndexTick\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PriceCalculationDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"PricePublicationTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"LastPrice\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"LastPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"PricePublishDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"MarketByPrice\",\"default\":null,\"type\":[\"null\",{\"type\":\"array\",\"items\":{\"type\":\"record\",\"name\":\"MarketByPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"RankLevel\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"Side\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Volume\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"NumberOfOrders\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"Timestamp\",\"default\":null,\"type\":[\"null\",\"long\"]}]}}]},{\"name\":\"MidPrice\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"MidPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PriceHigh\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PriceLow\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PriceClose\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"NAVPrice\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"NAVPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"ExchangeNAV\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"NAVDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"OHLPrice\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"OHLPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"OpenPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"OpenPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"HighPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"HighPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"LowPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"LowPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"InstrumentPerformanceStatistics\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"InstrumentPerformanceStatistics\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"MidNetChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"MidPercentChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"NetChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PercentChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PreMarketNetChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PreMarketPercentChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PostMarketNetChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PostMarketPercentChange\",\"default\":null,\"type\":[\"null\",\"double\"]}]}]},{\"name\":\"SettlementPrice\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"SettlementPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"FinalSettlementPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"TodaysSettlementPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"SettlementPriceType\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"SettlementPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"PreviousSettlementPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"SettlementPriceMethod\",\"default\":null,\"type\":[\"null\",\"string\"]}]}]},{\"name\":\"SpreadStatistics\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"SpreadStatistics\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"BidAskSpread\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"DailyAverageSpreadPercentage\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"DailyAverageSpread\",\"default\":null,\"type\":[\"null\",\"double\"]}]}]},{\"name\":\"Status\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"Status\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"InstrumentPhase\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"InstrumentPhaseDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"TradingStatus\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"ActiveInactiveFlag\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"TradingStatusDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"TopOfBook\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TopOfBook\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"AskPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"AskSize\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"AskPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"AskConditionFlag\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"AskExchange\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"BidPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"BidSize\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"BidPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"BidConditionFlag\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"BidExchange\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"BidPriceClose\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"AskPriceClose\",\"default\":null,\"type\":[\"null\",\"double\"]}]}]},{\"name\":\"TradeCancellation\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TradeCancellation\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"OriginalTradeID\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"TradeCorrection\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TradeCorrection\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"CorrectedTradePrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"CorrectedTradeSize\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"OriginalTradeID\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"CorrectedTradeConditions\",\"default\":null,\"type\":[\"null\",\"string\"]}]}]},{\"name\":\"Trade\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"Trade\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"TradePostMarket\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TradePostMarket\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"CumulativeVolume\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"Count\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"TradePreMarket\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TradePreMarket\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"CumulativeVolume\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"Count\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"TradeRegulatory\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TradeRegulatory\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"MMTFlagTransactionCategory\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagMarketMechanism\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagTradingMode\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagDuplicativeTradeIndicator\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagPublicationMode\",\"default\":null,\"type\":[\"null\",\"string\"]}]}]},{\"name\":\"TradePrice\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TradePrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"ExecutionDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"Conditions\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"IdentificationCode\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"PublishDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"ExecutionVenue\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"ExecutionCurrency\",\"default\":null,\"type\":[\"null\",\"string\"]}]}]}]}]},{\"name\":\"Admin\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"Admin\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Notice\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"NoticeType\",\"default\":null,\"type\":[\"null\",{\"type\":\"enum\",\"name\":\"NoticeType\",\"namespace\":\"com.morningstar.rtd.datatypes\",\"symbols\":[\"Unknown\",\"Disconnect\"],\"default\":\"Unknown\"}]},{\"name\":\"Description\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"Arbitrate\",\"default\":null,\"type\":[\"null\",\"boolean\"]},{\"name\":\"NoticeMinutes\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"NoticeDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]}]}"
        );

        private long? _InstrumentId;
        private string? _PerformanceId;
        private long? _SequenceNumber;
        private string? _ContextId;
        private IList<EventType>? _EventTypes;
        private AggregateSummary? _AggregateSummary;
        private Auction? _Auction;
        private Close? _Close;
        private Financials? _Financials;
        private IndexTick? _IndexTick;
        private LastPrice? _LastPrice;
        private IList<MarketByPrice>? _MarketByPrice;
        private MidPrice? _MidPrice;
        private NAVPrice? _NAVPrice;
        private OHLPrice? _OHLPrice;
        private InstrumentPerformanceStatistics? _InstrumentPerformanceStatistics;
        private SettlementPrice? _SettlementPrice;
        private SpreadStatistics? _SpreadStatistics;
        private Status? _Status;
        private TopOfBook? _TopOfBook;
        private TradeCancellation? _TradeCancellation;
        private TradeCorrection? _TradeCorrection;
        private Trade? _Trade;
        private Admin? _Admin;

        public virtual Schema Schema => _SCHEMA;

        public long? InstrumentId
        {
            get => _InstrumentId;
            set => _InstrumentId = value;
        }

        public string? PerformanceId
        {
            get => _PerformanceId;
            set => _PerformanceId = value;
        }

        public long? SequenceNumber
        {
            get => _SequenceNumber;
            set => _SequenceNumber = value;
        }

        public string? ContextId
        {
            get => _ContextId;
            set => _ContextId = value;
        }

        public IList<EventType>? EventTypes
        {
            get => _EventTypes;
            set => _EventTypes = value;
        }

        public AggregateSummary? AggregateSummary
        {
            get => _AggregateSummary;
            set => _AggregateSummary = value;
        }

        public Auction? Auction
        {
            get => _Auction;
            set => _Auction = value;
        }

        public Close? Close
        {
            get => _Close;
            set => _Close = value;
        }

        public Financials? Financials
        {
            get => _Financials;
            set => _Financials = value;
        }

        public IndexTick? IndexTick
        {
            get => _IndexTick;
            set => _IndexTick = value;
        }

        public LastPrice? LastPrice
        {
            get => _LastPrice;
            set => _LastPrice = value;
        }

        public IList<MarketByPrice>? MarketByPrice
        {
            get => _MarketByPrice;
            set => _MarketByPrice = value;
        }

        public MidPrice? MidPrice
        {
            get => _MidPrice;
            set => _MidPrice = value;
        }

        public NAVPrice? NAVPrice
        {
            get => _NAVPrice;
            set => _NAVPrice = value;
        }

        public OHLPrice? OHLPrice
        {
            get => _OHLPrice;
            set => _OHLPrice = value;
        }

        public InstrumentPerformanceStatistics? InstrumentPerformanceStatistics
        {
            get => _InstrumentPerformanceStatistics;
            set => _InstrumentPerformanceStatistics = value;
        }

        public SettlementPrice? SettlementPrice
        {
            get => _SettlementPrice;
            set => _SettlementPrice = value;
        }

        public SpreadStatistics? SpreadStatistics
        {
            get => _SpreadStatistics;
            set => _SpreadStatistics = value;
        }

        public Status? Status
        {
            get => _Status;
            set => _Status = value;
        }

        public TopOfBook? TopOfBook
        {
            get => _TopOfBook;
            set => _TopOfBook = value;
        }

        public TradeCancellation? TradeCancellation
        {
            get => _TradeCancellation;
            set => _TradeCancellation = value;
        }

        public TradeCorrection? TradeCorrection
        {
            get => _TradeCorrection;
            set => _TradeCorrection = value;
        }

        public Trade? Trade
        {
            get => _Trade;
            set => _Trade = value;
        }

        public Admin? Admin
        {
            get => _Admin;
            set => _Admin = value;
        }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => InstrumentId,
                1 => PerformanceId,
                2 => SequenceNumber,
                3 => ContextId,
                4 => EventTypes,
                5 => AggregateSummary,
                6 => Auction,
                7 => Close,
                8 => Financials,
                9 => IndexTick,
                10 => LastPrice,
                11 => MarketByPrice,
                12 => MidPrice,
                13 => NAVPrice,
                14 => OHLPrice,
                15 => InstrumentPerformanceStatistics,
                16 => SettlementPrice,
                17 => SpreadStatistics,
                18 => Status,
                19 => TopOfBook,
                20 => TradeCancellation,
                21 => TradeCorrection,
                22 => Trade,
                23 => Admin,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()"),
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0:
                    InstrumentId = (long?)fieldValue;
                    break;
                case 1:
                    PerformanceId = (string)fieldValue;
                    break;
                case 2:
                    SequenceNumber = (long?)fieldValue;
                    break;
                case 3:
                    ContextId = (string)fieldValue;
                    break;
                case 4:
                    EventTypes = (IList<EventType>)fieldValue;
                    break;
                case 5:
                    AggregateSummary = (AggregateSummary)fieldValue;
                    break;
                case 6:
                    Auction = (Auction)fieldValue;
                    break;
                case 7:
                    Close = (Close)fieldValue;
                    break;
                case 8:
                    Financials = (Financials)fieldValue;
                    break;
                case 9:
                    IndexTick = (IndexTick)fieldValue;
                    break;
                case 10:
                    LastPrice = (LastPrice)fieldValue;
                    break;
                case 11:
                    MarketByPrice = (IList<MarketByPrice>)fieldValue;
                    break;
                case 12:
                    MidPrice = (MidPrice)fieldValue;
                    break;
                case 13:
                    NAVPrice = (NAVPrice)fieldValue;
                    break;
                case 14:
                    OHLPrice = (OHLPrice)fieldValue;
                    break;
                case 15:
                    InstrumentPerformanceStatistics = (InstrumentPerformanceStatistics)fieldValue;
                    break;
                case 16:
                    SettlementPrice = (SettlementPrice)fieldValue;
                    break;
                case 17:
                    SpreadStatistics = (SpreadStatistics)fieldValue;
                    break;
                case 18:
                    Status = (Status)fieldValue;
                    break;
                case 19:
                    TopOfBook = (TopOfBook)fieldValue;
                    break;
                case 20:
                    TradeCancellation = (TradeCancellation)fieldValue;
                    break;
                case 21:
                    TradeCorrection = (TradeCorrection)fieldValue;
                    break;
                case 22:
                    Trade = (Trade)fieldValue;
                    break;
                case 23:
                    Admin = (Admin)fieldValue;
                    break;
                default:
                    throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    // Below: each record also implements ISpecificRecord with its own schema, Get and Put.
    // This ensures the nested record types are Avro-compatible when used with Avro serializers.

    [GeneratedCode("avrogen", "1.12.0")]
    public class AggregateSummary : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"AggregateSummary\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"TradeCount\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"CumulativeVolume\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"VWAP\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"ImpliedVolatility\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"OpenInterest\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"OpenInterestDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"Turnover\",\"default\":null,\"type\":[\"null\",\"double\"]}]}"
        );

        private int? _TradeCount;
        private long? _CumulativeVolume;
        private double? _VWAP;
        private double? _ImpliedVolatility;
        private int? _OpenInterest;
        private long? _OpenInterestDateTime;
        private double? _Turnover;

        public virtual Schema Schema => _SCHEMA;

        public int? TradeCount { get => _TradeCount; set => _TradeCount = value; }
        public long? CumulativeVolume { get => _CumulativeVolume; set => _CumulativeVolume = value; }
        public double? VWAP { get => _VWAP; set => _VWAP = value; }
        public double? ImpliedVolatility { get => _ImpliedVolatility; set => _ImpliedVolatility = value; }
        public int? OpenInterest { get => _OpenInterest; set => _OpenInterest = value; }
        public long? OpenInterestDateTime { get => _OpenInterestDateTime; set => _OpenInterestDateTime = value; }
        public double? Turnover { get => _Turnover; set => _Turnover = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => TradeCount,
                1 => CumulativeVolume,
                2 => VWAP,
                3 => ImpliedVolatility,
                4 => OpenInterest,
                5 => OpenInterestDateTime,
                6 => Turnover,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: TradeCount = (int?)fieldValue; break;
                case 1: CumulativeVolume = (long?)fieldValue; break;
                case 2: VWAP = (double?)fieldValue; break;
                case 3: ImpliedVolatility = (double?)fieldValue; break;
                case 4: OpenInterest = (int?)fieldValue; break;
                case 5: OpenInterestDateTime = (long?)fieldValue; break;
                case 6: Turnover = (double?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class Auction : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"Auction\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"IndicativeAuctionPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"IndicativeAuctionSize\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"ImbalanceSize\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"ImbalanceSide\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"AuctionType\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"AuctionDate\",\"default\":null,\"type\":[\"null\",\"int\"]}]}"
        );

        private double? _IndicativeAuctionPrice;
        private int? _IndicativeAuctionSize;
        private long? _ImbalanceSize;
        private string? _ImbalanceSide;
        private string? _AuctionType;
        private int? _AuctionDate;

        public virtual Schema Schema => _SCHEMA;

        public double? IndicativeAuctionPrice { get => _IndicativeAuctionPrice; set => _IndicativeAuctionPrice = value; }
        public int? IndicativeAuctionSize { get => _IndicativeAuctionSize; set => _IndicativeAuctionSize = value; }
        public long? ImbalanceSize { get => _ImbalanceSize; set => _ImbalanceSize = value; }
        public string? ImbalanceSide { get => _ImbalanceSide; set => _ImbalanceSide = value; }
        public string? AuctionType { get => _AuctionType; set => _AuctionType = value; }
        public int? AuctionDate { get => _AuctionDate; set => _AuctionDate = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => IndicativeAuctionPrice,
                1 => IndicativeAuctionSize,
                2 => ImbalanceSize,
                3 => ImbalanceSide,
                4 => AuctionType,
                5 => AuctionDate,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: IndicativeAuctionPrice = (double?)fieldValue; break;
                case 1: IndicativeAuctionSize = (int?)fieldValue; break;
                case 2: ImbalanceSize = (long?)fieldValue; break;
                case 3: ImbalanceSide = (string)fieldValue; break;
                case 4: AuctionType = (string)fieldValue; break;
                case 5: AuctionDate = (int?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class Close : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"Close\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"ClosePrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"ClosePriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"UnadjustedPreviousClosePrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"VendorProvidedAdjustedPreviousClosePrice\",\"default\":null,\"type\":[\"null\",\"double\"]}]}"
        );

        private double? _ClosePrice;
        private long? _ClosePriceDateTime;
        private double? _UnadjustedPreviousClosePrice;
        private double? _VendorProvidedAdjustedPreviousClosePrice;

        public virtual Schema Schema => _SCHEMA;

        public double? ClosePrice { get => _ClosePrice; set => _ClosePrice = value; }
        public long? ClosePriceDateTime { get => _ClosePriceDateTime; set => _ClosePriceDateTime = value; }
        public double? UnadjustedPreviousClosePrice { get => _UnadjustedPreviousClosePrice; set => _UnadjustedPreviousClosePrice = value; }
        public double? VendorProvidedAdjustedPreviousClosePrice { get => _VendorProvidedAdjustedPreviousClosePrice; set => _VendorProvidedAdjustedPreviousClosePrice = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => ClosePrice,
                1 => ClosePriceDateTime,
                2 => UnadjustedPreviousClosePrice,
                3 => VendorProvidedAdjustedPreviousClosePrice,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: ClosePrice = (double?)fieldValue; break;
                case 1: ClosePriceDateTime = (long?)fieldValue; break;
                case 2: UnadjustedPreviousClosePrice = (double?)fieldValue; break;
                case 3: VendorProvidedAdjustedPreviousClosePrice = (double?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class Financials : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"Financials\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"PriceBookRatio\",\"default\":null,\"type\":[\"null\",\"double\"]}]}"
        );

        private double? _PriceBookRatio;
        public virtual Schema Schema => _SCHEMA;
        public double? PriceBookRatio { get => _PriceBookRatio; set => _PriceBookRatio = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => PriceBookRatio,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            if (fieldPos == 0) PriceBookRatio = (double?)fieldValue;
            else throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class IndexTick : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"IndexTick\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PriceCalculationDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"PricePublicationTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private double? _Price;
        private long? _PriceCalculationDateTime;
        private long? _PricePublicationTime;

        public virtual Schema Schema => _SCHEMA;
        public double? Price { get => _Price; set => _Price = value; }
        public long? PriceCalculationDateTime { get => _PriceCalculationDateTime; set => _PriceCalculationDateTime = value; }
        public long? PricePublicationTime { get => _PricePublicationTime; set => _PricePublicationTime = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => Price,
                1 => PriceCalculationDateTime,
                2 => PricePublicationTime,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: Price = (double?)fieldValue; break;
                case 1: PriceCalculationDateTime = (long?)fieldValue; break;
                case 2: PricePublicationTime = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class LastPrice : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"LastPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"PricePublishDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private double? _Price;
        private int? _Size;
        private long? _PricePublishDateTime;

        public virtual Schema Schema => _SCHEMA;
        public double? Price { get => _Price; set => _Price = value; }
        public int? Size { get => _Size; set => _Size = value; }
        public long? PricePublishDateTime { get => _PricePublishDateTime; set => _PricePublishDateTime = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => Price,
                1 => Size,
                2 => PricePublishDateTime,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: Price = (double?)fieldValue; break;
                case 1: Size = (int?)fieldValue; break;
                case 2: PricePublishDateTime = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class MarketByPrice : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"MarketByPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"RankLevel\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"Side\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Volume\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"NumberOfOrders\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"Timestamp\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private int? _RankLevel;
        private string? _Side;
        private double? _Price;
        private int? _Volume;
        private int? _NumberOfOrders;
        private long? _Timestamp;

        public virtual Schema Schema => _SCHEMA;

        public int? RankLevel { get => _RankLevel; set => _RankLevel = value; }
        public string? Side { get => _Side; set => _Side = value; }
        public double? Price { get => _Price; set => _Price = value; }
        public int? Volume { get => _Volume; set => _Volume = value; }
        public int? NumberOfOrders { get => _NumberOfOrders; set => _NumberOfOrders = value; }
        public long? Timestamp { get => _Timestamp; set => _Timestamp = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => RankLevel,
                1 => Side,
                2 => Price,
                3 => Volume,
                4 => NumberOfOrders,
                5 => Timestamp,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: RankLevel = (int?)fieldValue; break;
                case 1: Side = (string)fieldValue; break;
                case 2: Price = (double?)fieldValue; break;
                case 3: Volume = (int?)fieldValue; break;
                case 4: NumberOfOrders = (int?)fieldValue; break;
                case 5: Timestamp = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class MidPrice : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"MidPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PriceHigh\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PriceLow\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PriceClose\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private double? _Price;
        private double? _PriceHigh;
        private double? _PriceLow;
        private double? _PriceClose;
        private long? _DateTime;

        public virtual Schema Schema => _SCHEMA;
        public double? Price { get => _Price; set => _Price = value; }
        public double? PriceHigh { get => _PriceHigh; set => _PriceHigh = value; }
        public double? PriceLow { get => _PriceLow; set => _PriceLow = value; }
        public double? PriceClose { get => _PriceClose; set => _PriceClose = value; }
        public long? DateTime { get => _DateTime; set => _DateTime = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => Price,
                1 => PriceHigh,
                2 => PriceLow,
                3 => PriceClose,
                4 => DateTime,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: Price = (double?)fieldValue; break;
                case 1: PriceHigh = (double?)fieldValue; break;
                case 2: PriceLow = (double?)fieldValue; break;
                case 3: PriceClose = (double?)fieldValue; break;
                case 4: DateTime = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class NAVPrice : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"NAVPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"ExchangeNAV\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"NAVDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private double? _ExchangeNAV;
        private long? _NAVDateTime;

        public virtual Schema Schema => _SCHEMA;
        public double? ExchangeNAV { get => _ExchangeNAV; set => _ExchangeNAV = value; }
        public long? NAVDateTime { get => _NAVDateTime; set => _NAVDateTime = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => ExchangeNAV,
                1 => NAVDateTime,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: ExchangeNAV = (double?)fieldValue; break;
                case 1: NAVDateTime = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class OHLPrice : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"OHLPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"OpenPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"OpenPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"HighPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"HighPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"LowPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"LowPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private double? _OpenPrice;
        private long? _OpenPriceDateTime;
        private double? _HighPrice;
        private long? _HighPriceDateTime;
        private double? _LowPrice;
        private long? _LowPriceDateTime;

        public virtual Schema Schema => _SCHEMA;
        public double? OpenPrice { get => _OpenPrice; set => _OpenPrice = value; }
        public long? OpenPriceDateTime { get => _OpenPriceDateTime; set => _OpenPriceDateTime = value; }
        public double? HighPrice { get => _HighPrice; set => _HighPrice = value; }
        public long? HighPriceDateTime { get => _HighPriceDateTime; set => _HighPriceDateTime = value; }
        public double? LowPrice { get => _LowPrice; set => _LowPrice = value; }
        public long? LowPriceDateTime { get => _LowPriceDateTime; set => _LowPriceDateTime = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => OpenPrice,
                1 => OpenPriceDateTime,
                2 => HighPrice,
                3 => HighPriceDateTime,
                4 => LowPrice,
                5 => LowPriceDateTime,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: OpenPrice = (double?)fieldValue; break;
                case 1: OpenPriceDateTime = (long?)fieldValue; break;
                case 2: HighPrice = (double?)fieldValue; break;
                case 3: HighPriceDateTime = (long?)fieldValue; break;
                case 4: LowPrice = (double?)fieldValue; break;
                case 5: LowPriceDateTime = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class InstrumentPerformanceStatistics : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"InstrumentPerformanceStatistics\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"MidNetChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"MidPercentChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"NetChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PercentChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PreMarketNetChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PreMarketPercentChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PostMarketNetChange\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"PostMarketPercentChange\",\"default\":null,\"type\":[\"null\",\"double\"]}]}"
        );

        private double? _MidNetChange;
        private double? _MidPercentChange;
        private double? _NetChange;
        private double? _PercentChange;
        private double? _PreMarketNetChange;
        private double? _PreMarketPercentChange;
        private double? _PostMarketNetChange;
        private double? _PostMarketPercentChange;

        public virtual Schema Schema => _SCHEMA;

        public double? MidNetChange { get => _MidNetChange; set => _MidNetChange = value; }
        public double? MidPercentChange { get => _MidPercentChange; set => _MidPercentChange = value; }
        public double? NetChange { get => _NetChange; set => _NetChange = value; }
        public double? PercentChange { get => _PercentChange; set => _PercentChange = value; }
        public double? PreMarketNetChange { get => _PreMarketNetChange; set => _PreMarketNetChange = value; }
        public double? PreMarketPercentChange { get => _PreMarketPercentChange; set => _PreMarketPercentChange = value; }
        public double? PostMarketNetChange { get => _PostMarketNetChange; set => _PostMarketNetChange = value; }
        public double? PostMarketPercentChange { get => _PostMarketPercentChange; set => _PostMarketPercentChange = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => MidNetChange,
                1 => MidPercentChange,
                2 => NetChange,
                3 => PercentChange,
                4 => PreMarketNetChange,
                5 => PreMarketPercentChange,
                6 => PostMarketNetChange,
                7 => PostMarketPercentChange,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: MidNetChange = (double?)fieldValue; break;
                case 1: MidPercentChange = (double?)fieldValue; break;
                case 2: NetChange = (double?)fieldValue; break;
                case 3: PercentChange = (double?)fieldValue; break;
                case 4: PreMarketNetChange = (double?)fieldValue; break;
                case 5: PreMarketPercentChange = (double?)fieldValue; break;
                case 6: PostMarketNetChange = (double?)fieldValue; break;
                case 7: PostMarketPercentChange = (double?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class SettlementPrice : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"SettlementPrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"FinalSettlementPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"TodaysSettlementPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"SettlementPriceType\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"SettlementPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"PreviousSettlementPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"SettlementPriceMethod\",\"default\":null,\"type\":[\"null\",\"string\"]}]}"
        );

        private double? _FinalSettlementPrice;
        private double? _TodaysSettlementPrice;
        private int? _SettlementPriceType;
        private long? _SettlementPriceDateTime;
        private double? _PreviousSettlementPrice;
        private string? _SettlementPriceMethod;

        public virtual Schema Schema => _SCHEMA;

        public double? FinalSettlementPrice { get => _FinalSettlementPrice; set => _FinalSettlementPrice = value; }
        public double? TodaysSettlementPrice { get => _TodaysSettlementPrice; set => _TodaysSettlementPrice = value; }
        public int? SettlementPriceType { get => _SettlementPriceType; set => _SettlementPriceType = value; }
        public long? SettlementPriceDateTime { get => _SettlementPriceDateTime; set => _SettlementPriceDateTime = value; }
        public double? PreviousSettlementPrice { get => _PreviousSettlementPrice; set => _PreviousSettlementPrice = value; }
        public string? SettlementPriceMethod { get => _SettlementPriceMethod; set => _SettlementPriceMethod = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => FinalSettlementPrice,
                1 => TodaysSettlementPrice,
                2 => SettlementPriceType,
                3 => SettlementPriceDateTime,
                4 => PreviousSettlementPrice,
                5 => SettlementPriceMethod,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: FinalSettlementPrice = (double?)fieldValue; break;
                case 1: TodaysSettlementPrice = (double?)fieldValue; break;
                case 2: SettlementPriceType = (int?)fieldValue; break;
                case 3: SettlementPriceDateTime = (long?)fieldValue; break;
                case 4: PreviousSettlementPrice = (double?)fieldValue; break;
                case 5: SettlementPriceMethod = (string)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class SpreadStatistics : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"SpreadStatistics\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"BidAskSpread\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"DailyAverageSpreadPercentage\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"DailyAverageSpread\",\"default\":null,\"type\":[\"null\",\"double\"]}]}"
        );

        private double? _BidAskSpread;
        private double? _DailyAverageSpreadPercentage;
        private double? _DailyAverageSpread;

        public virtual Schema Schema => _SCHEMA;
        public double? BidAskSpread { get => _BidAskSpread; set => _BidAskSpread = value; }
        public double? DailyAverageSpreadPercentage { get => _DailyAverageSpreadPercentage; set => _DailyAverageSpreadPercentage = value; }
        public double? DailyAverageSpread { get => _DailyAverageSpread; set => _DailyAverageSpread = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => BidAskSpread,
                1 => DailyAverageSpreadPercentage,
                2 => DailyAverageSpread,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: BidAskSpread = (double?)fieldValue; break;
                case 1: DailyAverageSpreadPercentage = (double?)fieldValue; break;
                case 2: DailyAverageSpread = (double?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class Status : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"Status\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"InstrumentPhase\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"InstrumentPhaseDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"TradingStatus\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"ActiveInactiveFlag\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"TradingStatusDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private string? _InstrumentPhase;
        private long? _InstrumentPhaseDateTime;
        private int? _TradingStatus;
        private string? _ActiveInactiveFlag;
        private long? _TradingStatusDateTime;

        public virtual Schema Schema => _SCHEMA;
        public string? InstrumentPhase { get => _InstrumentPhase; set => _InstrumentPhase = value; }
        public long? InstrumentPhaseDateTime { get => _InstrumentPhaseDateTime; set => _InstrumentPhaseDateTime = value; }
        public int? TradingStatus { get => _TradingStatus; set => _TradingStatus = value; }
        public string? ActiveInactiveFlag { get => _ActiveInactiveFlag; set => _ActiveInactiveFlag = value; }
        public long? TradingStatusDateTime { get => _TradingStatusDateTime; set => _TradingStatusDateTime = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => InstrumentPhase,
                1 => InstrumentPhaseDateTime,
                2 => TradingStatus,
                3 => ActiveInactiveFlag,
                4 => TradingStatusDateTime,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: InstrumentPhase = (string)fieldValue; break;
                case 1: InstrumentPhaseDateTime = (long?)fieldValue; break;
                case 2: TradingStatus = (int?)fieldValue; break;
                case 3: ActiveInactiveFlag = (string)fieldValue; break;
                case 4: TradingStatusDateTime = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class TopOfBook : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"TopOfBook\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"AskPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"AskSize\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"AskPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"AskConditionFlag\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"AskExchange\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"BidPrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"BidSize\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"BidPriceDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"BidConditionFlag\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"BidExchange\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"BidPriceClose\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"AskPriceClose\",\"default\":null,\"type\":[\"null\",\"double\"]}]}"
        );

        private double? _AskPrice;
        private int? _AskSize;
        private long? _AskPriceDateTime;
        private int? _AskConditionFlag;
        private int? _AskExchange;
        private double? _BidPrice;
        private int? _BidSize;
        private long? _BidPriceDateTime;
        private int? _BidConditionFlag;
        private int? _BidExchange;
        private double? _BidPriceClose;
        private double? _AskPriceClose;

        public virtual Schema Schema => _SCHEMA;

        public double? AskPrice { get => _AskPrice; set => _AskPrice = value; }
        public int? AskSize { get => _AskSize; set => _AskSize = value; }
        public long? AskPriceDateTime { get => _AskPriceDateTime; set => _AskPriceDateTime = value; }
        public int? AskConditionFlag { get => _AskConditionFlag; set => _AskConditionFlag = value; }
        public int? AskExchange { get => _AskExchange; set => _AskExchange = value; }
        public double? BidPrice { get => _BidPrice; set => _BidPrice = value; }
        public int? BidSize { get => _BidSize; set => _BidSize = value; }
        public long? BidPriceDateTime { get => _BidPriceDateTime; set => _BidPriceDateTime = value; }
        public int? BidConditionFlag { get => _BidConditionFlag; set => _BidConditionFlag = value; }
        public int? BidExchange { get => _BidExchange; set => _BidExchange = value; }
        public double? BidPriceClose { get => _BidPriceClose; set => _BidPriceClose = value; }
        public double? AskPriceClose { get => _AskPriceClose; set => _AskPriceClose = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => AskPrice,
                1 => AskSize,
                2 => AskPriceDateTime,
                3 => AskConditionFlag,
                4 => AskExchange,
                5 => BidPrice,
                6 => BidSize,
                7 => BidPriceDateTime,
                8 => BidConditionFlag,
                9 => BidExchange,
                10 => BidPriceClose,
                11 => AskPriceClose,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: AskPrice = (double?)fieldValue; break;
                case 1: AskSize = (int?)fieldValue; break;
                case 2: AskPriceDateTime = (long?)fieldValue; break;
                case 3: AskConditionFlag = (int?)fieldValue; break;
                case 4: AskExchange = (int?)fieldValue; break;
                case 5: BidPrice = (double?)fieldValue; break;
                case 6: BidSize = (int?)fieldValue; break;
                case 7: BidPriceDateTime = (long?)fieldValue; break;
                case 8: BidConditionFlag = (int?)fieldValue; break;
                case 9: BidExchange = (int?)fieldValue; break;
                case 10: BidPriceClose = (double?)fieldValue; break;
                case 11: AskPriceClose = (double?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class TradeCancellation : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"TradeCancellation\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"OriginalTradeID\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private string? _OriginalTradeID;
        private long? _DateTime;

        public virtual Schema Schema => _SCHEMA;
        public string? OriginalTradeID { get => _OriginalTradeID; set => _OriginalTradeID = value; }
        public long? DateTime { get => _DateTime; set => _DateTime = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => OriginalTradeID,
                1 => DateTime,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: OriginalTradeID = (string)fieldValue; break;
                case 1: DateTime = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class TradeCorrection : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"TradeCorrection\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"CorrectedTradePrice\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"CorrectedTradeSize\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"OriginalTradeID\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"CorrectedTradeConditions\",\"default\":null,\"type\":[\"null\",\"string\"]}]}"
        );

        private double? _CorrectedTradePrice;
        private int? _CorrectedTradeSize;
        private string? _OriginalTradeID;
        private long? _DateTime;
        private string? _CorrectedTradeConditions;

        public virtual Schema Schema => _SCHEMA;

        public double? CorrectedTradePrice { get => _CorrectedTradePrice; set => _CorrectedTradePrice = value; }
        public int? CorrectedTradeSize { get => _CorrectedTradeSize; set => _CorrectedTradeSize = value; }
        public string? OriginalTradeID { get => _OriginalTradeID; set => _OriginalTradeID = value; }
        public long? DateTime { get => _DateTime; set => _DateTime = value; }
        public string? CorrectedTradeConditions { get => _CorrectedTradeConditions; set => _CorrectedTradeConditions = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => CorrectedTradePrice,
                1 => CorrectedTradeSize,
                2 => OriginalTradeID,
                3 => DateTime,
                4 => CorrectedTradeConditions,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: CorrectedTradePrice = (double?)fieldValue; break;
                case 1: CorrectedTradeSize = (int?)fieldValue; break;
                case 2: OriginalTradeID = (string)fieldValue; break;
                case 3: DateTime = (long?)fieldValue; break;
                case 4: CorrectedTradeConditions = (string)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    // Trade and its related records
    [GeneratedCode("avrogen", "1.12.0")]
    public class Trade : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"Trade\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"TradePostMarket\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TradePostMarket\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"CumulativeVolume\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"Count\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"TradePreMarket\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TradePreMarket\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"CumulativeVolume\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"Count\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}]},{\"name\":\"TradeRegulatory\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TradeRegulatory\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"MMTFlagTransactionCategory\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagMarketMechanism\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagTradingMode\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagDuplicativeTradeIndicator\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagPublicationMode\",\"default\":null,\"type\":[\"null\",\"string\"]}]}]},{\"name\":\"TradePrice\",\"default\":null,\"type\":[\"null\",{\"type\":\"record\",\"name\":\"TradePrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"ExecutionDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"Conditions\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"IdentificationCode\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"PublishDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"ExecutionVenue\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"ExecutionCurrency\",\"default\":null,\"type\":[\"null\",\"string\"]}]}]}]}"
        );

        private TradePostMarket? _TradePostMarket;
        private TradePreMarket? _TradePreMarket;
        private TradeRegulatory? _TradeRegulatory;
        private TradePrice? _TradePrice;

        public virtual Schema Schema => _SCHEMA;

        public TradePostMarket? TradePostMarket { get => _TradePostMarket; set => _TradePostMarket = value; }
        public TradePreMarket? TradePreMarket { get => _TradePreMarket; set => _TradePreMarket = value; }
        public TradeRegulatory? TradeRegulatory { get => _TradeRegulatory; set => _TradeRegulatory = value; }
        public TradePrice? TradePrice { get => _TradePrice; set => _TradePrice = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => TradePostMarket,
                1 => TradePreMarket,
                2 => TradeRegulatory,
                3 => TradePrice,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: TradePostMarket = (TradePostMarket)fieldValue; break;
                case 1: TradePreMarket = (TradePreMarket)fieldValue; break;
                case 2: TradeRegulatory = (TradeRegulatory)fieldValue; break;
                case 3: TradePrice = (TradePrice)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class TradePostMarket : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"TradePostMarket\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"CumulativeVolume\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"Count\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private double? _Price;
        private int? _Size;
        private int? _CumulativeVolume;
        private int? _Count;
        private long? _DateTime;

        public virtual Schema Schema => _SCHEMA;
        public double? Price { get => _Price; set => _Price = value; }
        public int? Size { get => _Size; set => _Size = value; }
        public int? CumulativeVolume { get => _CumulativeVolume; set => _CumulativeVolume = value; }
        public int? Count { get => _Count; set => _Count = value; }
        public long? DateTime { get => _DateTime; set => _DateTime = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => Price,
                1 => Size,
                2 => CumulativeVolume,
                3 => Count,
                4 => DateTime,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: Price = (double?)fieldValue; break;
                case 1: Size = (int?)fieldValue; break;
                case 2: CumulativeVolume = (int?)fieldValue; break;
                case 3: Count = (int?)fieldValue; break;
                case 4: DateTime = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class TradePreMarket : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"TradePreMarket\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"CumulativeVolume\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"Count\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"DateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private double? _Price;
        private int? _Size;
        private int? _CumulativeVolume;
        private int? _Count;
        private long? _DateTime;

        public virtual Schema Schema => _SCHEMA;
        public double? Price { get => _Price; set => _Price = value; }
        public int? Size { get => _Size; set => _Size = value; }
        public int? CumulativeVolume { get => _CumulativeVolume; set => _CumulativeVolume = value; }
        public int? Count { get => _Count; set => _Count = value; }
        public long? DateTime { get => _DateTime; set => _DateTime = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => Price,
                1 => Size,
                2 => CumulativeVolume,
                3 => Count,
                4 => DateTime,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: Price = (double?)fieldValue; break;
                case 1: Size = (int?)fieldValue; break;
                case 2: CumulativeVolume = (int?)fieldValue; break;
                case 3: Count = (int?)fieldValue; break;
                case 4: DateTime = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class TradeRegulatory : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"TradeRegulatory\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"MMTFlagTransactionCategory\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagMarketMechanism\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagTradingMode\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagDuplicativeTradeIndicator\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"MMTFlagPublicationMode\",\"default\":null,\"type\":[\"null\",\"string\"]}]}"
        );

        private string? _MMTFlagTransactionCategory;
        private string? _MMTFlagMarketMechanism;
        private string? _MMTFlagTradingMode;
        private string? _MMTFlagDuplicativeTradeIndicator;
        private string? _MMTFlagPublicationMode;
        public virtual Schema Schema => _SCHEMA;

        public string? MMTFlagTransactionCategory { get => _MMTFlagTransactionCategory; set => _MMTFlagTransactionCategory = value; }
        public string? MMTFlagMarketMechanism { get => _MMTFlagMarketMechanism; set => _MMTFlagMarketMechanism = value; }
        public string? MMTFlagTradingMode { get => _MMTFlagTradingMode; set => _MMTFlagTradingMode = value; }
        public string? MMTFlagDuplicativeTradeIndicator { get => _MMTFlagDuplicativeTradeIndicator; set => _MMTFlagDuplicativeTradeIndicator = value; }
        public string? MMTFlagPublicationMode { get => _MMTFlagPublicationMode; set => _MMTFlagPublicationMode = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => MMTFlagTransactionCategory,
                1 => MMTFlagMarketMechanism,
                2 => MMTFlagTradingMode,
                3 => MMTFlagDuplicativeTradeIndicator,
                4 => MMTFlagPublicationMode,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: MMTFlagTransactionCategory = (string)fieldValue; break;
                case 1: MMTFlagMarketMechanism = (string)fieldValue; break;
                case 2: MMTFlagTradingMode = (string)fieldValue; break;
                case 3: MMTFlagDuplicativeTradeIndicator = (string)fieldValue; break;
                case 4: MMTFlagPublicationMode = (string)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class TradePrice : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"TradePrice\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Price\",\"default\":null,\"type\":[\"null\",\"double\"]},{\"name\":\"Size\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"ExecutionDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"Conditions\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"IdentificationCode\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"PublishDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]},{\"name\":\"ExecutionVenue\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"ExecutionCurrency\",\"default\":null,\"type\":[\"null\",\"string\"]}]}"
        );

        private double? _Price;
        private int? _Size;
        private long? _ExecutionDateTime;
        private string? _Conditions;
        private string? _IdentificationCode;
        private long? _PublishDateTime;
        private string? _ExecutionVenue;
        private string? _ExecutionCurrency;
        public virtual Schema Schema => _SCHEMA;

        public double? Price { get => _Price; set => _Price = value; }
        public int? Size { get => _Size; set => _Size = value; }
        public long? ExecutionDateTime { get => _ExecutionDateTime; set => _ExecutionDateTime = value; }
        public string? Conditions { get => _Conditions; set => _Conditions = value; }
        public string? IdentificationCode { get => _IdentificationCode; set => _IdentificationCode = value; }
        public long? PublishDateTime { get => _PublishDateTime; set => _PublishDateTime = value; }
        public string? ExecutionVenue { get => _ExecutionVenue; set => _ExecutionVenue = value; }
        public string? ExecutionCurrency { get => _ExecutionCurrency; set => _ExecutionCurrency = value; }
        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => Price,
                1 => Size,
                2 => ExecutionDateTime,
                3 => Conditions,
                4 => IdentificationCode,
                5 => PublishDateTime,
                6 => ExecutionVenue,
                7 => ExecutionCurrency,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: Price = (double?)fieldValue; break;
                case 1: Size = (int?)fieldValue; break;
                case 2: ExecutionDateTime = (long?)fieldValue; break;
                case 3: Conditions = (string)fieldValue; break;
                case 4: IdentificationCode = (string)fieldValue; break;
                case 5: PublishDateTime = (long?)fieldValue; break;
                case 6: ExecutionVenue = (string)fieldValue; break;
                case 7: ExecutionCurrency = (string)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }

    [GeneratedCode("avrogen", "1.12.0")]
    public class Admin : ISpecificRecord
    {
        public static Schema _SCHEMA = Schema.Parse(
            "{\"type\":\"record\",\"name\":\"Admin\",\"namespace\":\"com.morningstar.rtd.data.polarisetl\",\"fields\":[{\"name\":\"Notice\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"NoticeType\",\"default\":null,\"type\":[\"null\",{\"type\":\"enum\",\"name\":\"NoticeType\",\"namespace\":\"com.morningstar.rtd.datatypes\",\"symbols\":[\"Unknown\",\"Disconnect\"],\"default\":\"Unknown\"}]},{\"name\":\"Description\",\"default\":null,\"type\":[\"null\",\"string\"]},{\"name\":\"Arbitrate\",\"default\":null,\"type\":[\"null\",\"boolean\"]},{\"name\":\"NoticeMinutes\",\"default\":null,\"type\":[\"null\",\"int\"]},{\"name\":\"NoticeDateTime\",\"default\":null,\"type\":[\"null\",\"long\"]}]}"
        );

        private string? _Notice;
        private com.morningstar.rtd.datatypes.NoticeType? _NoticeType;
        private string? _Description;
        private bool? _Arbitrate;
        private int? _NoticeMinutes;
        private long? _NoticeDateTime;

        public virtual Schema Schema => _SCHEMA;

        public string? Notice { get => _Notice; set => _Notice = value; }
        public com.morningstar.rtd.datatypes.NoticeType? NoticeType { get => _NoticeType; set => _NoticeType = value; }
        public string? Description { get => _Description; set => _Description = value; }
        public bool? Arbitrate { get => _Arbitrate; set => _Arbitrate = value; }
        public int? NoticeMinutes { get => _NoticeMinutes; set => _NoticeMinutes = value; }
        public long? NoticeDateTime { get => _NoticeDateTime; set => _NoticeDateTime = value; }

        public virtual object? Get(int fieldPos)
        {
            return fieldPos switch
            {
                0 => Notice,
                1 => NoticeType,
                2 => Description,
                3 => Arbitrate,
                4 => NoticeMinutes,
                5 => NoticeDateTime,
                _ => throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()")
            };
        }

        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: Notice = (string)fieldValue; break;
                case 1: NoticeType = (com.morningstar.rtd.datatypes.NoticeType?)fieldValue; break;
                case 2: Description = (string)fieldValue; break;
                case 3: Arbitrate = (bool?)fieldValue; break;
                case 4: NoticeMinutes = (int?)fieldValue; break;
                case 5: NoticeDateTime = (long?)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
        }
    }
}