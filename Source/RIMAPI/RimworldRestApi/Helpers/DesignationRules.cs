namespace RIMAPI.Helpers
{
    /// <summary>
    /// Which targets a designation order covers (fox ticket #3425). The game
    /// has its own answer, and a tool that guesses differently fills the work
    /// list with jobs no colonist ever does.
    ///
    /// Harvest is the sharp case: Designator_PlantsHarvest.CanDesignateThing
    /// wants HarvestableNow and the "Standard" harvest tag, while a tree
    /// carries "Wood" (Plants_Bases.xml) and belongs to the chop order. A tree
    /// still reports HarvestableNow from 40 % growth, so checking that alone
    /// marked 770 trees as HarvestPlant in one sweep.
    ///
    /// Plain values, so the rule is checked without a game.
    /// </summary>
    public static class DesignationRules
    {
        public const string StandardHarvestTag = "Standard";

        /// <summary>
        /// Whether the harvest order takes a plant: ripe for picking and
        /// carrying the standard tag, which is exactly the game's designator
        /// rule. A null tag is no standard tag.
        /// </summary>
        public static bool IsHarvestTarget(bool harvestableNow, string harvestTag)
        {
            return harvestableNow && harvestTag == StandardHarvestTag;
        }

        /// <summary>
        /// Whether a rectangle touches a target's footprint, which is how a
        /// clear call finds what a designate call marked: designate walks the
        /// cells of a rectangle and marks the thing on each, so a 2x2 building
        /// is marked from any one of its cells and must be cleared from any
        /// one of them too. Corners may come in any order; a target with no
        /// cells (a thing outside the map) is never covered.
        /// </summary>
        public static bool Covers(int targetX1, int targetZ1, int targetX2, int targetZ2,
            int x1, int z1, int x2, int z2)
        {
            int minX = x1 < x2 ? x1 : x2;
            int maxX = x1 < x2 ? x2 : x1;
            int minZ = z1 < z2 ? z1 : z2;
            int maxZ = z1 < z2 ? z2 : z1;
            if (targetX1 > targetX2)
            {
                int swap = targetX1;
                targetX1 = targetX2;
                targetX2 = swap;
            }
            if (targetZ1 > targetZ2)
            {
                int swap = targetZ1;
                targetZ1 = targetZ2;
                targetZ2 = swap;
            }
            return targetX1 <= maxX && targetX2 >= minX && targetZ1 <= maxZ && targetZ2 >= minZ;
        }
    }
}
