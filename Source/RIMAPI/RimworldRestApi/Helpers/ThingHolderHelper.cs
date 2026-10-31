using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    public static class ThingHolderHelper
    {
        // FindContainerHolding is the container a despawned thing sits in: a
        // grave's corpse, a frame's delivered wood, a shelf's inner owner. The
        // stored read lists those things, so a write that takes an id from it
        // has to name the container instead of answering "not found"
        // (tickets #3150, #3169).
        public static Thing FindContainerHolding(Map map, int thingId)
        {
            if (map == null)
            {
                return null;
            }
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (!(building is IThingHolder holder) || building is ISlotGroupParent)
                {
                    continue;
                }
                if (HoldsThing(holder, thingId, 0))
                {
                    return building;
                }
            }
            return null;
        }

        // Same lookup for a route that holds no map (map/building/power reads the
        // building by id across every map), ticket #3200.
        public static Thing FindContainerHoldingAcrossMaps(int thingId)
        {
            foreach (Map map in Find.Maps)
            {
                Thing holder = FindContainerHolding(map, thingId);
                if (holder != null)
                {
                    return holder;
                }
            }
            return null;
        }

        // The sentence every write route answers with, so a run reads the same
        // reason from equip, haul and power instead of three wordings: the id
        // named a thing the stored read lists, and it sits inside a container.
        public static string DescribeHeld(int thingId, Thing holder)
        {
            return $"Thing {thingId} is inside {holder.def.defName} #{holder.thingIDNumber} ({holder.LabelShort}), not on the map";
        }

        // A building taken apart and carried to storage is a MinifiedThing lying
        // in a stockpile: it is in the map's thing list, not in
        // allBuildingsColonist, so FindBuildingByID misses it and a write
        // answered a bare "not found" (ticket #3235). The stored read lists the
        // minified thing by its own id, so both that id and the id of the
        // building packed inside it match.
        public static MinifiedThing FindMinifiedAcrossMaps(int thingId)
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Thing thing in map.listerThings.AllThings)
                {
                    MinifiedThing minified = thing as MinifiedThing;
                    if (minified == null)
                    {
                        continue;
                    }
                    if (
                        minified.thingIDNumber == thingId
                        || (
                            minified.InnerThing != null
                            && minified.InnerThing.thingIDNumber == thingId
                        )
                    )
                    {
                        return minified;
                    }
                }
            }
            return null;
        }

        // The reason every building route answers for an id that names no built
        // building on any map, or null when the id belongs to nothing the
        // stored read lists: the container holding it, else the stored building
        // that has to be installed first (tickets #3200, #3235, #3295). The
        // route supplies the clause about what only a built building can do.
        public static string DescribeUnbuilt(int thingId, string onlyBuilt)
        {
            Thing holder = FindContainerHoldingAcrossMaps(thingId);
            if (holder != null)
            {
                return UnbuiltBuildingReason.Describe(
                    DescribeHeld(thingId, holder),
                    false,
                    onlyBuilt
                );
            }
            MinifiedThing minified = FindMinifiedAcrossMaps(thingId);
            if (minified != null)
            {
                return UnbuiltBuildingReason.Describe(
                    DescribeMinified(thingId, minified),
                    true,
                    onlyBuilt
                );
            }
            return null;
        }

        // What a write answers for a stored building: the def it would have once
        // installed, the minified thing's id and cell, and the step that is
        // missing. Same shape as DescribeHeld, so a run reads one wording.
        public static string DescribeMinified(int thingId, MinifiedThing minified)
        {
            Thing inner = minified.InnerThing;
            string def = inner != null ? inner.def.defName : minified.def.defName;
            return $"Thing {thingId} is a {def} packed up as {minified.def.defName} "
                + $"#{minified.thingIDNumber} at ({minified.Position.x}, {minified.Position.z}), "
                + "not built on the map";
        }

        private static bool HoldsThing(IThingHolder holder, int thingId, int depth)
        {
            if (depth > 4)
            {
                return false;
            }
            ThingOwner owner = holder.GetDirectlyHeldThings();
            if (owner == null)
            {
                return false;
            }
            for (int i = 0; i < owner.Count; i++)
            {
                Thing held = owner[i];
                if (held.thingIDNumber == thingId)
                {
                    return true;
                }
                if (held is IThingHolder nested && HoldsThing(nested, thingId, depth + 1))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
