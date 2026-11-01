using System.Collections.Generic;
using System.Linq;
using RIMAPI.Models;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The perishable food that stands in a warm room, with the room and the
    /// hours the stack has left (fox ticket #3672). Every part exists in some
    /// read already; the rooms carry their temperature, the things their
    /// stack, the rot comp its progress; but no read joins them, so a run
    /// could not tell that the meat in the 16 C room was on its way out.
    /// </summary>
    public static class RottingFood
    {
        /// <summary>
        /// Every perishable food stack on the map whose room is above 0 C,
        /// soonest rot first. Food a shelf holds is included: it sits in the
        /// shelf's inner container, so listerThings never sees it, and a shelf
        /// in a warm room spoils its contents like any other store.
        /// </summary>
        public static MapRottingFoodDto ForMap(int mapId)
        {
            Map map = MapHelper.GetMapByID(mapId);
            if (map == null)
            {
                throw new System.Exception(MapLookup.MapNotFound(mapId));
            }

            var dto = new MapRottingFoodDto();
            int tile = MapHelper.GetMapTileId(map);
            var items = new List<RottingFoodDto>();
            var warmRooms = new HashSet<int>();
            foreach (Thing thing in FoodThings(map))
            {
                CompRottable rottable = thing.TryGetComp<CompRottable>();
                IntVec3 cell = thing.Spawned ? thing.Position : thing.PositionHeld;
                Room room = cell.IsValid ? cell.GetRoom(map) : null;

                var state = new RottingFoodRules.FoodState
                {
                    NutritionGiving = thing.def.IsNutritionGivingIngestible,
                    Corpse = thing is Corpse,
                    Rottable = rottable != null,
                    HasRoom = room != null,
                    RoomTemperature = room?.Temperature ?? 0f,
                };
                if (!RottingFoodRules.IsAtRisk(state))
                {
                    continue;
                }
                warmRooms.Add(room.ID);

                float hours = RottingFoodRules.ClampHours(
                    DaysUntilRotCalculator.ApproxTicksUntilRot_AssumeTimePassesBy(rottable, tile)
                        / 60000f
                        * 24f
                );
                items.Add(
                    new RottingFoodDto
                    {
                        ThingId = thing.thingIDNumber,
                        DefName = thing.def.defName,
                        Label = thing.Label,
                        StackCount = thing.stackCount,
                        Nutrition = thing.GetStatValue(StatDefOf.Nutrition) * thing.stackCount,
                        HoursUntilRot = hours,
                        RoomId = room.ID,
                        RoomLabel = room.GetRoomRoleLabel(),
                        RoomTemperature = room.Temperature,
                        Position = new PositionDto
                        {
                            X = cell.IsValid ? cell.x : 0,
                            Z = cell.IsValid ? cell.z : 0,
                        },
                    }
                );
            }

            dto.Items = items.OrderBy(i => i.HoursUntilRot).ToList();
            dto.TotalStacks = dto.Items.Count;
            dto.TotalUnits = dto.Items.Sum(i => i.StackCount);
            dto.WarmestTemperature = dto.Items.Count == 0
                ? 0f
                : dto.Items.Max(i => i.RoomTemperature);
            dto.Thermostats = RoomThermostats(map, warmRooms);
            return dto;
        }

        /// <summary>
        /// The coolers and heaters that work on a warm room in the answer (fox
        /// tickets #3745, #3834). The room read names a temperature and the rot
        /// read a stack, but a warm room reads the same with or without a
        /// machine in it: colony 15's cooler still stood at its default 21 C, so
        /// the room never chilled below 21 and the meat would have spoiled
        /// beside a machine that looked like the fix. The side says which way
        /// round the machine sits: a cooler that vents into the warm room heats
        /// it and cannot be fixed by turning its target down.
        /// </summary>
        private static List<RoomThermostatDto> RoomThermostats(Map map, HashSet<int> roomIds)
        {
            var found = new List<RoomThermostatDto>();
            if (roomIds.Count == 0) return found;
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                CompTempControl comp = building.TryGetComp<CompTempControl>();
                if (comp == null) continue;
                CompPowerTrader power = building.TryGetComp<CompPowerTrader>();
                foreach (RoomSideDto side in Thermostat.Sides(building))
                {
                    if (side.Room == null || !roomIds.Contains(side.Room.Id)) continue;
                    found.Add(
                        new RoomThermostatDto
                        {
                            RoomId = side.Room.Id,
                            Side = side.Side,
                            BuildingId = building.thingIDNumber,
                            Label = building.Label,
                            DefName = building.def?.defName,
                            Target = comp.targetTemperature,
                            HasPower = power != null,
                            Powered = power != null && power.PowerOn,
                        }
                    );
                }
            }
            return found;
        }

        /// <summary>The food stacks to look at: spawned storables plus what a shelf holds.</summary>
        private static List<Thing> FoodThings(Map map)
        {
            var food = map
                .listerThings.AllThings.Where(t =>
                    t.def.EverStorable(false) && t.def.IsNutritionGivingIngestible
                )
                .ToList();
            food.AddRange(ResourcesHelper.GetFoodHeldInContainers(map));
            return food;
        }
    }
}
