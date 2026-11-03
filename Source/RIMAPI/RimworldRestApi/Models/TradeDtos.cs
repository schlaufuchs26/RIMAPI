using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class TraderKindDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }

        // Using short boolean flags (0/1 or omit if false) could optimize further, 
        // but standard bools are fine for readability.
        [ParameterDescription("Whether the trader trades from orbit.")]
        public bool Orbital { get; set; }
        [ParameterDescription("Whether the trader walks into the colony.")]
        public bool Visitor { get; set; }
        [ParameterDescription("How often this trader kind appears.")]
        public float Commonality { get; set; }

        // -- BUCKETS --
        // We only initialize these if they have data to keep JSON clean
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [ParameterDescription("The item categories the trader deals in.")]
        public List<StockRuleDto> Items { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [ParameterDescription("The categories the trader's stock falls in.")]
        public List<StockRuleDto> Categories { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [ParameterDescription("The tags that describe the trader kind.")]
        public List<StockRuleDto> Tags { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [ParameterDescription("What is special about this trader kind.")]
        public List<StockRuleDto> Special { get; set; }
    }

    public class StockRuleDto
    {
        public string Name { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Count { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [ParameterDescription("The price the trader asks per unit.")]
        public string Price { get; set; }

        // FIX: Explicitly tell serializer that 'true' is the default
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(true)]
        [ParameterDescription("Whether the trader buys this item from the colony.")]
        public bool Buys { get; set; } = true;
    }

    // A live trader, not a trader kind (ticket #3170): a caravan visitor or an
    // orbital trade ship on the map, with the silver it holds and the stock it
    // sells. Id is the pawn's thing id for a visitor and negative for an
    // orbital ship (the tool prints it, and a trade call takes it back).
    public class TraderDto
    {
        [ParameterDescription("The trader's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The trader kind's def name.")]
        public string Kind { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The faction the trader belongs to.")]
        public string Faction { get; set; }
        [ParameterDescription("Whether the trader trades from orbit.")]
        public bool Orbital { get; set; }
        [ParameterDescription("Whether the trader will trade right now.")]
        public bool CanTradeNow { get; set; }
        [ParameterDescription("How much silver the trader carries.")]
        public int Silver { get; set; }
        [ParameterDescription("The numeric id of the colony's negotiator.")]
        public int NegotiatorId { get; set; }
        [ParameterDescription("The name of the colony's negotiator.")]
        public string Negotiator { get; set; }
        [ParameterDescription("One entry per item the trader offers.")]
        public List<TraderStockDto> Stock { get; set; }
        // The colony's side of the deal (fox ticket #3541): the silver the
        // deal with this trader would count, which is not the map's silver
        // total. Null when no colonist can trade right now, because the game
        // builds that list for a negotiator.
        [ParameterDescription("How much silver the colony holds.")]
        public int? ColonySilver { get; set; }
        // Every orbital trade beacon on the map, so a run can build or move
        // one before an orbital purchase is refused. Empty for a caravan
        // visitor, whose rule never asks for a beacon.
        [ParameterDescription("The orbital trade beacons the trader reaches.")]
        public List<TradeBeaconDto> Beacons { get; set; }
    }

    // One orbital trade beacon: the cell it stands on and whether it has
    // power. Only a powered one carries goods for an orbital trade.
    public class TradeBeaconDto
    {
        public int X { get; set; }
        public int Z { get; set; }
        [ParameterDescription("Whether the beacon has power.")]
        public bool Powered { get; set; }
    }

    // One line of a trader's stock, grouped by def and material. MarketValue is
    // the game's base value per unit, not the price the trader asks.
    public class TraderStockDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public int Count { get; set; }
        [ParameterDescription("What one unit is worth in silver.")]
        public float MarketValue { get; set; }
    }

    public class TradeRequestDto
    {
        public int MapId { get; set; }
        [ParameterDescription("The trader's numeric id.")]
        public int TraderId { get; set; }
        // Op is buy or sell.
        [ParameterDescription("The trade operation: buy or sell.")]
        public string Op { get; set; }
        public string DefName { get; set; }
        public int Count { get; set; }
    }

    public class TradeResultDto
    {
        [ParameterDescription("Whether the deal went through.")]
        public bool Traded { get; set; }
        [ParameterDescription("The trade operation that ran.")]
        public string Op { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        public int Count { get; set; }
        // Silver is what moved for the colony: positive paid out, negative
        // received. ColonySilver and TraderSilver are read after the deal.
        [ParameterDescription("How much silver the deal moved.")]
        public int Silver { get; set; }
        [ParameterDescription("How much silver the colony holds after it.")]
        public int ColonySilver { get; set; }
        [ParameterDescription("How much silver the trader holds after it.")]
        public int TraderSilver { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }
}
