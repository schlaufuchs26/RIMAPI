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
                throw new System.Exception($"Map not found: {mapId}");
            }

            var dto = new MapRottingFoodDto();
            int tile = MapHelper.GetMapTileId(map);
            var items = new List<RottingFoodDto>();
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
            return dto;
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
