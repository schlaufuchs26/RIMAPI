using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// What a player can see of the map (fox ticket #4220). The fog grid hides
    /// every cell the colony has not explored, and the game draws only the
    /// revealed ones; a read that walks the whole map hands a caller things,
    /// buildings, plants, ore and animals standing in the dark, which is
    /// information no player has.
    ///
    /// No RimWorld type appears here: the caller passes the fog verdict per
    /// cell, so the rules are stated outside the game (see FogRevealTests).
    /// </summary>
    public static class FogReveal
    {
        /// <summary>True for a cell the player sees: the fog does not hide it.</summary>
        public static bool ShowsCell(bool fogged)
        {
            return !fogged;
        }

        /// <summary>
        /// True when at least one of the cells a thing occupies is revealed.
        /// The game draws a multi-cell thing cell by cell, so a wall standing
        /// half in the dark is visible from the lit side; hiding it whole would
        /// hide something the player can see. A thing with no cell (a despawned
        /// one) puts nothing on screen, so it is not shown either.
        /// </summary>
        public static bool ShowsAny(IEnumerable<bool> foggedCells)
        {
            if (foggedCells == null) return false;
            foreach (bool fogged in foggedCells)
            {
                if (!fogged) return true;
            }
            return false;
        }
    }
}
