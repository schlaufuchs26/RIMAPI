using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class ThingDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The thing category the def belongs to.")]
        public string Category { get; set; }
        [ParameterDescription("The C# class the thing runs.")]
        public string ThingClass { get; set; }
        [ParameterDescription("The base values of the def's stats.")]
        public Dictionary<string, float> StatBase { get; set; }
        [ParameterDescription("What the thing costs to build.")]
        public List<ThingCostDto> CostList { get; set; }
        [ParameterDescription("Whether the def is a weapon.")]
        public bool IsWeapon { get; set; }
        [ParameterDescription("Whether it is apparel.")]
        public bool IsApparel { get; set; }
        [ParameterDescription("Whether it is an item.")]
        public bool IsItem { get; set; }
        [ParameterDescription("Whether it is a pawn.")]
        public bool IsPawn { get; set; }
        [ParameterDescription("Whether it is a plant.")]
        public bool IsPlant { get; set; }
        [ParameterDescription("Whether it is a building.")]
        public bool IsBuilding { get; set; }
        [ParameterDescription("Whether it is medicine.")]
        public bool IsMedicine { get; set; }
        [ParameterDescription("Whether it is a drug.")]
        public bool IsDrug { get; set; }
        [ParameterDescription("What one unit is worth in silver.")]
        public float MarketValue { get; set; }
        [ParameterDescription("How much one unit weighs, in kilograms.")]
        public float Mass { get; set; }
        [ParameterDescription("How much damage one unit takes.")]
        public float MaxHitPoints { get; set; }
        [ParameterDescription("How easily it catches fire, 0 to 1.")]
        public float Flammability { get; set; }
        [ParameterDescription("How many units one stack holds.")]
        public int StackLimit { get; set; }
        [ParameterDescription("How much nutrition one unit holds.")]
        public float Nutrition { get; set; }
        [ParameterDescription("How much work making one takes.")]
        public float WorkToMake { get; set; }
        [ParameterDescription("How much work building one takes.")]
        public float WorkToBuild { get; set; }
        [ParameterDescription("How much beauty it adds to its room.")]
        public float Beauty { get; set; }
        [ParameterDescription("The tech level the def belongs to.")]
        public string TechLevel { get; set; }
        [ParameterDescription("The trade tags that decide who deals in it.")]
        public List<string> TradeTags { get; set; }
        [ParameterDescription("The material categories it can be built from.")]
        public List<string> StuffCategories { get; set; }
        [ParameterDescription("How much sharp damage it stops, 0 to 1.")]
        public float ArmorRatingSharp { get; set; }
        [ParameterDescription("How much blunt damage it stops, 0 to 1.")]
        public float ArmorRatingBlunt { get; set; }
        [ParameterDescription("How much heat damage it stops, 0 to 1.")]
        public float ArmorRatingHeat { get; set; }
        [ParameterDescription("How much cold it keeps out, in degrees.")]
        public float InsulationCold { get; set; }
        [ParameterDescription("How much heat it keeps out, in degrees.")]
        public float InsulationHeat { get; set; }

        // StuffStats holds the same armor and insulation numbers once per
        // material the def accepts, and only for apparel (ticket #3615): an
        // apparel's protection is mostly its material's, so the abstract value
        // above answers a stuffable def with the game's 0.
        [ParameterDescription("StuffStats holds the same armor and insulation numbers once per material the def accepts, and only for apparel (ticket #3615): an apparel's protection is mostly its material's, so the abstract...")]
        public List<StuffStatDto> StuffStats { get; set; }

        public static ThingDefDto ToDto(ThingDef thing)
        {
            return new ThingDefDto
            {
                DefName = thing.defName,
                Label = thing.label,
                Description = thing.description,
                Category = thing.category.ToString(),
                ThingClass = thing.thingClass?.Name,
                StatBase = thing.statBases?.ToDictionary(s => s.stat?.defName, s => s.value),
                CostList = thing
                                    .costList?.Select(c => new ThingCostDto
                                    {
                                        ThingDef = c.thingDef?.defName,
                                        Count = c.count,
                                    })
                                    .ToList(),
                IsWeapon = thing.IsWeapon,
                IsApparel = thing.IsApparel,
                IsItem = thing.category == ThingCategory.Item,
                IsPawn = thing.category == ThingCategory.Pawn,
                IsPlant = thing.category == ThingCategory.Plant,
                IsBuilding = thing.category == ThingCategory.Building,
                IsMedicine = thing.IsMedicine,
                IsDrug = thing.IsDrug,
                MarketValue = thing.BaseMarketValue,
                Mass = thing.GetStatValueAbstract(StatDefOf.Mass),
                MaxHitPoints = thing.GetStatValueAbstract(StatDefOf.MaxHitPoints),
                Flammability = thing.GetStatValueAbstract(StatDefOf.Flammability),
                StackLimit = thing.stackLimit,
                Nutrition = thing.GetStatValueAbstract(StatDefOf.Nutrition),
                WorkToMake = thing.GetStatValueAbstract(StatDefOf.WorkToMake),
                WorkToBuild = thing.GetStatValueAbstract(StatDefOf.WorkToBuild),
                Beauty = thing.GetStatValueAbstract(StatDefOf.Beauty),
                TechLevel = thing.techLevel.ToString(),
                TradeTags = thing.tradeTags?.ToList(),
                StuffCategories = thing.stuffCategories?.Select(c => c.defName).ToList(),
                // The armor and insulation stats resolve the def's statBases
                // against its default material, so an apparel that sets none
                // answers the game's 0 instead of an unset field (ticket #3442).
                ArmorRatingSharp = thing.GetStatValueAbstract(StatDefOf.ArmorRating_Sharp),
                ArmorRatingBlunt = thing.GetStatValueAbstract(StatDefOf.ArmorRating_Blunt),
                ArmorRatingHeat = thing.GetStatValueAbstract(StatDefOf.ArmorRating_Heat),
                InsulationCold = thing.GetStatValueAbstract(StatDefOf.Insulation_Cold),
                InsulationHeat = thing.GetStatValueAbstract(StatDefOf.Insulation_Heat),
                StuffStats = StuffStatsFor(thing),
            };
        }

        /// <summary>
        /// The armor and insulation for every material the def accepts, so a
        /// reader can tell a wool parka from a cloth one (ticket #3615).
        /// </summary>
        /// <remarks>
        /// The material's share of these two stats is a StatPart_Stuff
        /// (Insulation_Cold takes the material's StuffPower_Insulation_Cold
        /// times the def's StuffEffectMultiplierInsulation_Cold), so the value
        /// has to come from the stat worker with the material in the request:
        /// the abstract value has no material and reports 0 for a stuffable
        /// apparel. Only apparel is answered per material, because apparel is
        /// what a run chooses between and a list on every stuffable building
        /// would grow a catalog that is already 2.6 MB.
        /// </remarks>
        private static List<StuffStatDto> StuffStatsFor(ThingDef thing)
        {
            if (!thing.IsApparel || thing.stuffCategories == null || thing.stuffCategories.Count == 0)
                return null;

            var stats = new List<StuffStatDto>();
            var stuffs = DefDatabase<ThingDef>
                .AllDefsListForReading.Where(d =>
                    d != null && d.IsStuff && d.stuffProps != null && d.stuffProps.CanMake(thing)
                )
                .OrderBy(d => d.label);

            foreach (ThingDef stuff in stuffs)
            {
                StatRequest req = StatRequest.For(thing, stuff);
                stats.Add(
                    new StuffStatDto
                    {
                        StuffDefName = stuff.defName,
                        StuffLabel = stuff.label,
                        ArmorRatingSharp = StatDefOf.ArmorRating_Sharp.Worker.GetValue(req),
                        ArmorRatingBlunt = StatDefOf.ArmorRating_Blunt.Worker.GetValue(req),
                        ArmorRatingHeat = StatDefOf.ArmorRating_Heat.Worker.GetValue(req),
                        InsulationCold = StatDefOf.Insulation_Cold.Worker.GetValue(req),
                        InsulationHeat = StatDefOf.Insulation_Heat.Worker.GetValue(req),
                    }
                );
            }
            return stats;
        }
    }

    /// <summary>
    /// One material's answer for a stuffable apparel (ticket #3615): what a
    /// tuque of cloth and a tuque of sheep wool actually give their wearer.
    /// </summary>
    public class StuffStatDto
    {
        [ParameterDescription("The material's def name.")]
        public string StuffDefName { get; set; }
        [ParameterDescription("The material's label, as the game shows it.")]
        public string StuffLabel { get; set; }
        [ParameterDescription("How much sharp armour the material gives.")]
        public float ArmorRatingSharp { get; set; }
        [ParameterDescription("How much blunt armour the material gives.")]
        public float ArmorRatingBlunt { get; set; }
        [ParameterDescription("How much heat armour the material gives.")]
        public float ArmorRatingHeat { get; set; }
        [ParameterDescription("How much cold the material keeps out.")]
        public float InsulationCold { get; set; }
        [ParameterDescription("How much heat the material keeps out.")]
        public float InsulationHeat { get; set; }
    }

    public class SpawnItemRequestDto
    {
        [ParameterDescription("The def name of the item to spawn, e.g. \"Gun_Revolver\".")]
        public string DefName { get; set; }

        [ParameterDescription("The material to build it from, e.g. \"Steel\"; empty for a def without one.")]
        public string StuffDefName { get; set; }

        [ParameterDescription("The quality to give it: Awful through Legendary; empty leaves the game's default.")]
        public string Quality { get; set; }

        [ParameterDescription("How many units to spawn, capped at the def's stack limit.")]
        public int Amount { get; set; }

        public int X { get; set; }

        public int Z { get; set; }
    }
}
