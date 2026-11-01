using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RIMAPI.Helpers
{
    public static class MapHelper
    {
        public static Thing GetThingOnMapById(int mapId, int id)
        {
            Map map = GetMapByID(mapId);
            return map.listerThings.AllThings.Where(s => s.thingIDNumber == id).FirstOrDefault();
        }

        public static Map GetMapByID(int uniqueID)
        {
            foreach (Map map in Find.Maps)
            {
                if (map.uniqueID == uniqueID)
                {
                    return map;
                }
            }
            return null;
        }

        public static List<MapDto> GetMaps()
        {
            var maps = new List<MapDto>();

            try
            {
                foreach (var map in Current.Game.Maps)
                {
                    maps.Add(
                        new MapDto
                        {
                            Id = map.uniqueID,
                            Index = map.Index,
                            Seed = map.ConstantRandSeed,
                            FactionId = map.ParentFaction.loadID.ToString(),
                            IsPlayerHome = map.IsPlayerHome,
                            IsPocketMap = map.IsPocketMap,
                            IsTempIncidentMap = map.IsTempIncidentMap,
                            Size = map.Size.ToString(),
                        }
                    );
                }

                return maps;
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error - {ex}");
                return maps;
            }
        }

        public static MapCreaturesSummaryDto GetMapCreaturesSummary(int mapId)
        {
            try
            {
                var map = GetMapByID(mapId);
                return new MapCreaturesSummaryDto
                {
                    ColonistsCount = map.mapPawns.FreeColonistsSpawnedCount,
                    PrisonersCount = map.mapPawns.PrisonersOfColonyCount,
                    // The same rule the combat read uses, so a manhunter
                    // animal or a mechanoid raid is a hostile here too
                    // (ticket #3451): counting humanlikes only made a warg
                    // raid read "0 hostiles" and the digest stayed silent.
                    EnemiesCount = map.mapPawns.AllPawnsSpawned.Count(Hostility.IsHostileToColony),
                    AnimalsCount = map.mapPawns.AllPawnsSpawned.Count(p => p.RaceProps.Animal),
                    InsectoidsCount = map.mapPawns.AllPawnsSpawned.Count(p =>
                        p != null && p.Faction != null && p.Faction.def == FactionDefOf.Insect
                    ),
                    MechanoidsCount = map.mapPawns.AllPawnsSpawned.Count(p =>
                        p != null && p.RaceProps != null && p.RaceProps.IsMechanoid
                    ),
                    Attackers = AttackersOf(map),
                };
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error - {ex}");
                return new MapCreaturesSummaryDto();
            }
        }

        // AttackersOf names the hostiles whose current job acts on a colony pawn
        // (fox ticket #3690). The count alone left a run guessing: DiLeo the wild
        // man beat Veniamin while the digest said "1 hostile on the map" and the
        // fight list called him factionless.
        public static List<CreatureAttackerDto> AttackersOf(Map map)
        {
            var attackers = new List<CreatureAttackerDto>();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned) continue;
                if (!Hostility.IsHostileToColony(pawn)) continue;
                Pawn victim = Hostility.AttackedColonist(pawn);
                if (victim == null) continue;
                attackers.Add(new CreatureAttackerDto
                {
                    Id = pawn.thingIDNumber,
                    Name = pawn.LabelShort,
                    Kind = Hostility.KindOf(pawn),
                    TargetId = victim.thingIDNumber,
                    Target = victim.LabelShort,
                });
            }
            return attackers;
        }

        public static MapTimeDto GetDatetimeAt(int tileID)
        {
            MapTimeDto mapTimeDto = new MapTimeDto();
            try
            {
                if (Current.ProgramState != ProgramState.Playing || Find.WorldGrid == null)
                {
                    return mapTimeDto;
                }

                var vector = Find.WorldGrid.LongLatOf(GetMapTileId(Find.CurrentMap));
                mapTimeDto.Datetime = GenDate.DateFullStringWithHourAt(
                    Find.TickManager.TicksAbs,
                    vector
                );

                return mapTimeDto;
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error - {ex}");
                return mapTimeDto;
            }
        }

        public static MapPowerInfoDto GetMapPowerInfoInternal(int mapId)
        {
            MapPowerInfoDto powerInfo = new MapPowerInfoDto();

            try
            {
                Map map = GetMapByID(mapId);

                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    // Check if building is - Power Generator
                    CompPowerPlant powerPlant = building.TryGetComp<CompPowerPlant>();
                    if (powerPlant != null)
                    {
                        powerInfo.TotalPossiblePower += Mathf.RoundToInt(
                            Mathf.Abs(powerPlant.Props.PowerConsumption)
                        );
                        powerInfo.CurrentPower += Mathf.RoundToInt(powerPlant.PowerOutput);
                        powerInfo.ProducePowerBuildings.Add(building.thingIDNumber);
                        continue;
                    }

                    // Check if building is - Battery
                    CompPowerBattery powerBattery = building.TryGetComp<CompPowerBattery>();
                    if (powerBattery != null)
                    {
                        powerInfo.CurrentlyStoredPower += Mathf.RoundToInt(
                            powerBattery.StoredEnergy
                        );
                        powerInfo.TotalPowerStorage += Mathf.RoundToInt(
                            powerBattery.Props.storedEnergyMax
                        );
                        powerInfo.StorePowerBuildings.Add(building.thingIDNumber);
                    }
                }

                // Calculate power consumption
                foreach (PowerNet net in map.powerNetManager.AllNetsListForReading)
                {
                    foreach (CompPowerTrader comp in net.powerComps)
                    {
                        if (comp.Props.PowerConsumption > 0f)
                        {
                            powerInfo.TotalConsumption += Mathf.RoundToInt(
                                comp.Props.PowerConsumption
                            );
                        }
                        if (comp.PowerOn && comp.PowerOutput < 0f)
                        {
                            powerInfo.ConsumptionPowerOn += Mathf.RoundToInt(
                                Mathf.Abs(comp.PowerOutput)
                            );
                        }

                        Building building = comp.parent as Building;
                        if (building != null)
                        {
                            powerInfo.ConsumePowerBuildings.Add(building.thingIDNumber);
                        }
                    }
                }

                // The rows are gathered from the built buildings, not from the
                // nets, so a machine no conduit reaches appears here with its
                // own switch and the distance to the nearest conduit (fox
                // ticket #3762).
                powerInfo.Buildings = PowerGrid.Buildings(map);

                // Every figure above is the last tick's, so the answer carries
                // the tick it came from: -1 while the colony has not ticked
                // since it loaded (fox ticket #3781).
                powerInfo.LastNetTick = PowerTick.Last(map.uniqueID);

                return powerInfo;
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error - {ex}");
                return powerInfo;
            }
        }

        public static int GetMapTileId(Map map)
        {
            return map.Tile.tileId;
        }

        public static List<AnimalDto> GetMapAnimals(int mapId)
        {
            List<AnimalDto> animals = new List<AnimalDto>();
            try
            {
                Map map = GetMapByID(mapId);
                if (map == null)
                {
                    return animals;
                }

                animals = map
                    .mapPawns.AllPawns.Where(p => p.RaceProps?.Animal == true)
                    // A carried animal is despawned into its carrier's hands:
                    // the spawned-only filter dropped it and a rescue looked
                    // like a death (fox #3668).
                    .Where(p => p != null && AnimalRules.Listed(p.Spawned, p.CarriedBy != null, p.Dead, p.Destroyed))
                    .Select(p => AnimalDtoOf(map, p))
                    .ToList();

                // The order a run decides on first: the herd that attacks on
                // sight, then the wild animals it could tame or hunt, then its
                // own (fox ticket #3467).
                AnimalRules.Sort(animals);

                return animals;
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error - {ex}");
                return new List<AnimalDto>();
            }
        }

        private static AnimalDto AnimalDtoOf(Map map, Pawn p)
        {
            // A carried animal keeps the cell its carrier picked it up at; where
            // it is now is the carrier's cell, and the destination is what the
            // carrier's job heads for (fox #3668).
            Pawn carrier = p.CarriedBy;
            IntVec3 cell = carrier != null ? carrier.Position : p.Position;
            LocalTargetInfo target = carrier?.CurJob != null
                ? carrier.CurJob.targetB
                : LocalTargetInfo.Invalid;

            return new AnimalDto
            {
                Id = p.thingIDNumber,
                Name = p.LabelShortCap,
                Def = p.def?.defName,
                Label = p.LabelShort,
                Faction = p.Faction?.ToString(),
                Side = AnimalRules.Side(p.Faction == Faction.OfPlayer, p.Faction?.Name),
                Sex = p.gender.ToString().ToLowerInvariant(),
                LifeStage = p.ageTracker?.CurLifeStage?.label,
                Health = p.health?.summaryHealth?.SummaryHealthPercent ?? 1f,
                Downed = p.Downed,
                Tameable = TameUtility.CanTame(p),
                TameDesignated = map.designationManager.DesignationOn(p, DesignationDefOf.Tame) != null,
                HuntDesignated = map.designationManager.DesignationOn(p, DesignationDefOf.Hunt) != null,
                Manhunter = Hostility.IsManhunter(p),
                Predator = p.RaceProps.predator,
                QuestLodger = p.IsQuestLodger(),
                // X, Y and Z as the map means them: Y is the altitude (0 for a
                // pawn on the ground) and Z the second ground coordinate. The
                // old line put the map's z into Y, so every animal cell read
                // "157,0" and a run could not build at the animal's position
                // (fox ticket #3519).
                Position = new PositionDto { X = cell.x, Y = cell.y, Z = cell.z },
                CarriedBy = carrier?.LabelShort ?? "",
                CarriedTo = AnimalRules.DestinationOf(
                    target.Thing?.LabelShort,
                    target.IsValid && target.Cell.IsValid,
                    target.Cell.x,
                    target.Cell.z),
                Trainer = p
                    .relations?.DirectRelations.Where(r => r.def == PawnRelationDefOf.Bond)
                    .Select(r => r.otherPawn?.thingIDNumber)
                    .FirstOrDefault(),
                Pregnant = p.health?.hediffSet?.HasHediff(HediffDefOf.Pregnant) ?? false,
            };
        }

        public static List<ThingDto> GetMapThings(int mapId)
        {
            List<ThingDto> things = new List<ThingDto>();
            try
            {
                Map map = GetMapByID(mapId);
                if (map == null)
                {
                    return things;
                }

                things = map
                    .listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver)
                    .Select(p => ResourcesHelper.ThingToDto(p))
                    .ToList();

                return things;
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error - {ex}");
                return new List<ThingDto>();
            }
        }

        public static List<ThingDto> GetMapPlants(int mapId)
        {
            List<ThingDto> plants = new List<ThingDto>();
            try
            {
                Map map = GetMapByID(mapId);
                if (map == null)
                {
                    return plants;
                }

                // Get all plants (trees, bushes, crops, etc.)
                plants = map
                    .listerThings.ThingsInGroup(ThingRequestGroup.Plant)
                    .Select(p => ResourcesHelper.ThingToDto(p))
                    .ToList();

                return plants;
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error - {ex}");
                return new List<ThingDto>();
            }
        }

        public static List<ZoneDto> GetMapZones(int mapId)
        {
            List<ZoneDto> zones = new List<ZoneDto>();
            try
            {
                Map map = GetMapByID(mapId);
                if (map == null)
                {
                    throw new Exception("Map with this id wasn't found");
                }

                foreach (Zone zone in map.zoneManager.AllZones)
                {
                    Zone_Stockpile stockpile = zone as Zone_Stockpile;
                    zones.Add(
                        new ZoneDto
                        {
                            Id = zone.ID,
                            CellsCount = zone.CellCount,
                            Label = zone.label,
                            BaseLabel = zone.BaseLabel,
                            Type = zone.GetType().Name,
                            Storage = stockpile == null ? null : GetStockpileFilter(stockpile),
                            Growing = GetZoneGrowing(zone as Zone_Growing, map),
                        }
                    );
                }

                return zones;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // GetZoneGrowing counts one growing zone's cells: which of them carry a
        // plant, which still need sowing, and how many of the plants are dying,
        // blighted or ready (fox ticket #3277). The cell counts are what tells
        // "nothing was ever sown here" from "it was sown and died" from "the
        // sowing work exists and nobody walks there".
        //
        // HarvestableCells is HarvestableNow, which starts at the plant's own
        // harvestMinGrowth (0.65 for rice), while the game's growing-zone
        // harvest (WorkGiver_GrowerHarvest) takes a plant only at
        // PlantLifeStage.Mature, growth above 0.999 (fox ticket #3396). A field
        // of rice at 0.73 therefore read "19 ready" and nobody touched it, so
        // the zone also reports the mature count and the ripest plant.
        public static ZoneGrowingDto GetZoneGrowing(Zone_Growing zone, Map map)
        {
            if (zone == null)
            {
                return null;
            }

            ThingDef plantDef = zone.GetPlantDefToGrow();
            ZoneGrowingDto dto = new ZoneGrowingDto
            {
                PlantDefName = plantDef?.defName ?? "None",
                PlantLabel = plantDef?.label,
                AllowSow = zone.allowSow,
            };

            foreach (IntVec3 cell in zone.Cells)
            {
                Plant plant = cell.GetPlant(map);
                if (plant == null)
                {
                    dto.UnsownCells++;
                    continue;
                }

                dto.SownCells++;
                if (plant.Dying)
                {
                    dto.DyingCells++;
                }
                if (plant.Blighted)
                {
                    dto.BlightedCells++;
                }
                if (plant.HarvestableNow)
                {
                    dto.HarvestableCells++;
                }
                // Mature is the game's own stage, not a growth threshold of
                // ours: the zone harvest asks plant.LifeStage == Mature. Both
                // this count and the ripest growth stay on the zone's own crop
                // (fox ticket #3677): a wild grass in the field is mature, so
                // the field read "1 fully grown" and "ripest 100 %" while every
                // rice plant stood at 94 %.
                if (plantDef == null || plant.def == plantDef)
                {
                    if (plant.LifeStage == PlantLifeStage.Mature)
                    {
                        dto.MatureCells++;
                    }
                    if (plant.Growth > dto.MaxGrowth)
                    {
                        dto.MaxGrowth = plant.Growth;
                    }
                }
            }

            // InSeason is the game's own growing-season verdict for the plant
            // the zone is set to: an unsown field in winter is not a broken
            // zone, and the count alone would read as one.
            if (plantDef != null && zone.CellCount > 0)
            {
                dto.InSeason = PlantUtility.GrowthSeasonNow(map, plantDef);
            }
            return dto;
        }

        // GetMapPlantsInRect lists the plants standing on one rectangle, which
        // is the field-sized answer `map/plants` cannot give: that read returns
        // every plant on the map and a full map is 6.5 MB.
        //
        // The plants come from the cell grid, one GetPlant per cell, and not
        // from listerThings' plant group filtered by a CellRect: that filter
        // answered nothing for a rectangle holding the colony's own potato
        // field, and nothing for a rectangle spanning the whole map, while
        // `map/plants` counted 15,691 plants from the same group in the same
        // game (fox ticket #3404). The growing-zone read asks each cell with
        // GetPlant and has counted the same field all along, so that is the
        // lookup this read uses too.
        public static List<PlantDto> GetMapPlantsInRect(
            int mapId,
            int x1,
            int z1,
            int x2,
            int z2
        )
        {
            Map map = GetMapByID(mapId);
            if (map == null)
            {
                throw new Exception("Map with this id wasn't found");
            }

            int minX = Math.Min(x1, x2);
            int minZ = Math.Min(z1, z2);
            int maxX = Math.Max(x1, x2);
            int maxZ = Math.Max(z1, z2);
            CellRect rect = new CellRect(minX, minZ, maxX - minX + 1, maxZ - minZ + 1);

            List<PlantDto> plants = new List<PlantDto>();
            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                Plant plant = cell.GetPlant(map);
                if (plant == null)
                {
                    continue;
                }
                plants.Add(
                    new PlantDto
                    {
                        ThingId = plant.thingIDNumber,
                        DefName = plant.def?.defName,
                        Label = plant.LabelShort,
                        X = cell.x,
                        Z = cell.z,
                        Growth = plant.Growth,
                        Harvestable = plant.HarvestableNow,
                        Dying = plant.Dying,
                        Blighted = plant.Blighted,
                    }
                );
            }
            return plants.OrderBy(p => p.Z).ThenBy(p => p.X).ToList();
        }

        // GetStockpileFilter describes one stockpile's filter the way a player
        // reads it off the storage tab (ticket #3181): the priority, and whether
        // it takes food, raw food and meals. The filter is a def list, so the
        // question is asked of the game's own def database: does it allow any def
        // that gives nutrition, any meal, any raw food.
        public static StockpileFilterDto GetStockpileFilter(Zone_Stockpile stockpile)
        {
            ThingFilter filter = stockpile.settings.filter;
            return new StockpileFilterDto
            {
                Priority = stockpile.settings.Priority.ToString(),
                AcceptsFood = FilterAllowsAny(filter, IsFoodDef),
                AcceptsMeals = FilterAllowsAny(filter, IsMealDef),
                AcceptsRawFood = FilterAllowsAny(filter, def => IsFoodDef(def) && !IsMealDef(def)),
                AllowedDefCount = filter.AllowedThingDefs.Count(),
                AllowedFoodCategories = FoodCategoriesAllowed(filter),
                AllowedMealDefs = AllowedDefsWith(filter, IsMealDef),
                AllowedRawDefs = AllowedDefsWith(filter, def => IsFoodDef(def) && !IsMealDef(def)),
            };
        }

        // FoodCategoryNames is the storage tab's food subtree in the tab's own
        // order, the five names the tool's refusal text offers (fox ticket #3231).
        private static readonly string[] FoodCategoryNames =
        {
            "Foods",
            "FoodMeals",
            "FoodRaw",
            "PlantFoodRaw",
            "MeatRaw",
        };

        // FoodCategoriesAllowed answers the question a player actually clicks
        // (fox ticket #3231): which of the food checkboxes the filter still
        // allows a def of. The food-type flags above are the game's own
        // def-level question, and the two disagree wherever a def sits outside
        // the category its food type suggests.
        private static List<string> FoodCategoriesAllowed(ThingFilter filter)
        {
            var allowed = new List<string>();
            foreach (string name in FoodCategoryNames)
            {
                ThingCategoryDef category = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(name);
                if (category == null)
                {
                    continue;
                }
                foreach (ThingDef def in category.DescendantThingDefs)
                {
                    if (filter.Allows(def))
                    {
                        allowed.Add(name);
                        break;
                    }
                }
            }
            return allowed;
        }

        // AllowedDefsWith names the allowed defs that satisfy predicate, sorted.
        // It answers what keeps a food-type flag true when the category a player
        // denied says otherwise: a meal comes in through Pemmican or BabyFood and
        // not through FoodMeals (fox ticket #3231), raw food through Chocolate,
        // HemogenPack or Kibble and not through FoodRaw (fox ticket #3303).
        private static List<string> AllowedDefsWith(ThingFilter filter, Func<ThingDef, bool> predicate)
        {
            var names = new List<string>();
            foreach (ThingDef def in filter.AllowedThingDefs)
            {
                if (predicate(def))
                {
                    names.Add(def.defName);
                }
            }
            names.Sort(StringComparer.Ordinal);
            return names;
        }

        // GetUnwantedFood lists the edible things that lie on the map with no
        // storage that takes them (ticket #3181): a colony starved next to 267
        // rice because no stockpile's filter allowed RawRice, and neither the
        // stock nor the zone read could say it. The filter alone is only half
        // the answer (ticket #3207): a shelf whose filter takes rice but whose
        // cells are full also leaves the rice on the ground, and the two states
        // need different fixes, so they come back in separate lists.
        public static UnwantedFoodDto GetUnwantedFood(int mapId)
        {
            Map map = GetMapByID(mapId);
            if (map == null)
            {
                throw new Exception("Map with this id wasn't found");
            }

            List<ThingFilter> filters = StorageFilters(map);
            Pawn hauler = FindHauler(map);
            var accepted = new Dictionary<string, bool>();
            var things = new List<ThingDto>();
            var noRoom = new List<ThingDto>();
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver))
            {
                if (thing == null || !thing.Spawned || !IsFoodDef(thing.def))
                {
                    continue;
                }
                if (IsStoredWhereItBelongs(map, thing))
                {
                    continue;
                }
                if (!AcceptedByAny(filters, thing.def, accepted))
                {
                    things.Add(ResourcesHelper.ThingToDto(thing));
                    continue;
                }
                if (hauler != null && !HasHaulDestination(hauler, thing))
                {
                    noRoom.Add(ResourcesHelper.ThingToDto(thing));
                }
            }
            things.Sort((a, b) => a.ThingId.CompareTo(b.ThingId));
            noRoom.Sort((a, b) => a.ThingId.CompareTo(b.ThingId));
            return new UnwantedFoodDto
            {
                Things = things,
                NoRoomThings = noRoom,
                StorageCount = filters.Count,
                CapacityChecked = hauler != null,
            };
        }

        // IsStoredWhereItBelongs says whether the thing already lies in a
        // storage whose filter takes it: such food is not rotting on the
        // ground, and a full shelf must not report the rice it holds.
        private static bool IsStoredWhereItBelongs(Map map, Thing thing)
        {
            SlotGroup group = map.haulDestinationManager.SlotGroupAt(thing.Position);
            if (group == null || group.Settings == null || group.Settings.filter == null)
            {
                return false;
            }
            return group.Settings.filter.Allows(thing);
        }

        // FindHauler is the pawn the capacity question needs: the game's own
        // store search takes a carrier. One colonist that can haul is enough,
        // and the search never moves him. Null means the question cannot be
        // asked right now, which the caller reports instead of guessing.
        private static Pawn FindHauler(Map map)
        {
            return map.mapPawns.FreeColonists.FirstOrDefault(p =>
                p != null
                && !p.Downed
                && !p.Dead
                && p.jobs != null
                && p.Spawned
                && !p.WorkTypeIsDisabled(WorkTypeDefOf.Hauling)
            );
        }

        // HasHaulDestination asks the game's own store search whether a hauler
        // could put the thing anywhere: it honours every filter, the free
        // cells, stacking, reservations and the carrier's reach. A null job is
        // the game saying "no destination", which is what makes the food rot.
        private static bool HasHaulDestination(Pawn hauler, Thing thing)
        {
            try
            {
                return HaulAIUtility.HaulToStorageJob(hauler, thing, forced: true) != null;
            }
            catch (Exception ex)
            {
                Core.LogApi.Error(
                    $"Error - haul destination for thing {thing.thingIDNumber}: {ex.Message}"
                );
                return true;
            }
        }

        // StorageFilters collects the filters of everything that could take a
        // hauled thing: the stockpiles and the storage buildings (a shelf, a
        // grave). Capacity is not asked, only the filter, which is the question
        // this read answers.
        private static List<ThingFilter> StorageFilters(Map map)
        {
            var filters = new List<ThingFilter>();
            foreach (Zone zone in map.zoneManager.AllZones)
            {
                Zone_Stockpile stockpile = zone as Zone_Stockpile;
                if (stockpile != null && stockpile.settings.filter != null)
                {
                    filters.Add(stockpile.settings.filter);
                }
            }
            foreach (Building_Storage storage in map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>())
            {
                if (storage.settings != null && storage.settings.filter != null)
                {
                    filters.Add(storage.settings.filter);
                }
            }
            return filters;
        }

        private static bool AcceptedByAny(
            List<ThingFilter> filters,
            ThingDef def,
            Dictionary<string, bool> cache
        )
        {
            if (cache.TryGetValue(def.defName, out bool known))
            {
                return known;
            }
            bool accepted = filters.Any(f => f.Allows(def));
            cache[def.defName] = accepted;
            return accepted;
        }

        private static bool FilterAllowsAny(ThingFilter filter, Func<ThingDef, bool> predicate)
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (predicate(def) && filter.Allows(def))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsFoodDef(ThingDef def) => def != null && def.IsNutritionGivingIngestible;

        private static bool IsMealDef(ThingDef def) =>
            IsFoodDef(def)
            && def.ingestible != null
            && def.ingestible.foodType.HasFlag(FoodTypeFlags.Meal);

        public static List<ZoneDto> GetMapAreas(int mapId)
        {
            List<ZoneDto> zones = new List<ZoneDto>();
            try
            {
                Map map = GetMapByID(mapId);
                if (map == null)
                {
                    throw new Exception("Map with this id wasn't found");
                }

                foreach (Area area in map.areaManager.AllAreas)
                {
                    zones.Add(
                        new ZoneDto
                        {
                            Id = area.ID,
                            CellsCount = area.ActiveCells.Count(),
                            Label = area.Label,
                            BaseLabel = area.Label,
                            Type = area.GetType().Name,
                        }
                    );
                }

                return zones;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Find one area by its label or its type name, Home when nothing is
        /// named (fox ticket #3095). The game translates the label, so a German
        /// install answers "Zuhause" for the home area; the type name
        /// Area_Home does not move, so both are accepted.
        /// </summary>
        public static Area FindArea(Map map, string label, out string error)
        {
            error = null;
            if (map == null)
            {
                error = "No map is loaded.";
                return null;
            }

            string wanted = string.IsNullOrWhiteSpace(label) ? "Home" : label.Trim();
            var areas = map.areaManager.AllAreas;
            foreach (var area in areas)
            {
                string type = area.GetType().Name;
                if (string.Equals(area.Label.ToString(), wanted, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(type, wanted, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(type, "Area_" + wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return area;
                }
            }

            error = $"No area is named \"{wanted}\". The map has: " +
                string.Join(", ", areas.Select(a => a.Label + " (" + a.GetType().Name + ")"));
            return null;
        }

        /// <summary>
        /// One area's cells as rectangles (fox ticket #3095). The area read
        /// listed only a count, so a caller could not tell whether a cell was
        /// inside the Home area, which is where colonists haul and therefore
        /// where a blueprint is ever built.
        /// </summary>
        public static AreaCellsDto GetAreaCells(Map map, Area area)
        {
            var cells = new HashSet<IntVec3>(area.ActiveCells);
            var dto = new AreaCellsDto
            {
                Id = area.ID,
                Label = area.Label,
                Type = area.GetType().Name,
                CellsCount = cells.Count,
                Rects = AreaRectangles(cells),
            };
            if (dto.Rects.Count > 0)
            {
                dto.Bounds = new AreaRectDto
                {
                    X1 = dto.Rects.Min(r => r.X1),
                    Z1 = dto.Rects.Min(r => r.Z1),
                    X2 = dto.Rects.Max(r => r.X2),
                    Z2 = dto.Rects.Max(r => r.Z2),
                };
            }
            return dto;
        }

        /// <summary>
        /// Compress a set of cells into non-overlapping rectangles: horizontal
        /// runs per row, merged downward while the run stays identical. A home
        /// area is usually a few hundred to a few thousand cells, and a list of
        /// rectangles is what a caller can test a cell against.
        /// </summary>
        public static List<AreaRectDto> AreaRectangles(HashSet<IntVec3> cells)
        {
            var rects = new List<AreaRectDto>();
            if (cells.Count == 0)
            {
                return rects;
            }

            // Runs still open at the previous row, keyed by their x span.
            var open = new Dictionary<(int, int), AreaRectDto>();
            var rows = cells.GroupBy(c => c.z).OrderBy(g => g.Key);
            foreach (var row in rows)
            {
                var next = new Dictionary<(int, int), AreaRectDto>();
                foreach (var run in RowRuns(row.Select(c => c.x)))
                {
                    if (open.TryGetValue(run, out AreaRectDto rect) && rect.Z2 == row.Key - 1)
                    {
                        rect.Z2 = row.Key;
                    }
                    else
                    {
                        rect = new AreaRectDto { X1 = run.Item1, X2 = run.Item2, Z1 = row.Key, Z2 = row.Key };
                        rects.Add(rect);
                    }
                    next[run] = rect;
                }
                open = next;
            }

            rects.Sort((a, b) => a.Z1 != b.Z1 ? a.Z1.CompareTo(b.Z1) : a.X1.CompareTo(b.X1));
            return rects;
        }

        /// <summary>The contiguous x spans of one row, sorted.</summary>
        private static List<(int, int)> RowRuns(IEnumerable<int> xs)
        {
            var sorted = xs.OrderBy(x => x).ToList();
            var runs = new List<(int, int)>();
            if (sorted.Count == 0)
            {
                return runs;
            }
            int start = sorted[0];
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] != sorted[i - 1] + 1)
                {
                    runs.Add((start, sorted[i - 1]));
                    start = sorted[i];
                }
            }
            runs.Add((start, sorted[sorted.Count - 1]));
            return runs;
        }

        /// <summary>
        /// Turn every cell of the rectangle on in the area, the game's own
        /// "expand area" designator applied at once (fox ticket #3095). Cells
        /// outside the map are counted, not set, so the answer can say why a
        /// rectangle did not land whole.
        /// </summary>
        public static AreaCellsDto ExpandArea(Map map, Area area, CellRect rect)
        {
            int added = 0, already = 0, outside = 0;
            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                {
                    outside++;
                    continue;
                }
                if (area[cell])
                {
                    already++;
                    continue;
                }
                area[cell] = true;
                added++;
            }

            AreaCellsDto dto = GetAreaCells(map, area);
            dto.Added = added;
            dto.Already = already;
            dto.Outside = outside;
            return dto;
        }

        public static List<BuildingDto> GetMapBuildings(int mapId)
        {
            List<BuildingDto> buildings = new List<BuildingDto>();
            Map map = GetMapByID(mapId);
            if (map == null)
            {
                throw new Exception("Map with this id wasn't found");
            }

            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                buildings.Add(
                    new BuildingDto
                    {
                        Id = building.thingIDNumber,
                        Def = building.def.defName,
                        Label = building.Label,
                        Position = new PositionDto
                        {
                            X = building.Position.x,
                            Y = building.Position.y,
                            Z = building.Position.z,
                        },
                        Rotation = building.Rotation.AsInt,
                        Size = new PositionDto
                        {
                            X = building.def.size.x,
                            Y = 0,
                            Z = building.def.size.z
                        },
                        Type = building.GetType().Name,
                        Bed = BuildingHelper.BedDtoOf(building),
                        // A grave's assignment and contents, the same block
                        // BuildingHelper.BuildingToDto fills (fox ticket
                        // #3784): this list is the one `things` reads.
                        Grave = BuildingHelper.GraveDtoOf(building),
                    }
                );
            }

            return buildings;
        }

        public static MapRoomsDto GetRooms(Map map)
        {
            var mapRooms = new MapRoomsDto();
            var allRooms = map.regionGrid.AllRooms;
            mapRooms = new MapRoomsDto
            {
                Rooms = allRooms.Select(s => RoomDtoOf(s, map)).ToList(),
            };
            return mapRooms;
        }

        /// <summary>
        /// One room as the read reports it. The contained pawns are the
        /// humanlike ones standing inside (fox ticket #3225): the read carries
        /// no cell list, so a bed was the only occupancy signal, and a room
        /// with a colonist and no bed was invisible to a caller.
        /// </summary>
        private static RoomDto RoomDtoOf(Room room, Map map)
        {
            return new RoomDto
            {
                Id = room.ID,
                RoleLabel = room.GetRoomRoleLabel(),
                Temperature = room.Temperature,
                CellsCount = room.CellCount,
                TouchesMapEdge = room.TouchesMapEdge,
                IsPrisonCell = room.IsPrisonCell,
                IsDoorway = room.IsDoorway,
                ContainedBedsIds = room.ContainedBeds.Select(b => b.thingIDNumber).ToList(),
                ContainedPawnsIds = map.mapPawns.AllPawnsSpawned
                    .Where(p => p.RaceProps.Humanlike && p.GetRoom() == room)
                    .Select(p => p.thingIDNumber)
                    .ToList(),
                OpenRoofCount = room.OpenRoofCount,
            };
        }

        // GetMapTerrain reads the natural terrain and the constructed floors as
        // a run-length grid. A rectangle narrows the answer to that box (fox
        // ticket #3381): the whole map is about 30 kB even encoded, while a
        // fire read needs the ground under a handful of cells.
        public static MapTerrainDto GetMapTerrain(int mapId, TerrainRect.Box? box = null)
        {
            var map = GetMapByID(mapId);
            if (map == null) return new MapTerrainDto();

            var terrainGrid = map.terrainGrid;
            var size = map.Size;

            TerrainRect.Box area;
            if (box.HasValue)
            {
                if (
                    !TerrainRect.Clip(
                        size.x,
                        size.z,
                        box.Value.X1,
                        box.Value.Z1,
                        box.Value.X2,
                        box.Value.Z2,
                        out area
                    )
                )
                {
                    throw new Exception(
                        $"The rectangle {box.Value} misses the map, which is {size.x}x{size.z} cells"
                    );
                }
            }
            else
            {
                area = new TerrainRect.Box { X1 = 0, Z1 = 0, X2 = size.x - 1, Z2 = size.z - 1 };
            }

            int width = area.Width;
            int height = area.Height;

            // 1. Build Palette and Raw Index Grid
            var palette = new List<string>();
            var paletteLookup = new Dictionary<TerrainDef, int>();
            var rawIndices = new int[width * height];

            int cellIndex = 0;
            // Iterate Z then X (Standard loop order), row-major inside the box
            for (int z = area.Z1; z <= area.Z2; z++)
            {
                for (int x = area.X1; x <= area.X2; x++)
                {
                    TerrainDef def = terrainGrid.TerrainAt(new IntVec3(x, 0, z));

                    if (!paletteLookup.TryGetValue(def, out int pIndex))
                    {
                        pIndex = palette.Count;
                        palette.Add(def.defName);
                        paletteLookup[def] = pIndex;
                    }

                    rawIndices[cellIndex++] = pIndex;
                }
            }

            // 2. Run-Length Encoding (RLE)
            var compressedGrid = new List<int>();
            if (rawIndices.Length > 0)
            {
                int currentVal = rawIndices[0];
                int count = 1;

                for (int i = 1; i < rawIndices.Length; i++)
                {
                    if (rawIndices[i] == currentVal)
                    {
                        count++;
                    }
                    else
                    {
                        compressedGrid.Add(count);
                        compressedGrid.Add(currentVal);
                        currentVal = rawIndices[i];
                        count = 1;
                    }
                }
                // Add final run
                compressedGrid.Add(count);
                compressedGrid.Add(currentVal);
            }

            // 3. Build Floor Palette and Grid (for constructed floors)
            var floorPalette = new List<string>();
            var floorPaletteLookup = new Dictionary<string, int>();
            var rawFloorIndices = new int[width * height];

            cellIndex = 0;
            for (int z = area.Z1; z <= area.Z2; z++)
            {
                for (int x = area.X1; x <= area.X2; x++)
                {
                    // The edifice grid is the map's own array, indexed by the
                    // whole map's row length, not by the box.
                    var building = map.edificeGrid.InnerArray[z * size.x + x];

                    // Check if this is a floor (constructed floor blueprint)
                    // Floors in RimWorld are Buildings that have a graphic but no altitude (they're on the ground)
                    string floorDefName = null;
                    if (building != null && building.def.building != null)
                    {
                        // Floors typically have graphicData and no altitudeLayer set
                        // Or check if the defName contains "Floor"
                        if (building.def.graphicData != null && building.def.altitudeLayer == Verse.AltitudeLayer.Floor)
                        {
                            floorDefName = building.def.defName;
                        }
                    }

                    int fIndex = 0; // 0 = no floor (null)
                    if (floorDefName != null)
                    {
                        if (!floorPaletteLookup.TryGetValue(floorDefName, out fIndex))
                        {
                            fIndex = floorPalette.Count + 1; // +1 because 0 is reserved for null
                            floorPalette.Add(floorDefName);
                            floorPaletteLookup[floorDefName] = fIndex;
                        }
                    }

                    rawFloorIndices[cellIndex++] = fIndex;
                }
            }

            // 4. Run-Length Encoding for Floors
            var compressedFloorGrid = new List<int>();
            if (rawFloorIndices.Length > 0)
            {
                int currentVal = rawFloorIndices[0];
                int count = 1;

                for (int i = 1; i < rawFloorIndices.Length; i++)
                {
                    if (rawFloorIndices[i] == currentVal)
                    {
                        count++;
                    }
                    else
                    {
                        compressedFloorGrid.Add(count);
                        compressedFloorGrid.Add(currentVal);
                        currentVal = rawFloorIndices[i];
                        count = 1;
                    }
                }
                // Add final run
                compressedFloorGrid.Add(count);
                compressedFloorGrid.Add(currentVal);
            }

            return new MapTerrainDto
            {
                X1 = area.X1,
                Z1 = area.Z1,
                Width = width,
                Height = height,
                Palette = palette,
                Grid = compressedGrid,
                FloorPalette = floorPalette,
                FloorGrid = compressedFloorGrid
            };
        }

        public static List<ThingDto> GetMapThingsInRadius(int mapId, int centerX, int centerZ, int radius)
        {
            var results = new List<ThingDto>();
            Map map = GetMapByID(mapId);
            if (map == null) return results;

            IntVec3 center = new IntVec3(centerX, 0, centerZ);
            var processedIds = new HashSet<int>();

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map)) continue;
                List<Thing> thingsAtCell = map.thingGrid.ThingsListAt(cell);

                for (int i = 0; i < thingsAtCell.Count; i++)
                {
                    Thing t = thingsAtCell[i];
                    if (processedIds.Contains(t.thingIDNumber)) continue;

                    // Filter: Items, Buildings, Plants
                    if (t.def.category == ThingCategory.Item ||
                        t.def.category == ThingCategory.Building ||
                        t.def.category == ThingCategory.Plant)
                    {
                        // Skip invisible things
                        if (t.def.drawerType == DrawerType.None) continue;

                        // Convert to DTO
                        // Use existing helper but ensure we capture building-specifics if needed
                        var dto = ResourcesHelper.ThingToDto(t);

                        // Correction for Buildings: Size/Rotation logic in ThingToDto is generic
                        // Ensure it matches what we need

                        results.Add(dto);
                        processedIds.Add(t.thingIDNumber);
                    }
                }
            }
            return results;
        }

        public static OreDataDto GetOreData(int mapId)
        {
            Map map = MapHelper.GetMapByID(mapId);
            if (map == null) return null;

            int width = map.Size.x;

            var oreData = new OreDataDto
            {
                MapWidth = width,
                Ores = new Dictionary<string, OreGroupDto>()
            };

            // Use a single loop through AllThings to populate the groups
            // This is faster than LINQ GroupBy for large datasets in Unity/RimWorld
            foreach (var thing in map.listerThings.AllThings)
            {
                if (thing.def.mineable)
                {
                    string defName = thing.def.defName;

                    // Get or Create the Group for this ore type
                    if (!oreData.Ores.TryGetValue(defName, out OreGroupDto group))
                    {
                        group = new OreGroupDto
                        {
                            MaxHp = thing.MaxHitPoints,
                            Cells = new List<int>(),
                            Hp = new List<int>()
                        };
                        oreData.Ores[defName] = group;
                    }

                    // Flatten Position (X, Z) into a single integer Index
                    // Formula: index = (z * width) + x
                    // We ignore Y because ores are always on the surface layer in RimWorld
                    int index = (thing.Position.z * width) + thing.Position.x;

                    group.Cells.Add(index);
                    group.Hp.Add(thing.HitPoints);
                }
            }

            return oreData;
        }

        public static StockpileResponseDto CreateStockpile(CreateStockpileRequestDto request)
        {
            try
            {
                var map = GetMapByID(request.MapId);
                if (map == null)
                {
                    return new StockpileResponseDto
                    {
                        Success = false,
                        Message = MapLookup.MapNotFound(request.MapId)
                    };
                }

                // Validate positions
                if (request.PointA == null || request.PointB == null)
                {
                    return new StockpileResponseDto
                    {
                        Success = false,
                        Message = "PointA and PointB cannot be null."
                    };
                }

                // Convert positions to IntVec3
                IntVec3 pointA = new IntVec3(request.PointA.X, request.PointA.Y, request.PointA.Z);
                IntVec3 pointB = new IntVec3(request.PointB.X, request.PointB.Y, request.PointB.Z);

                // Normalize rectangle (ensure min/max are in correct order)
                int minX = Mathf.Min(pointA.x, pointB.x);
                int maxX = Mathf.Max(pointA.x, pointB.x);
                int minZ = Mathf.Min(pointA.z, pointB.z);
                int maxZ = Mathf.Max(pointA.z, pointB.z);

                // Validate that rectangle has at least one cell
                if (minX > maxX || minZ > maxZ)
                {
                    return new StockpileResponseDto
                    {
                        Success = false,
                        Message = "Invalid rectangle: PointA and PointB form an invalid area."
                    };
                }

                // Create CellRect from normalized coordinates
                IntVec3 cellRectA = new IntVec3(minX, 0, minZ);
                IntVec3 cellRectB = new IntVec3(maxX, 0, maxZ);
                CellRect rect = CellRect.FromLimits(cellRectA, cellRectB);

                // Validate cells are in bounds
                if (!rect.All(cell => cell.InBounds(map)))
                {
                    return new StockpileResponseDto
                    {
                        Success = false,
                        Message = "Some cells in the specified rectangle are out of map bounds."
                    };
                }

                if (rect.Area == 0)
                {
                    return new StockpileResponseDto
                    {
                        Success = false,
                        Message = "No valid cells found in the specified rectangle."
                    };
                }

                // Create new stockpile zone
                Zone_Stockpile stockpile = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);

                // Set name BEFORE registering
                if (string.IsNullOrEmpty(request.Name))
                {
                    // Auto-generate name: "Stockpile #"
                    int stockpileCount = map.zoneManager.AllZones.OfType<Zone_Stockpile>().Count() + 1;
                    stockpile.label = $"Stockpile {stockpileCount}";
                }
                else
                {
                    stockpile.label = request.Name;
                }

                // Set priority BEFORE registering. Only a priority the caller
                // named overrides the preset (fox ticket #3206): the old
                // "request.Priority ?? 0" made every stockpile this endpoint
                // created "Unstored", a value the storage tab never offers, and
                // the clamp to 2 turned a requested Critical into Low.
                if (request.Priority.HasValue)
                {
                    int priorityValue = request.Priority.Value;
                    if (priorityValue < 0 || priorityValue > 5)
                    {
                        return new StockpileResponseDto
                        {
                            Success = false,
                            Message = $"Priority {priorityValue} is out of range: 0 unstored, 1 low, 2 normal, 3 preferred, 4 important, 5 critical."
                        };
                    }
                    stockpile.settings.Priority = (StoragePriority)priorityValue;
                }

                // Configure storage settings (filtration)
                bool hasItemDefs = request.AllowedItemDefs != null && request.AllowedItemDefs.Count > 0;
                bool hasCategories = request.AllowedItemCategories != null && request.AllowedItemCategories.Count > 0;
                var appliedDefs = new List<string>();
                var appliedCategories = new List<string>();
                var unknown = new List<string>();

                if (hasItemDefs || hasCategories)
                {
                    // Disallow all first: the preset allows everything, and a
                    // list of allowed names narrows it rather than adding to it.
                    foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
                    {
                        stockpile.settings.filter.SetAllow(thingDef, false);
                    }

                    ApplyFilterNames(stockpile.settings.filter, request.AllowedItemDefs, true, appliedDefs, appliedCategories, unknown);
                    ApplyFilterNames(stockpile.settings.filter, request.AllowedItemCategories, true, appliedDefs, appliedCategories, unknown);
                }
                else
                {
                    // Default allowance: allow all items that storage zones normally allow
                    foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
                    {
                        if (thingDef.category == ThingCategory.Item && 
                            !thingDef.IsCorpse && 
                            thingDef.alwaysHaulable)
                        {
                            stockpile.settings.filter.SetAllow(thingDef, true);
                        }
                    }
                }

                // Set hit points filter
                float minHpPercent = request.MinHitPointsPercent ?? 0.0f;
                float maxHpPercent = request.MaxHitPointsPercent ?? 1.0f;
                minHpPercent = Mathf.Clamp01(minHpPercent);
                maxHpPercent = Mathf.Clamp01(maxHpPercent);
                
                if (minHpPercent > 0.0f || maxHpPercent < 1.0f)
                {
                    stockpile.settings.filter.AllowedHitPointsPercents = new Verse.FloatRange(minHpPercent, maxHpPercent);
                    stockpile.settings.filter.allowedHitPointsConfigurable = true;
                }

                // Set quality filter
                if (!string.IsNullOrEmpty(request.MinQuality) || !string.IsNullOrEmpty(request.MaxQuality))
                {
                    QualityCategory minQuality = QualityCategory.Awful;
                    QualityCategory maxQuality = QualityCategory.Legendary;

                    if (!string.IsNullOrEmpty(request.MinQuality))
                    {
                        if (System.Enum.TryParse<QualityCategory>(request.MinQuality, out var parsedMin))
                        {
                            minQuality = parsedMin;
                        }
                    }

                    if (!string.IsNullOrEmpty(request.MaxQuality))
                    {
                        if (System.Enum.TryParse<QualityCategory>(request.MaxQuality, out var parsedMax))
                        {
                            maxQuality = parsedMax;
                        }
                    }

                    stockpile.settings.filter.AllowedQualityLevels = new QualityRange(minQuality, maxQuality);
                    stockpile.settings.filter.allowedQualitiesConfigurable = true;
                }

                // IMPORTANT: Register the zone BEFORE adding cells to avoid conflicts
                map.zoneManager.RegisterZone(stockpile);

                // NOW add all cells to the registered zone
                foreach (IntVec3 cell in rect)
                {
                    stockpile.AddCell(cell);
                }

                int cellCount = rect.Area;
                LogApi.Info($"Created stockpile '{stockpile.label}' with {cellCount} cells at priority {stockpile.settings.Priority}");

                return new StockpileResponseDto
                {
                    Success = true,
                    ZoneId = stockpile.ID,
                    Name = stockpile.label,
                    CellsCount = cellCount,
                    Priority = (int)stockpile.settings.Priority,
                    Message = $"Stockpile '{stockpile.label}' created successfully with {cellCount} cells.",
                    AppliedDefs = appliedDefs,
                    AppliedCategories = appliedCategories,
                    Unknown = unknown,
                    Filter = GetStockpileFilter(stockpile)
                };
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error creating stockpile: {ex}");
                return new StockpileResponseDto
                {
                    Success = false,
                    Message = $"Failed to create stockpile: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Helper method to find a stockpile zone by ID across all maps
        /// </summary>
        private static (Zone_Stockpile stockpile, Map map) FindStockpileById(int zoneId)
        {
            foreach (Map map in Find.Maps)
            {
                var zone = map.zoneManager.AllZones.OfType<Zone_Stockpile>()
                    .FirstOrDefault(z => z.ID == zoneId);
                
                if (zone != null)
                {
                    return (zone, map);
                }
            }
            return (null, null);
        }

        public static ApiResult DeleteStockpile(int zoneId)
        {
            try
            {
                var (stockpile, stockpileMap) = FindStockpileById(zoneId);

                if (stockpile == null)
                {
                    return ApiResult.Fail($"Stockpile with ID {zoneId} not found.");
                }

                string stockpileName = stockpile.label;
                int cellsCount = stockpile.CellCount;
                Map mapToCleanup = stockpileMap; // Capture for lambda
                Zone_Stockpile zoneToDelete = stockpile; // Capture for lambda

                // Execute on main thread to ensure proper cleanup
                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    try
                    {
                        if (mapToCleanup != null && zoneToDelete != null)
                        {
                            // Clear all cells from the zone using Cells list directly
                            var cellsList = zoneToDelete.Cells;
                            if (cellsList != null && cellsList.Count > 0)
                            {
                                // Copy cells to avoid collection modification during iteration
                                var cellsToRemove = cellsList.ToList();
                                foreach (var cell in cellsToRemove)
                                {
                                    zoneToDelete.RemoveCell(cell);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("haul destination"))
                        {
                            LogApi.Warning($"Stockpile deletion warning: {ex.Message}");
                        }
                    }
                });

                LogApi.Info($"Deleted stockpile '{stockpileName}' (ID: {zoneId}) with {cellsCount} cells");
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error deleting stockpile: {ex}");
                return ApiResult.Fail($"Failed to delete stockpile: {ex.Message}");
            }
        }

        /// <summary>
        /// Find any zone (stockpile, growing, dumping) by ID across all maps.
        /// </summary>
        private static (Zone zone, Map map) FindZoneById(int zoneId)
        {
            foreach (Map map in Find.Maps)
            {
                var zone = map.zoneManager.AllZones.FirstOrDefault(z => z.ID == zoneId);
                if (zone != null)
                {
                    return (zone, map);
                }
            }
            return (null, null);
        }

        /// <summary>
        /// Delete any zone by ID, whatever its type. The stockpile route above
        /// only clears a stockpile's cells and only knows stockpiles; a growing
        /// zone had no removal at all, so a field sown over the wrong rectangle
        /// stayed on the map and kept pulling work. Zone.Delete() is the game's
        /// own removal: it clears the cells and deregisters the zone, while the
        /// plants already growing on those cells stay where they are.
        /// </summary>
        public static ApiResult DeleteZone(int zoneId)
        {
            try
            {
                var found = FindZoneById(zoneId);
                Zone zone = found.zone;

                if (zone == null)
                {
                    return ApiResult.Fail($"Zone with ID {zoneId} not found.");
                }

                string label = zone.label;
                string type = zone.GetType().Name;
                int cellsCount = zone.CellCount;
                string failure = null;

                // Zone.Delete() touches the game's own state and plays its
                // deletion sound, so it runs on the main thread. The caller
                // waits for it: an answer that says "deleted" while the action
                // still sits in the queue is the kind of lie that costs a
                // colony its stores.
                using (var done = new System.Threading.ManualResetEventSlim(false))
                {
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        try
                        {
                            zone.Delete();
                        }
                        catch (Exception ex)
                        {
                            failure = ex.Message;
                        }
                        finally
                        {
                            done.Set();
                        }
                    });

                    if (!done.Wait(3000))
                    {
                        return ApiResult.Fail(
                            $"Removal of zone {zoneId} ('{label}') is queued but the game did not run it " +
                            "within 3 s (a loading screen or a modal holds the main thread). " +
                            "Read the zones again to see whether it went.");
                    }
                }

                if (failure != null)
                {
                    return ApiResult.Fail($"Failed to delete zone {zoneId} ('{label}'): {failure}");
                }

                LogApi.Info($"Deleted zone '{label}' ({type}, ID: {zoneId}) with {cellsCount} cells");
                return ApiResult.Ok(new
                {
                    ZoneId = zoneId,
                    Label = label,
                    Type = type,
                    CellsCount = cellsCount,
                });
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error deleting zone: {ex}");
                return ApiResult.Fail($"Failed to delete zone: {ex.Message}");
            }
        }

        // ApplyFilterNames sets the allow state of each name on a stockpile's
        // filter (fox ticket #3206). A name is a thing def first and a thing
        // category second, because both are what the storage tab lists
        // ("RawRice" is a def, "Foods" a category); an exact defName wins, and
        // a case-insensitive defName or label match is the fallback. A name
        // that matches neither goes into unknown, which the answer names
        // instead of reporting a filter change that never happened.
        private static void ApplyFilterNames(
            ThingFilter filter,
            List<string> names,
            bool allow,
            List<string> appliedDefs,
            List<string> appliedCategories,
            List<string> unknown
        )
        {
            if (names == null)
            {
                return;
            }
            foreach (string raw in names)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }
                string name = raw.Trim();

                ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (thingDef == null)
                {
                    thingDef = DefDatabase<ThingDef>.AllDefsListForReading.FirstOrDefault(d =>
                        string.Equals(d.defName, name, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(d.label, name, StringComparison.OrdinalIgnoreCase)
                    );
                }
                if (thingDef != null)
                {
                    filter.SetAllow(thingDef, allow);
                    if (!appliedDefs.Contains(thingDef.defName))
                    {
                        appliedDefs.Add(thingDef.defName);
                    }
                    continue;
                }

                ThingCategoryDef categoryDef = DefDatabase<ThingCategoryDef>.GetNamedSilentFail(name);
                if (categoryDef == null)
                {
                    categoryDef = DefDatabase<ThingCategoryDef>.AllDefsListForReading.FirstOrDefault(d =>
                        string.Equals(d.defName, name, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(d.label, name, StringComparison.OrdinalIgnoreCase)
                    );
                }
                if (categoryDef != null)
                {
                    // The game's own category setter, the call the storage tab
                    // makes: it walks the descendant defs and the category's
                    // special filters, which a plain def loop would miss.
                    filter.SetAllow(categoryDef, allow);
                    if (!appliedCategories.Contains(categoryDef.defName))
                    {
                        appliedCategories.Add(categoryDef.defName);
                    }
                    continue;
                }

                unknown.Add(name);
            }
        }

        public static ApiResult<StockpileResponseDto> UpdateStockpile(UpdateStockpileRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return ApiResult<StockpileResponseDto>.Fail("Request cannot be null.");
                }

                var (stockpile, stockpileMap) = FindStockpileById(request.ZoneId);

                if (stockpile == null)
                {
                    return ApiResult<StockpileResponseDto>.Fail(
                        $"No stockpile with ID {request.ZoneId}: the `zones` read prints the stockpiles with their ids, and a growing zone has no filter."
                    );
                }

                // A filter write answers what it applied and what it could not
                // resolve (fox ticket #3206): a name the def database does not
                // know used to be dropped while the call still answered
                // success, so a stockpile the player believed widened took
                // nothing.
                var appliedDefs = new List<string>();
                var appliedCategories = new List<string>();
                var unknown = new List<string>();

                if (!string.IsNullOrEmpty(request.Name))
                {
                    stockpile.label = request.Name;
                }

                if (request.Priority.HasValue)
                {
                    int priorityValue = request.Priority.Value;
                    if (priorityValue < 0 || priorityValue > 5)
                    {
                        return ApiResult<StockpileResponseDto>.Fail(
                            $"Priority {priorityValue} is out of range: 0 unstored, 1 low, 2 normal, 3 preferred, 4 important, 5 critical."
                        );
                    }
                    stockpile.settings.Priority = (StoragePriority)priorityValue;
                }

                if (request.MinHitPointsPercent.HasValue || request.MaxHitPointsPercent.HasValue)
                {
                    float minHp = request.MinHitPointsPercent ?? stockpile.settings.filter.AllowedHitPointsPercents.min;
                    float maxHp = request.MaxHitPointsPercent ?? stockpile.settings.filter.AllowedHitPointsPercents.max;
                    
                    minHp = Mathf.Clamp01(minHp);
                    maxHp = Mathf.Clamp01(maxHp);
                    
                    stockpile.settings.filter.AllowedHitPointsPercents = new Verse.FloatRange(minHp, maxHp);
                    stockpile.settings.filter.allowedHitPointsConfigurable = true;
                }

                if (!string.IsNullOrEmpty(request.MinQuality) || !string.IsNullOrEmpty(request.MaxQuality))
                {
                    QualityCategory minQuality = stockpile.settings.filter.AllowedQualityLevels.min;
                    QualityCategory maxQuality = stockpile.settings.filter.AllowedQualityLevels.max;

                    if (!string.IsNullOrEmpty(request.MinQuality))
                    {
                        if (System.Enum.TryParse<QualityCategory>(request.MinQuality, out var parsedMin))
                        {
                            minQuality = parsedMin;
                        }
                    }

                    if (!string.IsNullOrEmpty(request.MaxQuality))
                    {
                        if (System.Enum.TryParse<QualityCategory>(request.MaxQuality, out var parsedMax))
                        {
                            maxQuality = parsedMax;
                        }
                    }

                    stockpile.settings.filter.AllowedQualityLevels = new QualityRange(minQuality, maxQuality);
                    stockpile.settings.filter.allowedQualitiesConfigurable = true;
                }

                ApplyFilterNames(stockpile.settings.filter, request.RemoveItemDefs, false, appliedDefs, appliedCategories, unknown);
                ApplyFilterNames(stockpile.settings.filter, request.AddItemDefs, true, appliedDefs, appliedCategories, unknown);
                ApplyFilterNames(stockpile.settings.filter, request.RemoveItemCategories, false, appliedDefs, appliedCategories, unknown);
                ApplyFilterNames(stockpile.settings.filter, request.AddItemCategories, true, appliedDefs, appliedCategories, unknown);

                // No ResolveReferences here: that is the load-time fixup. The
                // game's own setters already tell the zone its settings moved
                // (StorageSettings builds the filter with TryNotifyChanged, and
                // Zone_Stockpile.Notify_SettingsChanged marks the slot group
                // dirty in ListerHaulables), which is what makes a widened
                // filter a haul job on the next tick.

                LogApi.Info($"Updated stockpile '{stockpile.label}' (ID: {request.ZoneId})");

                return ApiResult<StockpileResponseDto>.Ok(new StockpileResponseDto
                {
                    Success = true,
                    ZoneId = stockpile.ID,
                    Name = stockpile.label,
                    CellsCount = stockpile.CellCount,
                    Priority = (int)stockpile.settings.Priority,
                    Message = $"Stockpile '{stockpile.label}' updated successfully.",
                    AppliedDefs = appliedDefs,
                    AppliedCategories = appliedCategories,
                    Unknown = unknown,
                    Filter = GetStockpileFilter(stockpile),
                });
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error updating stockpile: {ex}");
                return ApiResult<StockpileResponseDto>.Fail($"Failed to update stockpile: {ex.Message}");
            }
        }
    }
}
