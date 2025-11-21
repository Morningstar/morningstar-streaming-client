using System.Runtime.Serialization;

namespace Morningstar.Streaming.Domain.Contracts
{
    /// <summary>
    /// Stream request object.
    /// </summary>
    [DataContract]
    public class StreamRequest
    {
        /// <summary>
        /// A list of Morningstar Performance Ids.
        /// </summary>
        [DataMember]
        public List<Investments> Investments { get; set; }

        /// <summary>
        /// A list of event types to subscribe to.
        /// 
        /// Possible values:
        /// 
        ///  * "AggregateSummary" - Aggregated trade statistics including cumulative volume, trade count, VWAP, and open interest.
        ///  * "Auction" - Indicative auction pricing, imbalance data, and auction metadata.
        ///  * "Close" - End-of-day closing price, adjusted and unadjusted previous close.
        ///  * "IndexTick" - Index level with calculation and publication timestamps.
        ///  * "InstrumentPerformanceStatistics" - Net and percent change metrics across trading sessions.
        ///  * "LastPrice" - Most recent trade price and size that meets eligibility criteria for official last price display.
        ///  * "MidPrice" - Midpoint pricing with high, low, close, and timestamp values.
        ///  * "NAVPrice" - Net Asset Value with corresponding timestamp.
        ///  * "OHLPrice" - Open, High, and Low prices for the current trading session with timestamps.
        ///  * "SettlementPrice" - Final, current, and previous settlement prices with calculation and method details.
        ///  * "SpreadStatistics" - Bid-ask spread values and daily spread averages.
        ///  * "Status" - Instrument trading status, phase, and activity flags. 
        ///  * "TopOfBook" - Best Bid and Ask prices, sizes, timestamps, and exchange-level details.
        ///  * "Trade" - Executed trade details including price, size, execution time, venue, and conditions.
        ///     * Subtypes also delivered when subscribing to the "Trade" event type:
        ///     * "TradePreMarket" - Trade activity before official market open, including volume and pricing.
        ///     * "TradePostMarket" - Trade activity after market close, including volume and pricing.
        ///  * "TradeCancellation" - Cancelled trade details.
        ///  * "TradeCorrection" - Corrected trade details.
        /// </summary>
        [DataMember]
        public string[] EventTypes { get; set; }
    }
}
