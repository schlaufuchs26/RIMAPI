using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The arithmetic behind a candidate batch (fox ticket #3924): the RNG
    /// seed a batch is reproduced from, the bounds a batch and a pick are
    /// checked against, and which skills are worth printing. Pure integers and
    /// strings, so the unit tests cover it without a running game.
    /// </summary>
    public static class PawnBatchRules
    {
        // Most candidates one call may ask for. Thirty is what a run asks for;
        // the cap keeps one answer inside a payload a model can read, and a
        // batch larger than the game's own reroll screen offers is no use.
        public const int MaxCount = 60;

        // The skills a candidate line carries: a level above 0 or a passion.
        // The other eight or so are all zero and none and would cost a line
        // each without telling a pick anything.
        public static bool WorthShowing(int level, int passion)
        {
            return level > 0 || passion > 0;
        }

        /// <summary>
        /// The seed the candidates are drawn from. It is derived from the world
        /// seed alone, so two calls with the same seed build the same 30, and
        /// the colony start can rebuild the same list to install a pick.
        /// FNV-1a, because string.GetHashCode is randomized per process and
        /// GenText.StableStringHash is a game type the unit tests do not carry.
        /// </summary>
        public static int SeedOf(string worldSeed)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in (worldSeed ?? "").ToLowerInvariant())
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }

        /// <summary>
        /// The RNG state one candidate is drawn from. Each candidate gets its
        /// own, so a name the game rejects and redraws cannot shift the pawns
        /// after it: id 7 is the same pawn whatever the batch around it did.
        /// </summary>
        public static int PawnSeed(string worldSeed, int id)
        {
            unchecked
            {
                uint hash = (uint)SeedOf(worldSeed) ^ ((uint)id * 2654435761u);
                return (int)(hash ^ (hash >> 16));
            }
        }

        /// <summary>
        /// Why a batch request is refused, or "" when it is fine. The batch has
        /// to offer at least as many candidates as the scenario starts
        /// colonists, or the run could not fill its colony from it.
        /// </summary>
        public static string BatchProblem(int count, int pawnCount)
        {
            if (count < 1)
                return $"a candidate batch needs a positive count, got {count}.";
            if (count > MaxCount)
                return $"a candidate batch holds at most {MaxCount} candidates, got {count}.";
            if (pawnCount > 0 && count < pawnCount)
                return $"the scenario starts {pawnCount} colonists, so the batch needs at least that many candidates, got {count}.";
            return "";
        }

        /// <summary>
        /// Why a pick is refused, or "" when it is fine. Ids are 1-based
        /// positions in the batch the answer printed.
        /// </summary>
        public static string PicksProblem(IList<int> picks, int count)
        {
            if (picks == null || picks.Count == 0)
                return "";
            if (count > 0 && picks.Count > count)
                return $"the pick names {picks.Count} candidates but the batch holds {count}.";
            HashSet<int> seen = new HashSet<int>();
            foreach (int id in picks)
            {
                if (id < 1 || (count > 0 && id > count))
                    return count > 0
                        ? $"candidate {id} is not in the batch: its ids are 1 to {count}."
                        : $"candidate {id} is not a batch id: ids start at 1.";
                if (!seen.Add(id))
                    return $"candidate {id} is picked twice.";
            }
            return "";
        }
    }
}
