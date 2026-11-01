using System.Collections.Generic;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// How much room the map's storage has for one product (fox ticket #3648).
    /// Colony 14 stood at 167 ripe rice plants with 78 of them marked for
    /// harvest and nobody picked for a whole game day, while the store held 38
    /// stacks with no free cell left. Every read named the plants and none the
    /// room, so the cause only showed up after a second stockpile happened to
    /// be built.
    ///
    /// The def may be a plant: a growing zone is sown with Plant_Rice while
    /// the store takes RawRice, so asking with the plant name keeps the caller
    /// from having to know the product. The cell answer is the game's own
    /// IsGoodStoreCell, which honours stacking, shelves' several stacks per
    /// cell, reservations and storage blockers.
    /// </summary>
    public static class StorageCapacity
    {
        /// <summary>
        /// The room the map's storages have for the def, as the DTO the route
        /// answers. Throws for a name no def carries and for a def that is no
        /// storable item, so the caller reports the bad input instead of a
        /// capacity of zero that reads like a full store.
        /// </summary>
        public static StorageCapacityDto ForDef(int mapId, string defName)
        {
            Map map = MapHelper.GetMapByID(mapId);
            if (map == null)
            {
                throw new System.Exception($"Map not found: {mapId}");
            }
            ThingDef asked = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (asked == null)
            {
                throw new System.Exception(StorageCapacityText.UnknownDefMessage(defName));
            }
            ThingDef thingDef = asked;
            if (asked.plant != null && asked.plant.harvestedThingDef != null)
            {
                thingDef = asked.plant.harvestedThingDef;
            }
            if (!thingDef.EverStorable(false))
            {
                throw new System.Exception(StorageCapacityText.NotStorableMessage(thingDef.defName));
            }

            Thing probe = ThingMaker.MakeThing(thingDef);
            int storages = 0;
            int cells = 0;
            int freeCells = 0;
            foreach (SlotGroup group in Groups(map))
            {
                if (group.Settings == null || group.Settings.filter == null) continue;
                if (!group.Settings.filter.Allows(thingDef)) continue;
                storages++;
                foreach (IntVec3 cell in group.CellsList)
                {
                    cells++;
                    // No carrier: the question is room, not reach, and an
                    // unspawned probe has no position the game could walk from.
                    if (StoreUtility.IsGoodStoreCell(cell, map, probe, null, Faction.OfPlayer))
                    {
                        freeCells++;
                    }
                }
            }
            return new StorageCapacityDto
            {
                DefName = defName,
                PlantDef = thingDef != asked ? defName : null,
                ThingDef = thingDef.defName,
                Storages = storages,
                Cells = cells,
                FreeCells = freeCells,
            };
        }

        /// <summary>
        /// The slot groups that could take a hauled thing: stockpiles and the
        /// storage buildings (a shelf, a grave). Same walk as the storage
        /// filters the unwanted-food read uses, so the two reads agree about
        /// which storages exist.
        /// </summary>
        public static List<SlotGroup> Groups(Map map)
        {
            var groups = new List<SlotGroup>();
            foreach (Zone zone in map.zoneManager.AllZones)
            {
                if (zone is Zone_Stockpile stockpile)
                {
                    groups.Add(stockpile.GetSlotGroup());
                }
            }
            foreach (Building_Storage storage in map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>())
            {
                groups.Add(storage.GetSlotGroup());
            }
            return groups;
        }

    }
}
