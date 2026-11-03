using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class ThingDto
    {
        public int ThingId { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The thing categories the def belongs to.")]
        public List<string> Categories { get; set; }
        public PositionDto Position { get; set; }
        [ParameterDescription("The thing's rotation: 0, 90, 180 or 270.")]
        public int Rotation { get; set; }
        [ParameterDescription("How many cells the thing covers.")]
        public PositionDto Size { get; set; }
        [ParameterDescription("How many units the stack holds.")]
        public int StackCount { get; set; }
        [ParameterDescription("What one unit is worth in silver.")]
        public double MarketValue { get; set; }
        [ParameterDescription("Whether the colony is forbidden to touch it.")]
        public bool IsForbidden { get; set; }
        public int Quality { get; set; }
        [ParameterDescription("The material's def name, e.g. \"Steel\".")]
        public string StuffDefName { get; set; }
        [ParameterDescription("The thing's current hit points.")]
        public int HitPoints { get; set; }
        [ParameterDescription("The thing's hit points when unhurt.")]
        public int MaxHitPoints { get; set; }
        public string Description { get; set; }

        public static ThingDto ToDto(Thing thing)
        {
            var dto = new ThingDto
            {
                ThingId = thing.thingIDNumber,
                DefName = thing.def.defName,
                Label = thing.Label,
                Categories = thing.def.thingCategories?.Select(c => c.defName).ToList() ?? new List<string>(),
                Position = new PositionDto
                {
                    X = thing.Position.x,
                    Y = thing.Position.y,
                    Z = thing.Position.z,
                },
                StackCount = thing.stackCount,
                MarketValue = thing.MarketValue,
                IsForbidden = thing.IsForbidden(Faction.OfPlayer),
                HitPoints = thing.HitPoints,
                MaxHitPoints = thing.MaxHitPoints,
                Rotation = thing.Rotation.AsInt,

                // Map the new fields
                StuffDefName = thing.Stuff?.defName,
                Description = thing.DescriptionDetailed
            };

            // Safely get Quality
            var qualityComp = thing.TryGetComp<CompQuality>();
            if (qualityComp != null)
            {
                dto.Quality = (int)qualityComp.Quality;
            }

            return dto;
        }
    }

    public class SetForbiddenRequestDto
    {
        [ParameterDescription("The things to change, by numeric id.")]
        public List<int> ThingIds { get; set; }
        public int MapId { get; set; }
        [ParameterDescription("Whether the things become forbidden.")]
        public bool Forbidden { get; set; }
    }

    public class ThingSourcesDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The thing categories this entry covers.")]
        public List<string> ThingCategories { get; set; }

        // -- Acquisition Flags --
        [ParameterDescription("Whether a work bench can make it.")]
        public bool CanCraft { get; set; }
        [ParameterDescription("Whether traders buy or sell it.")]
        public bool CanTrade { get; set; }
        [ParameterDescription("Whether a plant yields it when harvested.")]
        public bool CanHarvest { get; set; } // Plants
        [ParameterDescription("Whether a miner digs it out.")]
        public bool CanMine { get; set; }    // Ores
        [ParameterDescription("Whether butchering a corpse yields it.")]
        public bool CanButcher { get; set; } // Meat/Leather

        // -- Detailed Sources --
        [ParameterDescription("The recipes that produce it at a work bench.")]
        public List<string> CraftingRecipes { get; set; } // e.g. "Smelt metal from slag"
        [ParameterDescription("The plants that yield it when harvested.")]
        public List<string> HarvestedFrom { get; set; }   // e.g. "Oak tree", "Corn plant"
        [ParameterDescription("The rocks that yield it when mined.")]
        public List<string> MinedFrom { get; set; }       // e.g. "Compacted machinery"
        [ParameterDescription("The trade tags that decide who deals in it.")]
        public List<string> TradeTags { get; set; }       // e.g. "ExoticMisc", "ResourcesRaw"
    }
}