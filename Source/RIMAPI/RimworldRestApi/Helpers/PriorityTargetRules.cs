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
        /// IsConstructJob reports whether a job finishes a blueprint or a frame.
        /// The game builds exactly one job for that, FinishFrame; the
        /// WorkGiverDef names (ConstructFinishFrames, ConstructDeliverResources*)
        /// are not JobDefs, so a prefix test on "Construct" matched no job at
        /// all and skipped the build it was meant to protect (fox #3706).
        /// </summary>
        public static bool IsConstructJob(string jobDefName)
        {
            return string.Equals(jobDefName, "FinishFrame", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The jobs that take a thing away rather than build it. A caller who
        /// names one of these on the colony's own blueprint or frame is asking
        /// for the plan to be destroyed, which is never what a prioritize call
        /// on a planned build means (fox #3706).
        /// </summary>
        private static readonly string[] RemovalJobs =
        {
            "Deconstruct",
            "DeconstructForBlueprint",
            "Uninstall",
        };

        /// <summary>
        /// NamesRemoval reports whether the caller's word names a removal job,
        /// by the menu's word ("deconstruct") or by the JobDef defName the
        /// game prints, so an explicit deconstruct of the colony's own
        /// blueprint is refused whichever form the run wrote.
        /// </summary>
        public static bool NamesRemoval(string requestedJob)
        {
            if (string.IsNullOrEmpty(requestedJob))
            {
                return false;
            }
            foreach (string jobDefName in RemovalJobs)
            {
                if (PriorityJobRules.Matches(requestedJob, jobDefName))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
