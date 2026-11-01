using System;
using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The target half of `prioritize` (fox ticket #3708): a cell target means
    /// the things standing on it. The game's construct work givers answer only
    /// for a thing, never for a cell, so asking the cell alone reached the roof
    /// giver first and a wall frame was never built. No RimWorld type appears
    /// here, so RIMAPI.Tests covers the ordering (see
    /// PriorityTargetRulesTests).
    /// </summary>
    public static class PriorityTargetRules
    {
        /// <summary>
        /// ConstructiblesFirst puts the blueprints and frames of a cell ahead of
        /// everything else standing there, keeping the game's order inside each
        /// group: a caller who names a cell with a blueprint on it wants that
        /// built, not the roof above it.
        /// </summary>
        public static List<T> ConstructiblesFirst<T>(IEnumerable<T> items, Func<T, bool> isConstructible)
        {
            List<T> ordered = new List<T>();
            if (items == null)
            {
                return ordered;
            }
            List<T> rest = new List<T>();
            foreach (T item in items)
            {
                if (isConstructible != null && isConstructible(item))
                {
                    ordered.Add(item);
                }
                else
                {
                    rest.Add(item);
                }
            }
            ordered.AddRange(rest);
            return ordered;
        }

        /// <summary>
        /// IsConstructJob reports whether a job builds a blueprint or a frame:
        /// the game's three construct jobs all carry the prefix, while the roof
        /// job (BuildRoof) and the deconstruct of a blueprint do not. A cell or
        /// thing that carries a blueprint is what those jobs are for.
        /// </summary>
        public static bool IsConstructJob(string jobDefName)
        {
            return !string.IsNullOrEmpty(jobDefName)
                && jobDefName.StartsWith("Construct", StringComparison.Ordinal);
        }
    }
}
