using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Pairing burning cells with colonists (fox ticket #3444). A wildfire took
    /// colony 12 apart while the only lever the tool had was the Firefighter
    /// work priority, which sends a colonist to whatever fire the game picks:
    /// the barracks stood at 249 C with a downed colonist inside and nobody
    /// went. The order behind this plan names cells instead, so the pairing is
    /// plain arithmetic and stays testable without the game.
    ///
    /// One colonist per cell: a second job on the same fire waits for the first
    /// to finish, and a field fire has hundreds of cells. Colonists whose
    /// Firefighter work is on pick first, then the nearest cell wins.
    /// </summary>
    public static class FirefightPlan
    {
        public class BurningCell
        {
            public int X;
            public int Z;
        }

        public class FreePawn
        {
            public int Id;
            public string Name;
            public int X;
            public int Z;
            public bool FirefighterActive;
        }

        public class Assignment
        {
            public int FireX;
            public int FireZ;
            public int PawnId;
            public string PawnName;
            public bool FirefighterActive;
        }

        /// <summary>
        /// Assign pairs as many colonists with distinct cells as there are of
        /// both, at most <paramref name="limit"/> pairs (0 or less means "one
        /// per free colonist"). The result keeps the colonists' order, so the
        /// caller can name the same assignment twice and get the same answer.
        /// </summary>
        public static List<Assignment> Assign(
            IReadOnlyList<BurningCell> fires, IReadOnlyList<FreePawn> pawns, int limit)
        {
            List<Assignment> assignments = new List<Assignment>();
            if (fires == null || pawns == null || fires.Count == 0 || pawns.Count == 0)
            {
                return assignments;
            }

            List<FreePawn> order = new List<FreePawn>(pawns);
            // A colonist whose Firefighter work is on is the one the game would
            // send, so he picks before a nearer colonist who has it off. The id
            // breaks the tie so the answer does not depend on list order.
            order.Sort((a, b) =>
            {
                if (a.FirefighterActive != b.FirefighterActive)
                {
                    return a.FirefighterActive ? -1 : 1;
                }
                return a.Id.CompareTo(b.Id);
            });

            int max = limit > 0 && limit < order.Count ? limit : order.Count;
            bool[] taken = new bool[fires.Count];
            foreach (FreePawn pawn in order)
            {
                if (assignments.Count >= max)
                {
                    break;
                }
                int best = -1;
                long bestDistance = long.MaxValue;
                for (int i = 0; i < fires.Count; i++)
                {
                    if (taken[i])
                    {
                        continue;
                    }
                    long dx = fires[i].X - pawn.X;
                    long dz = fires[i].Z - pawn.Z;
                    long distance = dx * dx + dz * dz;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = i;
                    }
                }
                if (best < 0)
                {
                    break;
                }
                taken[best] = true;
                assignments.Add(new Assignment
                {
                    FireX = fires[best].X,
                    FireZ = fires[best].Z,
                    PawnId = pawn.Id,
                    PawnName = pawn.Name,
                    FirefighterActive = pawn.FirefighterActive,
                });
            }
            return assignments;
        }
    }
}
