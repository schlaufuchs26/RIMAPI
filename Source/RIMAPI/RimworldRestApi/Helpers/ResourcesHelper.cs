using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Models;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace RIMAPI.Helpers
{
    public static class ResourcesHelper
    {
        public static ResourcesSummaryDto GenerateResourcesSummary(Map map)
        {
            try
            {
                var allItems = map
                    .listerThings.AllThings.Where(t => t.def.EverStorable(false))
                    .ToList();

                var summary = new ResourcesSummaryDto
                {
                    TotalItems = allItems.Count,
                    TotalMarketValue = allItems.Sum(t => t.MarketValue * t.stackCount),
                    LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                };

                summary.Categories = GetResourcesCategorizedList(map);
                summary.CriticalResources = AnalyzeCriticalResources(map, allItems);
                return summary;
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error generating resources summary: {ex}");
            }
            return new ResourcesSummaryDto();
        }

        public static CriticalResourcesDto AnalyzeCriticalResources(Map map, List<Thing> allItems)
        {
            var critical = new CriticalResourcesDto();

            // Food analysis
            critical.FoodSummary = GetResourcesFoodSummary(map, allItems);

            // Medicine analysis
            var medicineItems = allItems.Where(t => t.def.IsMedicine).ToList();
            critical.MedicineTotal = medicineItems.Sum(t => t.stackCount);

            // Weapon analysis
            var weapons = allItems.Where(t => t.def.IsWeapon).ToList();
            critical.WeaponCount = weapons.Count;
            critical.WeaponValue = weapons.Sum(t => t.MarketValue);

            // Fuel analysis (fox ticket #3678): the wood in the stockpile and
            // every tank that burns it.
            critical.FuelSummary = GetFuelSummary(map, allItems);

            return critical;
        }

        // WoodLogDef is the defName every colony burns first: a fueled stove,
        // a fueled generator and a torch all take it, and it is the cheapest
        // building material. It is named even at zero.
        public const string WoodLogDef = "WoodLog";

        // GetFuelSummary answers the two halves of the fuel question: how much
        // fuel lies on the map, and which tanks are empty. A fueled building
        // with an empty tank works on nothing and says so in no alert, which
        // is how a colony cooked nothing for a game day while its bills read
        // "running" (fox ticket #3678).
        public static FuelSummaryDto GetFuelSummary(Map map, List<Thing> allItems)
        {
            var summary = new FuelSummaryDto();
            var fuelDefs = new List<string> { WoodLogDef };
            var seen = new HashSet<string> { WoodLogDef };

            foreach (var building in map.listerBuildings.allBuildingsColonist)
            {
                var comp = building.TryGetComp<CompRefuelable>();
                if (comp == null)
                    continue;

                string fuelType = comp.Props?.fuelFilter?.AnyAllowedDef?.defName;
                if (!string.IsNullOrEmpty(fuelType) && seen.Add(fuelType))
                    fuelDefs.Add(fuelType);

                int bills = 0;
                int mealBills = 0;
                var giver = building as IBillGiver;
                if (giver != null)
                {
                    foreach (var bill in giver.BillStack)
                    {
                        bills++;
                        // A stove's bills make meals; a generator's do not, so
                        // the recipe is what tells "cooking has stopped" from
                        // "a lamp is dark" (Recipes_Meals.xml, CookMeal*).
                        if (bill.recipe?.defName != null && bill.recipe.defName.StartsWith("CookMeal"))
                            mealBills++;
                    }
                }

                // An empty tank is only news when nobody is on the way to
                // refill it (fox ticket #3731). The game's own check is
                // WorkGiver_Refuel's, so a tank the colony is already fetching
                // wood for reads as covered, and a tank the game refuses to
                // refuel carries the game's reason.
                bool refuelAvailable = false;
                bool refuelForced = false;
                string refuelBlocked = null;
                if (!comp.HasFuel)
                    ReadRefuelVerdict(map, building, out refuelAvailable, out refuelForced, out refuelBlocked);

                summary.Buildings.Add(new FuelBuildingDto
                {
                    Id = building.thingIDNumber,
                    DefName = building.def?.defName,
                    Label = building.Label,
                    Fuel = comp.Fuel,
                    FuelCapacity = comp.Props?.fuelCapacity ?? 0f,
                    FuelType = fuelType,
                    HasFuel = comp.HasFuel,
                    RefuelAvailable = refuelAvailable,
                    RefuelForced = refuelForced,
                    RefuelBlocked = refuelBlocked,
                    Bills = bills,
                    MealBills = mealBills,
                });
            }

            // Sorted by id so two reads of the same colony agree.
            summary.Buildings = summary.Buildings.OrderBy(b => b.Id).ToList();

            foreach (var defName in fuelDefs)
            {
                int count = allItems
                    .Where(t => t.def?.defName == defName)
                    .Sum(t => t.stackCount);
                var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                summary.Stocks.Add(new FuelStockDto
                {
                    DefName = defName,
                    Label = def?.label ?? defName,
                    Count = count,
                });
            }

            return summary;
        }

        // ReadRefuelVerdict answers the two questions a digest asks about an
        // empty tank, with the game's own gate: RefuelWorkGiverUtility.CanRefuel
        // is what WorkGiver_Refuel asks before it hands out the job (fox ticket
        // #3731).
        //
        //   auto   - the colony refuels this by itself: nothing to do.
        //   forced - a colonist would refuel it if told to (the game's
        //            right-click entry), so the tank's auto-refuel is off.
        //   blocked- neither, and the game said why, e.g.
        //            "No fuel to refuel (wood)".
        //
        // A building the game neither refuels nor explains is one whose tank
        // needs no fuel at all (a passive cooler reports a refuelable comp with
        // a target of zero, so it is full at zero): nothing to report.
        //
        // CanRefuel writes its refusal into JobFailReason, a process-wide
        // string the UI shows; the previous value is put back so a read does
        // not leave a stale reason behind.
        private static void ReadRefuelVerdict(Map map, Thing building, out bool auto, out bool forced, out string blocked)
        {
            auto = false;
            forced = false;
            blocked = null;
            string previousReason = JobFailReason.Reason;
            try
            {
                foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (!auto && RefuelWorkGiverUtility.CanRefuel(pawn, building, false))
                        auto = true;
                    // A tank the colony refuels by itself is refuelable when
                    // told to as well; the forced check is the wider gate.
                    if (!forced && (auto || RefuelWorkGiverUtility.CanRefuel(pawn, building, true)))
                        forced = true;
                    if (blocked == null && !string.IsNullOrEmpty(JobFailReason.Reason))
                        blocked = JobFailReason.Reason;
                }
            }
            finally
            {
                if (previousReason == null)
                    JobFailReason.Clear();
                else
                    JobFailReason.Is(previousReason);
            }
        }

        public static ResourcesFoodSummaryDto GetResourcesFoodSummary(Map map, List<Thing> allItems)
        {
            var foodItems = allItems.Where(t => t.def.IsNutritionGivingIngestible).ToList();
            foodItems.AddRange(GetFoodHeldInContainers(map));

            // Every count below is in units, so meals + raw equals food_total and
            // a stack of ten meals reads as ten. The old code filled a second DTO
            // and returned the first, so meals and raw always read 0 while food
            // sat in storage (#3202).
            return new ResourcesFoodSummaryDto
            {
                FoodTotal = foodItems.Sum(t => t.stackCount),
                TotalNutrition = map.resourceCounter.TotalHumanEdibleNutrition,
                MealsCount = foodItems
                    .Where(t => t.def.ingestible.IsMeal)
                    .Sum(t => t.stackCount),
                RawFoodCount = foodItems
                    .Where(t => !t.def.ingestible.IsMeal)
                    .Sum(t => t.stackCount),
                RotStatusInfo = GetFoodRotStatusInfo(map, foodItems),
            };
        }

        // Food on a shelf sits in the shelf's inner container instead of being
        // spawned, so listerThings never sees it and a colony with a full shelf
        // would still read "meals 0". A corpse in a grave is skipped: the colony
        // cannot eat it, and graves would otherwise inflate the food line.
        public static List<Thing> GetFoodHeldInContainers(Map map)
        {
            var food = new List<Thing>();
            foreach (var building in map.listerBuildings.allBuildingsColonist)
            {
                var holder = building as IThingHolder;
                if (holder == null)
                    continue;
                foreach (var thing in holder.GetDirectlyHeldThings())
                {
                    if (thing.def.IsNutritionGivingIngestible && !(thing is Corpse))
                        food.Add(thing);
                }
            }
            return food;
        }

        public static RotStatusInfoDto GetFoodRotStatusInfo(Map map, List<Thing> allItems)
        {
            var dto = new RotStatusInfoDto();

            try
            {
                float foodRotatingSoon = 0f;
                float foodNotRotating = 0f;
                var soonRottingItems = new List<RottingFoodItemDto>();

                foreach (Thing thing in allItems)
                {
                    if (thing.def.IsNutritionGivingIngestible && !(thing is Corpse))
                    {
                        float daysUntilRot = 600f; // Infinite by default
                        CompRottable rottable = thing.TryGetComp<CompRottable>();

                        if (rottable != null && rottable.Active)
                        {
                            daysUntilRot =
                                (float)
                                    DaysUntilRotCalculator.ApproxTicksUntilRot_AssumeTimePassesBy(
                                        rottable,
                                        MapHelper.GetMapTileId(map)
                                    ) / 60000f;
                        }

                        float nutrition =
                            thing.GetStatValue(StatDefOf.Nutrition) * thing.stackCount;

                        if (daysUntilRot < 1f) // Less than 1 day to rot
                        {
                            foodRotatingSoon += nutrition;

                            // Add to soon rotting items list
                            soonRottingItems.Add(
                                new RottingFoodItemDto
                                {
                                    ThingId = thing.thingIDNumber,
                                    DefName = thing.def.defName,
                                    Label = thing.Label,
                                    StackCount = thing.stackCount,
                                    Nutrition = nutrition,
                                    DaysUntilRot = daysUntilRot,
                                    HoursUntilRot = daysUntilRot * 24f,
                                }
                            );
                        }
                        else
                        {
                            foodNotRotating += nutrition;
                        }
                    }
                }

                dto.NutritionRotatingSoon = foodRotatingSoon;
                dto.NutritionNotRotating = foodNotRotating;
                dto.PercentageRotatingSoon =
                    (foodRotatingSoon + foodNotRotating) > 0
                        ? foodRotatingSoon / (foodRotatingSoon + foodNotRotating)
                        : 0f;
                dto.SoonRottingItems = soonRottingItems.OrderBy(x => x.DaysUntilRot).ToList();
                dto.TotalSoonRottingItems = soonRottingItems.Count;
                dto.TotalSoonRottingStacks = soonRottingItems.Sum(x => x.StackCount);

                return dto;
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error getting food rot status: {ex}");
                return dto;
            }
        }

        public static float GetFoodNutrition(Thing food)
        {
            CompRottable comp = food.TryGetComp<CompRottable>();

            if (comp != null && comp.PropsRot.daysToRotStart > 0 && food.def.ingestible.HumanEdible)
            {
                return food.stackCount * food.def.GetStatValueAbstract(StatDefOf.Nutrition);
            }
            return 0f;
        }

        public static StoragesSummaryDto StoragesSummary(Map map)
        {
            var analysis = new StoragesSummaryDto();

            try
            {
                var stockpiles = map.zoneManager.AllZones.OfType<Zone_Stockpile>().ToList();
                analysis.TotalStockpiles = stockpiles.Count;
                analysis.TotalCells = stockpiles.Sum(z => z.CellCount);
                analysis.UsedCells = stockpiles.Sum(z =>
                    z.Cells.Count(c => c.GetItemCount(map) > 0)
                );

                if (analysis.TotalCells > 0)
                {
                    analysis.UtilizationPercent = (int)(
                        (double)analysis.UsedCells / analysis.TotalCells * 100
                    );
                }
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error analyzing storage: {ex}");
            }

            return analysis;
        }

        public static List<ResourceCategoryDto> GetResourcesCategorizedList(Map map)
        {
            try
            {
                var categoryData = new Dictionary<ThingCategoryDef, (int count, double value)>();

                var rootCategories = DefDatabase<ThingCategoryDef>.AllDefs.Where(cat =>
                    cat.resourceReadoutRoot
                );

                foreach (var category in rootCategories)
                {
                    int count = map.resourceCounter.GetCountIn(category);
                    double marketValue = 0.0;

                    foreach (var kvp in map.resourceCounter.AllCountedAmounts)
                    {
                        ThingDef thingDef = kvp.Key;
                        int thingCount = kvp.Value;

                        if (thingCount > 0 && category.ContainedInThisOrDescendant(thingDef))
                        {
                            marketValue += thingDef.BaseMarketValue * thingCount;
                        }
                    }

                    if (count > 0)
                    {
                        categoryData[category] = (count, marketValue);
                    }
                }

                return categoryData
                    .Select(kvp => new ResourceCategoryDto
                    {
                        Category = kvp.Key.LabelCap,
                        Count = kvp.Value.count,
                        MarketValue = kvp.Value.value,
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error analyzing storage: {ex}");
            }
            return new List<ResourceCategoryDto>();
        }

        public static List<Thing> GetItemsFromStorageLocations(Map map)
        {
            List<Thing> things = new List<Thing>();

            // The sources below overlap: an item on a shelf keeps its position at
            // the shelf's cell, so a shelf standing inside a stockpile zone is
            // listed by the shelf's slot group and again by
            // Zone_Stockpile.AllContainedThings. Adding both double-counts the
            // stack (ticket #3232), so each thing id is taken once.
            HashSet<int> seen = new HashSet<int>();
            int duplicates = 0;

            void Collect(IEnumerable<Thing> source)
            {
                foreach (Thing thing in source)
                {
                    if (thing == null)
                        continue;
                    if (seen.Add(thing.thingIDNumber))
                        things.Add(thing);
                    else
                        duplicates++;
                }
            }

            List<Building> allBuildings = map.listerBuildings.allBuildingsColonist;
            LogApi.Info("allBuildings: " + allBuildings.Count);
            for (int i = 0; i < allBuildings.Count; i++)
            {
                // Add items from ISlotGroupParent
                ISlotGroupParent storage = allBuildings[i] as ISlotGroupParent;
                if (storage != null)
                {
                    var slotGroup = storage.GetSlotGroup();
                    if (slotGroup == null)
                        continue;
                    Core.LogApi.Info($"add count: {slotGroup.HeldThings.Count()}");
                    Collect(slotGroup.HeldThings);
                    continue;
                }

                // Add items from Bookcases
                IThingHolder bookcase = allBuildings[i] as IThingHolder;
                if (bookcase != null)
                {
                    Collect(bookcase.GetDirectlyHeldThings());
                    continue;
                }
            }

            List<Zone> allZones = map.zoneManager.AllZones;
            LogApi.Info("allZones: " + allZones.Count);
            for (int i = 0; i < allZones.Count; i++)
            {
                Zone_Stockpile stockpile = allZones[i] as Zone_Stockpile;
                if (stockpile != null)
                {
                    Collect(stockpile.AllContainedThings);
                    LogApi.Info(
                        "stockpile.AllContainedThings: " + stockpile.AllContainedThings.Count()
                    );
                }
            }
            if (duplicates > 0)
                LogApi.Info(
                    $"storage items: {things.Count} unique, {duplicates} duplicate listing(s) skipped"
                );
            return things;
        }

        public static Dictionary<string, List<ThingDto>> GetStoredItemsByCategory(List<Thing> items)
        {
            var itemsByCategory = new Dictionary<string, List<ThingDto>>();
            LogApi.Info("Items: " + items.Count);

            foreach (var thing in items)
            {
                var category = GetPrimaryCategoryForThing(thing.def);
                if (category == null)
                    continue;

                var categoryLabel = category.defName;

                // Get or create the list for this category
                if (!itemsByCategory.TryGetValue(categoryLabel, out var categoryList))
                {
                    categoryList = new List<ThingDto>();
                    itemsByCategory[categoryLabel] = categoryList;
                }

                categoryList.Add(ThingToDto(thing));
            }

            return itemsByCategory;
        }

        public static List<ThingDto> GetStoredItemsListByCategory(
            List<Thing> items,
            string categoryDef
        )
        {
            var itemsByCategory = new List<ThingDto>();

            foreach (var thing in items)
            {
                if (!IsThingInCategory(thing, categoryDef))
                    continue;

                var dto = ThingToDto(thing);
                itemsByCategory.Add(dto);
            }

            return itemsByCategory;
        }

        public static bool IsThingInCategory(Thing thing, string categoryDef)
        {
            var categories = thing.def.thingCategories;
            return categories != null
                && categories.Any(c => c.defName == TransformToPascalCase(categoryDef));
        }

        public static ThingDto ThingToDto(Thing thing)
        {
            var dto = new ThingDto
            {
                ThingId = thing.thingIDNumber,
                DefName = thing.def.defName,
                Label = thing.Label,
                Categories =
                    thing.def.thingCategories?.Select(c => c.defName).ToList()
                    ?? new List<string>(),
                Position = new PositionDto
                {
                    X = thing.Position.x,
                    Y = thing.Position.y,
                    Z = thing.Position.z,
                },
                Rotation = thing.Rotation.AsInt,
                Size = new PositionDto
                {
                    X = thing.def.size.x,
                    Y = 0,
                    Z = thing.def.size.z
                },
                StackCount = thing.stackCount,
                MarketValue = thing.MarketValue,
                IsForbidden = thing.IsForbidden(Faction.OfPlayer),
            };

            dto.Quality = -1;
            if (thing.TryGetQuality(out QualityCategory quality))
            {
                dto.Quality = (int)quality;
            }

            // Set hit points if applicable
            if (thing.def.useHitPoints)
            {
                dto.HitPoints = thing.HitPoints;
                dto.MaxHitPoints = thing.MaxHitPoints;
            }

            return dto;
        }

        public static ThingCategoryDef GetPrimaryCategoryForThing(ThingDef thingDef)
        {
            // Get the first (most specific) category from thingCategories list
            if (thingDef.thingCategories != null && thingDef.thingCategories.Count > 0)
            {
                return thingDef.thingCategories[0];
            }
            return null;
        }

        public static string TransformToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(input.Replace('_', ' ')).Replace(" ", "");
        }
    }
}
