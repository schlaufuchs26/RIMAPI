using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RIMAPI.Models
{
    public class ThingDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string ThingClass { get; set; }
        public Dictionary<string, float> StatBase { get; set; }
        public List<ThingCostDto> CostList { get; set; }
        public bool IsWeapon { get; set; }
        public bool IsApparel { get; set; }
        public bool IsItem { get; set; }
        public bool IsPawn { get; set; }
        public bool IsPlant { get; set; }
        public bool IsBuilding { get; set; }
        public bool IsMedicine { get; set; }
        public bool IsDrug { get; set; }
        public float MarketValue { get; set; }
        public float Mass { get; set; }
        public float MaxHitPoints { get; set; }
        public float Flammability { get; set; }
        public int StackLimit { get; set; }
        public float Nutrition { get; set; }
        public float WorkToMake { get; set; }
        public float WorkToBuild { get; set; }
        public float Beauty { get; set; }
        public string TechLevel { get; set; }
        public List<string> TradeTags { get; set; }
        public List<string> StuffCategories { get; set; }
        public float ArmorRatingSharp { get; set; }
        public float ArmorRatingBlunt { get; set; }
        public float ArmorRatingHeat { get; set; }
        public float InsulationCold { get; set; }
        public float InsulationHeat { get; set; }

        // StuffStats holds the same armor and insulation numbers once per
        // material the def accepts, and only for apparel (ticket #3615): an
        // apparel's protection is mostly its material's, so the abstract value
        // above answers a stuffable def with the game's 0.
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
        public string StuffDefName { get; set; }
        public string StuffLabel { get; set; }
        public float ArmorRatingSharp { get; set; }
        public float ArmorRatingBlunt { get; set; }
        public float ArmorRatingHeat { get; set; }
        public float InsulationCold { get; set; }
        public float InsulationHeat { get; set; }
    }

    public class SpawnItemRequestDto
    {
        public string defName;      // "Gun_Revolver"
        public string stuffDefName; // "Steel" (Optional, can be null)
        public string quality;      // "Normal", "Good", "Legendary" (Optional)
        public int amount;
        public int x;
        public int z;
    }
}
