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
        /// The two cells a cooler works across: the one whose room it chills and
        /// the one it dumps its heat into, in map coordinates.
        /// </summary>
        public struct CoolerCells
        {
            public int ColdX;
            public int ColdZ;
            public int VentX;
            public int VentZ;
        }

        /// <summary>
        /// The cells a cooler chills and vents into, from its own cell and its
        /// rotation (fox ticket #3834). The game's own rule, read from
        /// Building_Cooler.TickRare: the cold cell is IntVec3.South rotated by
        /// the building's rotation and the vent cell is IntVec3.North rotated
        /// by it. A read that instead guessed from the two rooms' temperatures
        /// named the outdoors whenever the machine's cold side was a wall, and
        /// a working freezer then looked broken.
        ///
        /// Rotation is Rot4.AsInt: 0 north, 1 east, 2 south, 3 west.
        /// </summary>
        public static CoolerCells CoolerSideCells(int x, int z, int rotation)
        {
            var cells = new CoolerCells();
            switch (rotation & 3)
            {
                case 1: // east: south rotated to west, north rotated to east
                    cells.ColdX = x - 1;
                    cells.ColdZ = z;
                    cells.VentX = x + 1;
                    cells.VentZ = z;
                    break;
                case 2: // south
                    cells.ColdX = x;
                    cells.ColdZ = z + 1;
                    cells.VentX = x;
                    cells.VentZ = z - 1;
                    break;
                case 3: // west
                    cells.ColdX = x + 1;
                    cells.ColdZ = z;
                    cells.VentX = x - 1;
                    cells.VentZ = z;
                    break;
                default: // north
                    cells.ColdX = x;
                    cells.ColdZ = z - 1;
                    cells.VentX = x;
                    cells.VentZ = z + 1;
                    break;
            }
            return cells;
        }

    }
}
