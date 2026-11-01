using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The numbers behind a thermostat (fox ticket #3744): a cooler's and a
    /// heater's target temperature is a per-building setting the player changes
    /// on the building's own tab, and no endpoint read or set it. Colony 15
    /// built a powered cooler into a walled, roofed room and the room stayed at
    /// 16 C, the PassiveCooler's floor, because the cooler still stood at its
    /// default 21 C and nothing in the tool could see that, let alone change
    /// it.
    ///
    /// No RimWorld type appears here, so Source/RIMAPI.Tests states the rules
    /// outside the game (see TemperatureTargetRulesTests).
    /// </summary>
    public static class TemperatureTargetRules
    {
        /// <summary>The coldest value the game's temperature scale holds.</summary>
        public const float MinC = -273f;

        /// <summary>The hottest value the game's temperature scale holds.</summary>
        public const float MaxC = 1000f;

        /// <summary>
        /// True when the value is one the game's own scale can hold. A target
        /// off the scale is refused rather than clamped: no room can reach it,
        /// so a silent clamp would report a setting the caller never asked for.
        /// </summary>
        public static bool IsOnScale(float target)
        {
            if (float.IsNaN(target) || float.IsInfinity(target)) return false;
            return target >= MinC && target <= MaxC;
        }

        /// <summary>
        /// The refusal for a target off the scale, naming the range the game
        /// holds, or null when the value is fine.
        /// </summary>
        public static string Problem(float target)
        {
            if (IsOnScale(target)) return null;
            return "the target must be a temperature on the game's own scale, "
                + MinC + " to " + MaxC + " C, not " + target + " C";
        }

        /// <summary>
        /// True when the setting took. The game stores the target as a float
        /// and the answer reads it back, so the two are compared with a slack
        /// that hides the round-trip noise and nothing more.
        /// </summary>
        public static bool Took(float requested, float after)
        {
            float diff = after - requested;
            if (diff < 0f) diff = -diff;
            return diff <= 0.05f;
        }

        /// <summary>
        /// One room beside the building, as the answer reports it: the room the
        /// machine chills (or warms) and the room it dumps its exhaust into.
        /// </summary>
        public struct RoomReading
        {
            public int Id;
            public string Label;
            public float Temperature;
        }

        /// <summary>
        /// The index of the coldest room in the list, or -1 for no rooms. A
        /// cooler sits in a wall with a room on each side; the one it chills is
        /// the colder of the two, so this is the room a run has to watch. A
        /// heater stands inside its own room, which is the only one it touches.
        /// </summary>
        public static int ColdestIndex(IList<RoomReading> rooms)
        {
            if (rooms == null || rooms.Count == 0) return -1;
            int coldest = 0;
            for (int i = 1; i < rooms.Count; i++)
            {
                if (rooms[i].Temperature < rooms[coldest].Temperature) coldest = i;
            }
            return coldest;
        }
    }
}
