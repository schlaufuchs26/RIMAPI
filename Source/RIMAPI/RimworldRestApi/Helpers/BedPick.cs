using System.Collections.Generic;
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
        ///
        /// A prisoner bed also has to be a real cell (fox ticket #3621): the
        /// game calls only a bed inside an enclosed room a prison cell, and a
        /// prisoner carried into an open bed escapes at once, however cool the
        /// spot is. So the search never proposes a bed that is no cell.
        /// </summary>
        public static Building_Bed SaferBed(
            Map map, Pawn target, Pawn carrier, Building_Bed unsafeBed, bool forPrisoners)
        {
            return map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>()
                .Where(b => b != unsafeBed && b.Spawned)
                .Where(b => forPrisoners ? b.ForPrisoners : !b.ForPrisoners && !b.ForSlaves)
                .Where(b => b.AnyUnownedSleepingSlot)
                .Where(b => !forPrisoners || IsCell(map, b))
                .Where(b => !BedSafety.IsUnsafe(b.AmbientTemperature, RoomHasFire(map, b)))
                .Where(b => carrier.CanReserveAndReach(b, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, false))
                .OrderBy(b => (b.Position - target.Position).LengthHorizontalSquared)
                .FirstOrDefault();
        }

        /// <summary>
        /// Whether the bed stands in a room the game calls a prison cell: an
        /// enclosed room holding a prisoner bed. This is the game's own answer
        /// (`Room.IsPrisonCell`), not a roof guess, because that is what the
        /// prisoner's escape check reads (fox ticket #3621).
        /// </summary>
        public static bool IsCell(Map map, Building_Bed bed)
        {
            Room room = BedRoom(bed);
            return room != null && room.IsPrisonCell;
        }

        /// <summary>
        /// Why the bed is no cell, or "" when it is one: the rule lives in
        /// BedSafety, this is the game-side read of it.
        /// </summary>
        public static string NoCellReason(Map map, Building_Bed bed)
        {
            Room room = BedRoom(bed);
            return BedSafety.NoCellReason(room != null, Outdoors(map, bed), room != null && room.IsPrisonCell) ?? "";
        }

        /// <summary>
        /// The room the bed stands in: the bed itself reports it (fox ticket
        /// #3770). Scanning AllRooms for one whose ContainedBeds names the bed
        /// is not the same question: the game's ContainedBeds is
        /// ContainedAndAdjacentThings, so the doorway room beside a bedroom
        /// names the same bed, and whichever room comes first in the list wins.
        /// Null for a bed whose cell belongs to no region the room grid knows.
        /// </summary>
        public static Room BedRoom(Building_Bed bed)
        {
            return bed.GetRoom();
        }

        /// <summary>
        /// The room's role label as the answer should print it, or "" when the
        /// room has none: the game's own word for that is "none", which read
        /// as a room name ("in the none at 34.5 C", fox ticket #3622). A bed
        /// with no room of its own gets that word too.
        /// </summary>
        public static string RoomLabel(Room room)
        {
            if (room == null || room.Role == null) return "";
            return room.GetRoomRoleLabel();
        }

        /// <summary>
        /// Whether the bed stands under no roof. A roleless room and the open
        /// map read the same through the room label, and only the roof grid
        /// tells them apart, so the answer says "outdoors" only here.
        /// </summary>
        public static bool Outdoors(Map map, Building_Bed bed)
        {
            return !bed.Position.Roofed(map);
        }

        /// <summary>
        /// The fire the thermometer can miss: a small fire in a large room
        /// leaves the room below the heat threshold while it spreads towards
        /// the bed.
        /// </summary>
        public static bool RoomHasFire(Map map, Building_Bed bed)
        {
            Room room = BedRoom(bed);
            if (room == null) return false;
            return map.listerThings.ThingsOfDef(ThingDefOf.Fire).Any(f => f.GetRoom() == room);
        }

        /// <summary>
        /// Recomputes the room's cached prison-cell flag after the bed's owner
        /// type changed (fox ticket #3742). The bed's own setter only dirties
        /// the room's role (Room.Notify_BedTypeChanged), while the flag the
        /// game's bed lookup reads (Room.IsPrisonCell) follows the room's
        /// shape. Vanilla's "Set for prisoners" command therefore refreshes
        /// the district and the room itself after the change
        /// (Building_Bed.SetBedOwnerTypeByInterface); a bed order that does not
        /// leaves the room answering "no prison cell" until a wall or door
        /// changes, so the game's lookup refuses the bed an arrest needs.
        /// </summary>
        public static void RefreshAfterBedTypeChange(Building_Bed bed)
        {
            if (bed == null || !bed.Spawned) return;
            District district = bed.GetDistrict();
            Room room = district?.Room;
            if (district == null || room == null) return;
            district.Notify_RoomShapeOrContainedBedsChanged();
            if (!room.TouchesMapEdge) room.Notify_RoomShapeChanged();
        }

        /// <summary>
        /// Refreshes every room that could be a prison cell, holds a prisoner
        /// bed and still carries the stale false flag (fox ticket #3742). A bed
        /// set to prisoners before its room changed shape leaves that flag
        /// behind, and the game's own bed lookup then passes the bed over, so
        /// the arrest route runs this before it asks the game. Cheap and
        /// idempotent: rooms whose flag is already right are skipped.
        /// </summary>
        public static void RefreshStalePrisonCells(Map map)
        {
            foreach (Room room in map.regionGrid.AllRooms)
            {
                if (room == null || room.IsPrisonCell) continue;
                if (!Building_Bed.RoomCanBePrisonCell(room)) continue;
                if (!room.ContainedBeds.Any(b => b.ForPrisoners)) continue;
                room.Notify_RoomShapeChanged();
            }
        }

        /// <summary>
        /// Why the game finds no prisoner bed for this arrest, in the game's own
        /// terms (fox ticket #3742). "no prisoner bed" alone leaves a run
        /// guessing: the colony may have no such bed, or a bed whose room the
        /// game refuses, or one the arrestor cannot reach. The first three
        /// candidates are named with the condition each one fails.
        /// </summary>
        public static string PrisonerBedRefusal(Map map, Pawn arrestor)
        {
            List<Building_Bed> beds = map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>()
                .Where(b => b.Spawned && b.def.building != null && b.def.building.bed_humanlike)
                .ToList();
            if (beds.Count == 0)
            {
                return "the map holds no humanlike bed: `build name=Bed` places one and `bed` sets it to prisoners";
            }
            List<Building_Bed> prisonerBeds = beds.Where(b => b.ForPrisoners).ToList();
            if (prisonerBeds.Count == 0)
            {
                return "none of the " + beds.Count + " humanlike beds is set to prisoners: "
                    + "`things` lists the beds and `bed` with bed_for=prisoners sets one";
            }
            List<string> named = new List<string>();
            foreach (Building_Bed bed in prisonerBeds.Take(3))
            {
                named.Add(bed.Label + " (id " + bed.thingIDNumber + ") " + WhyNoPrisonerBed(map, bed, arrestor));
            }
            string text = string.Join("; ", named);
            if (prisonerBeds.Count > 3)
            {
                text += "; and " + (prisonerBeds.Count - 3) + " more";
            }
            return text;
        }

        /// <summary>
        /// Why the game would refuse this bed's room as a prison cell, or null
        /// when it would not (fox ticket #3743). The cell half of the arrest
        /// refusal, shared with the bed order's own answer so the two never
        /// disagree. The label travels with the sentence (fox ticket #3750):
        /// the bed list prints the label, the bed answer the sentence.
        /// </summary>
        public static CellRefusal CellRefusalOf(Map map, Building_Bed bed)
        {
            Room room = BedRoom(bed);
            CellRefusal noCell = BedSafety.NotPrisonCell(
                room != null,
                room != null && room.TouchesMapEdge,
                room != null && room.IsHuge,
                room != null && room.ProperRoom);
            if (noCell != null) return noCell;
            if (!bed.Position.IsInPrisonCell(map))
            {
                // A proper room whose flag the game has not re-read since the
                // bed changed; the arrest route refreshes these before its
                // lookup, so only a foreign change reaches this line.
                return new CellRefusal("not re-read", "its room has not been re-read as a prison cell");
            }
            return null;
        }

        /// <summary>
        /// The same refusal as a sentence, "" when the room would do: the
        /// refusal line of an arrest reads it mid-answer.
        /// </summary>
        public static string CellRefusal(Map map, Building_Bed bed)
        {
            return CellRefusalOf(map, bed)?.Reason ?? "";
        }

        /// <summary>
        /// The first condition that keeps the game from using this prisoner bed,
        /// in the order the game's own check reads them (RestUtility.
        /// CanUseBedNow and IsValidBedFor).
        /// </summary>
        private static string WhyNoPrisonerBed(Map map, Building_Bed bed, Pawn arrestor)
        {
            string noCell = CellRefusal(map, bed);
            if (noCell != "") return "is no prison bed: " + noCell;
            if (!bed.AnyUnoccupiedSleepingSlot) return "has no free sleeping slot";
            if (bed.IsBurning()) return "is on fire";
            if (bed.IsForbidden(arrestor)) return "is forbidden";
            if (!arrestor.CanReserveAndReach(bed, PathEndMode.OnCell, Danger.Some, 1, -1, null, false))
            {
                return "cannot be reached or reserved by " + arrestor.LabelShort;
            }
            return "is a prisoner bed the game's lookup passed over";
        }
    }
}
