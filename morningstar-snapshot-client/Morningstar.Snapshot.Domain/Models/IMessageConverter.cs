using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Morningstar.Snapshot.Domain.Models;

/// <summary>
/// Custom JsonConverter for IMessage interface to handle polymorphic deserialization.
/// Determines the concrete type based on the JSON properties present.
/// </summary>
public class IMessageConverter : JsonConverter
{
    // Mapping of property names to their corresponding message types
    private static readonly Dictionary<string, Type> PropertyTypeMap = new()
    {
        // LastPriceMessage properties
        { "price", typeof(LastPriceMessage) },
        { "size", typeof(LastPriceMessage) },
        { "pricePublishDateTime", typeof(LastPriceMessage) },

        // TopOfBookMessage properties
        { "askPrice", typeof(TopOfBookMessage) },
        { "bidPrice", typeof(TopOfBookMessage) },
        { "askSize", typeof(TopOfBookMessage) },
        { "bidSize", typeof(TopOfBookMessage) },
        { "rankLevel", typeof(MarketByPriceMessage) },
        { "side", typeof(MarketByPriceMessage) },
        { "volume", typeof(MarketByPriceMessage) },
        { "numberOfOrders", typeof(MarketByPriceMessage) },

        // TradeMessage properties
        { "tradePrice", typeof(TradeMessage) },
        { "tradePostMarket", typeof(TradeMessage) },
        { "tradePreMarket", typeof(TradeMessage) },
        { "tradeRegulatory", typeof(TradeMessage) },

        // OHLPriceMessage properties
        { "openPrice", typeof(OHLPriceMessage) },
        { "highPrice", typeof(OHLPriceMessage) },
        { "lowPrice", typeof(OHLPriceMessage) },

        // MidPriceMessage properties
        { "priceLow", typeof(MidPriceMessage) },
        { "priceHigh", typeof(MidPriceMessage) },
        { "priceClose", typeof(MidPriceMessage) },

        // CloseMessage properties
        { "closePrice", typeof(CloseMessage) },
        { "closePriceDateTime", typeof(CloseMessage) },
        { "unadjustedPreviousClosePrice", typeof(CloseMessage) },

        // TopOfBookMessage extended properties
        { "askConditionFlag", typeof(TopOfBookMessage) },
        { "askExchange", typeof(TopOfBookMessage) },
        { "bidConditionFlag", typeof(TopOfBookMessage) },
        { "bidExchange", typeof(TopOfBookMessage) },

        // AggregateSummaryMessage properties
        { "tradeCount", typeof(AggregateSummaryMessage) },
        { "cumulativeVolume", typeof(AggregateSummaryMessage) },
        { "vwap", typeof(AggregateSummaryMessage) },

        // AuctionMessage properties
        { "indicativeAuctionPrice", typeof(AuctionMessage) },
        { "imbalanceSize", typeof(AuctionMessage) },

        // StatusMessage properties
        { "instrumentPhase", typeof(StatusMessage) },
        { "tradingStatus", typeof(StatusMessage) },

        // SettlementPriceMessage properties
        { "finalSettlementPrice", typeof(SettlementPriceMessage) },
        { "settlementPriceType", typeof(SettlementPriceMessage) },

        // SpreadStatisticsMessage properties
        { "bidAskSpread", typeof(SpreadStatisticsMessage) },
        { "dailyAverageSpreadPercentage", typeof(SpreadStatisticsMessage) },

        // NAVPriceMessage properties
        { "exchangeNAV", typeof(NAVPriceMessage) },
        { "navDateTime", typeof(NAVPriceMessage) },

        // InstrumentPerformanceStatisticsMessage properties
        { "netChange", typeof(InstrumentPerformanceStatisticsMessage) },
        { "percentChange", typeof(InstrumentPerformanceStatisticsMessage) },

        // FinancialsMessage properties
        { "intradayPriceToBookRatio", typeof(FinancialsMessage) },

        // AdminMessage properties
        { "notice", typeof(AdminMessage) },
        { "description", typeof(AdminMessage) },

        // TradeCancellationMessage properties
        { "originalTradeID", typeof(TradeCancellationMessage) },

        // TradeCorrectionMessage properties
        { "correctedTradePrice", typeof(TradeCorrectionMessage) },
        { "correctedTradeSize", typeof(TradeCorrectionMessage) },

        // IndexTickMessage properties
        { "priceCalculationDateTime", typeof(IndexTickMessage) },
    };

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IMessage);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        var targetType = DetermineMessageType(jObject);

        if (targetType == null)
        {
            // Default to a generic object if type cannot be determined
            return null;
        }

        return JsonConvert.DeserializeObject(jObject.ToString(), targetType, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new IMessageConverter() }
        });
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    /// <summary>
    /// Determines the concrete message type based on the properties in the JSON object.
    /// </summary>
    private Type? DetermineMessageType(JObject jObject)
    {
        if (jObject == null || jObject.Count == 0)
            return null;

        // Get all property names from the JSON object (case-insensitive)
        var properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in jObject.Properties())
        {
            properties.Add(prop.Name);
        }

        // Score each potential type based on matching properties
        var typeScores = new Dictionary<Type, int>();

        foreach (var property in properties)
        {
            if (PropertyTypeMap.TryGetValue(property.ToLower(), out var type))
            {
                if (!typeScores.ContainsKey(type))
                    typeScores[type] = 0;
                typeScores[type]++;
            }
        }

        // Return the type with the highest score
        if (typeScores.Count > 0)
        {
            var bestMatch = typeScores.OrderByDescending(x => x.Value).First();
            return bestMatch.Key;
        }

        return null;
    }
}
