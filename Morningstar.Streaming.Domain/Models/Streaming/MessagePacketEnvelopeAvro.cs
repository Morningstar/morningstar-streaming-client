using Newtonsoft.Json.Linq;

namespace Morningstar.Streaming.Domain.Models.Streaming;

public class MessagePacketEnvelopeAvro
{
    public int? InstrumentId { get; set; }
    public string? ContextId { get; set; }
    public string? PerformanceId { get; set; }
    public List<string>? EventTypes { get; set; } = null!;
    public long? SequenceNumber { get; set; }
    public JObject? AggregateSummary { get; set; }
    public JObject? Auction { get; set; }
    public JObject? Close { get; set; }
    public JObject? Financials { get; set; }
    public JObject? IndexTick { get; set; }
    public JObject? LastPrice { get; set; }
    public JObject? MidPrice { get; set; }
    public IList<JObject>? MarketByPrice { get; set; }
    public JObject? NAVPrice { get; set; }
    public JObject? OHLPrice { get; set; }
    public JObject? InstrumentPerformanceStatistics { get; set; }
    public JObject? SettlementPrice { get; set; }
    public JObject? SpreadStatistics { get; set; }
    public JObject? Status { get; set; }
    public JObject? TopOfBook { get; set; }
    public JObject? TradeCancellation { get; set; }
    public JObject? TradeCorrection { get; set; }
    public JObject? Trade { get; set; }
    public JObject? Admin { get; set; }
}
