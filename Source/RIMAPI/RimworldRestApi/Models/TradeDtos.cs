using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;

namespace RIMAPI.Models
{
    public class TraderKindDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }

        // Using short boolean flags (0/1 or omit if false) could optimize further, 
        // but standard bools are fine for readability.
        public bool Orbital { get; set; }
        public bool Visitor { get; set; }
        public float Commonality { get; set; }

        // -- BUCKETS --
        // We only initialize these if they have data to keep JSON clean
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<StockRuleDto> Items { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<StockRuleDto> Categories { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<StockRuleDto> Tags { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<StockRuleDto> Special { get; set; }
    }

    public class StockRuleDto
    {
        public string Name { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Count { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Price { get; set; }

        // FIX: Explicitly tell serializer that 'true' is the default
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(true)]
        public bool Buys { get; set; } = true;
    }

    // A live trader, not a trader kind (ticket #3170): a caravan visitor or an
    // orbital trade ship on the map, with the silver it holds and the stock it
    // sells. Id is the pawn's thing id for a visitor and negative for an
    // orbital ship (the tool prints it, and a trade call takes it back).
    public class TraderDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public string Label { get; set; }
        public string Faction { get; set; }
        public bool Orbital { get; set; }
        public bool CanTradeNow { get; set; }
        public int Silver { get; set; }
        public int NegotiatorId { get; set; }
        public string Negotiator { get; set; }
        public List<TraderStockDto> Stock { get; set; }
        // The colony's side of the deal (fox ticket #3541): the silver the
        // deal with this trader would count, which is not the map's silver
        // total. Null when no colonist can trade right now, because the game
        // builds that list for a negotiator.
        public int? ColonySilver { get; set; }
        // Every orbital trade beacon on the map, so a run can build or move
        // one before an orbital purchase is refused. Empty for a caravan
        // visitor, whose rule never asks for a beacon.
        public List<TradeBeaconDto> Beacons { get; set; }
    }

    // One orbital trade beacon: the cell it stands on and whether it has
    // power. Only a powered one carries goods for an orbital trade.
    public class TradeBeaconDto
    {
        public int X { get; set; }
        public int Z { get; set; }
        public bool Powered { get; set; }
    }

    // One line of a trader's stock, grouped by def and material. MarketValue is
    // the game's base value per unit, not the price the trader asks.
    public class TraderStockDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public int Count { get; set; }
        public float MarketValue { get; set; }
    }

    public class TradeRequestDto
    {
        public int MapId { get; set; }
        public int TraderId { get; set; }
        // Op is buy or sell.
        public string Op { get; set; }
        public string DefName { get; set; }
        public int Count { get; set; }
    }

    public class TradeResultDto
    {
        public bool Traded { get; set; }
        public string Op { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        public int Count { get; set; }
        // Silver is what moved for the colony: positive paid out, negative
        // received. ColonySilver and TraderSilver are read after the deal.
        public int Silver { get; set; }
        public int ColonySilver { get; set; }
        public int TraderSilver { get; set; }
        public string Note { get; set; }
    }
}
