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
