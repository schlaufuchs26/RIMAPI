using System.Globalization;

namespace RIMAPI.Helpers
{
    // The order that keeps the range (fox ticket #4175). Since #4163 an attack
    // on a target beyond the weapon's reach queues the melee job, which is what
    // the game's own right-click does: a revolver pawn ordered onto a raider 120
    // cells away charges into contact. That is right for a colony with a club
    // and wrong for one with a gun, so the order grows a second mode: walk to a
    // cell that has the shot, then fire from there.
    //
    // The wording lives here so Source/RIMAPI.Tests can state it without a game;
    // CombatService finds the cell, walks the pawn there and queues the shot.
    public static class ShootRules
    {
        /// <summary>One shot order, as the mod planned it.</summary>
        public struct Plan
        {
            /// <summary>The weapon can hit from the cell the pawn stands on, so no walk is needed.</summary>
            public bool InRange;
            /// <summary>A cell with a shot and a walk to it was found.</summary>
            public bool Found;
            /// <summary>Cells within the weapon's reach that have the target in sight.</summary>
            public int CellsWithShot;
            /// <summary>How many of those the pawn may walk to, checked nearest first (at most the cap).</summary>
            public int ReachableCells;
            /// <summary>The walk to the chosen cell in path cells; zero when unmeasured.</summary>
            public int Walk;
            /// <summary>Cells between the chosen cell and the target.</summary>
            public float Distance;
            /// <summary>The weapon's range in cells (EffectiveRange).</summary>
            public float Range;
            /// <summary>The chosen cell, e.g. "141,102"; empty when none was found.</summary>
            public string Cell;
            /// <summary>The pawn's cell, e.g. "120,88"; empty when unread.</summary>
            public string From;
            /// <summary>The target's cell, e.g. "145,3"; empty when unread.</summary>
            public string Target;
        }

        /// <summary>
        /// How many cells with a shot are tried for a walk. Every candidate costs
        /// the game's pathfinder, so a target in a sealed room must not turn one
        /// order into a full map search; the count is named when none of them is
        /// reachable.
        /// </summary>
        public const int ReachChecks = 40;

        /// <summary>The refusal for a pawn without a ranged weapon.</summary>
        public static string NoRanged(string pawn)
        {
            return pawn + " has no ranged weapon, so there is nothing to shoot from range: "
                + "`attack` closes in and swings";
        }

        /// <summary>
        /// The note for a shot order that took: the cell the pawn walks to and
        /// why that cell has the shot, so a run knows the pawn moves first and
        /// fires on arrival instead of reading a plain walk.
        /// </summary>
        public static string PlanNote(Plan plan)
        {
            if (plan.InRange)
            {
                return $"he already has the shot from {plan.From}: {Cells(plan.Distance)} from the target, "
                    + $"the weapon reaches {Cells(plan.Range)}, so he stands and fires instead of closing in";
            }
            string head = $"he walks to {plan.Cell}, {Cells(plan.Distance)} from the target "
                + $"(the weapon reaches {Cells(plan.Range)})";
            if (plan.Walk > 0)
            {
                head += $", {plan.Walk} cells of path";
            }
            return head + ", and the shot is queued behind the walk: he fires when he gets there. "
                + "If the target moves out of the weapon's reach on the way, the pawn stands and waits";
        }

        /// <summary>
        /// Why no cell gives a shot the pawn can walk to. Two causes are worth
        /// separating because the next step differs: a target nothing has a line
        /// to (a wall all around) and cells with a line that the pawn cannot
        /// reach (water, a sealed room).
        /// </summary>
        public static string NoCellReason(Plan plan)
        {
            if (plan.CellsWithShot == 0)
            {
                return $"no cell within {Cells(plan.Range)} of the target at {plan.Target} has a line to it, "
                    + "so no shot from range is possible: `attack` closes in and swings";
            }
            return $"none of the {Count(plan.CellsWithShot)} cells with a shot that the pawn could try is reachable "
                + $"from {plan.From} (a wall, water or a sealed room in the way): `attack` closes in and swings";
        }

        private static string Count(int cells)
        {
            return cells >= ReachChecks ? $"nearest {ReachChecks}" : cells.ToString(CultureInfo.InvariantCulture);
        }

        private static string Cells(float cells)
        {
            return cells.ToString("0.##", CultureInfo.InvariantCulture)
                + (cells == 1f ? " cell" : " cells");
        }
    }
}
