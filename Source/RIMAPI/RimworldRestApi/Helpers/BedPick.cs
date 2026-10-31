using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Which bed a carry order walks a pawn to (fox ticket #3421). Rescue
    /// (#3383) and arrest/capture take the bed the game's own lookup picked
    /// first, and that bed can stand in a burning or oven-hot room: a pawn
    /// carried into it cannot walk out again. The rule is BedSafety; this class
    /// is the game-side search for a bed that obeys it.
    /// </summary>
    public static class BedPick
    {
        /// <summary>
        /// The nearest free bed of the wanted kind outside the fire's reach, or
        /// null when the colony has none. Nearest keeps the carry as short as
        /// the game's own choice once safety is equal. A rescue wants a normal
        /// bed, an arrest or capture a prisoner bed; the two never share one.
        /// </summary>
        public static Building_Bed SaferBed(
            Map map, Pawn target, Pawn carrier, Building_Bed unsafeBed, bool forPrisoners)
        {
            return map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>()
                .Where(b => b != unsafeBed && b.Spawned)
                .Where(b => forPrisoners ? b.ForPrisoners : !b.ForPrisoners && !b.ForSlaves)
                .Where(b => b.AnyUnownedSleepingSlot)
                .Where(b => !BedSafety.IsUnsafe(b.AmbientTemperature, RoomHasFire(map, b)))
                .Where(b => carrier.CanReserveAndReach(b, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, false))
                .OrderBy(b => (b.Position - target.Position).LengthHorizontalSquared)
                .FirstOrDefault();
        }

        /// <summary>
        /// The room the bed stands in: the room read reports its beds, the bed
        /// itself reports no room. Null for a bed whose cell belongs to no
        /// region the room grid knows.
        /// </summary>
        public static Room BedRoom(Map map, Building_Bed bed)
        {
            return map.regionGrid.AllRooms.FirstOrDefault(r => r.ContainedBeds.Contains(bed));
        }

        /// <summary>
        /// The fire the thermometer can miss: a small fire in a large room
        /// leaves the room below the heat threshold while it spreads towards
        /// the bed.
        /// </summary>
        public static bool RoomHasFire(Map map, Building_Bed bed)
        {
            Room room = BedRoom(map, bed);
            if (room == null) return false;
            return map.listerThings.ThingsOfDef(ThingDefOf.Fire).Any(f => f.GetRoom() == room);
        }
    }
}
