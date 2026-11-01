using System.Globalization;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Whether a bed is a fit destination for a carried patient (fox ticket
    /// #3383). The game's own rescue lookup takes the first free bed it finds,
    /// and in colony 10 that was the one inside a room at 111 C: the patient was
    /// put back into the burning room and got heatstroke while a free bed stood
    /// 13 cells away on open ground. The rule is one number and one flag, kept
    /// free of game types so a test can state it.
    /// </summary>
    public static class BedSafety
    {
        /// <summary>
        /// The room temperature at which a bed is no destination for a carried
        /// patient (fox ticket #3383). The fox's digest calls a room a danger at
        /// 40 C already (core/rimworld/digest.go, HotRoomC); a bed is stricter
        /// because the patient cannot walk out of it.
        /// </summary>
        public const float TooHotC = 50f;

        /// <summary>
        /// Why the bed is no destination, or null when it is one. A fire in the
        /// room counts whatever the thermometer reads: it spreads, and a patient
        /// who cannot walk cannot leave it.
        /// </summary>
        public static string UnsafeReason(float temperatureC, bool roomHasFire)
        {
            if (roomHasFire) return "a fire burns in its room";
            if (temperatureC >= TooHotC)
            {
                // Invariant, because the number reaches a Go answer that prints
                // its own decimals; a German locale would send "111,9 C".
                return "its room reads "
                    + temperatureC.ToString("F1", CultureInfo.InvariantCulture)
                    + " C, above the " + TooHotC.ToString("F0", CultureInfo.InvariantCulture)
                    + " C a carried patient cannot take";
            }
            return null;
        }

        public static bool IsUnsafe(float temperatureC, bool roomHasFire)
        {
            return UnsafeReason(temperatureC, roomHasFire) != null;
        }

        /// <summary>
        /// Why a prisoner bed is no cell, or null when it is one (fox ticket
        /// #3621). Temperature and fire say nothing about this: a cool bed on
        /// open ground passes every check above, and the prisoner put into it
        /// is "escaping" in the same minute, because the game counts only a bed
        /// inside an enclosed room as a prison cell. The caller passes the
        /// game's own cell answer, so this stays free of game types.
        /// </summary>
        public static string NoCellReason(bool hasRoom, bool outdoors, bool isPrisonCell)
        {
            if (isPrisonCell) return null;
            if (!hasRoom || outdoors) return "it stands outdoors";
            return "its room is no prison cell";
        }

        /// <summary>
        /// Why the game would refuse to make this room a prison cell, or null
        /// when it can be one (fox ticket #3742). The game counts only a
        /// proper room that is not huge (Building_Bed.RoomCanBePrisonCell),
        /// and a bed set to prisoners in any other room never takes a
        /// prisoner: the game's own bed lookup passes it over, so an arrest
        /// answers "no prisoner bed" although the colony has one. The caller
        /// passes the game's own room answers, so this stays free of game
        /// types and a test can state the rule.
        ///
        /// The label rides along with the sentence (fox ticket #3750): the
        /// sentence is written for prose and about 80 characters long, which
        /// is too much for a marker next to a bed id in a list.
        /// </summary>
        public static CellRefusal NotPrisonCell(bool hasRoom, bool touchesMapEdge, bool huge, bool enclosed)
        {
            if (!hasRoom) return new CellRefusal("no room", "it stands in no room");
            if (touchesMapEdge)
                return new CellRefusal("map edge",
                    "its room touches the map edge, which the game never counts as a prison cell");
            if (huge) return new CellRefusal("too large", "its room is too large to be a prison cell");
            if (!enclosed) return new CellRefusal("not enclosed", "its room is not enclosed");
            return null;
        }

        /// <summary>
        /// The sentence alone, for a caller that has no room for the label.
        /// </summary>
        public static string NotPrisonCellReason(bool hasRoom, bool touchesMapEdge, bool huge, bool enclosed)
        {
            return NotPrisonCell(hasRoom, touchesMapEdge, huge, enclosed)?.Reason;
        }
    }

    /// <summary>
    /// A room the game refuses as a prison cell, as the short label and the
    /// sentence that explains it (fox ticket #3750). The two travel together,
    /// so the compact marker in a list and the prose answer never disagree
    /// about the cause.
    /// </summary>
    public sealed class CellRefusal
    {
        public CellRefusal(string cause, string reason)
        {
            Cause = cause;
            Reason = reason;
        }

        /// <summary>The label a marker prints, e.g. "map edge".</summary>
        public string Cause { get; }

        /// <summary>The sentence an answer prints, e.g. "its room touches the
        /// map edge, which the game never counts as a prison cell".</summary>
        public string Reason { get; }
    }
}
