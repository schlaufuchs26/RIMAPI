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
        /// IsConstructJob reports whether a job builds a blueprint or a frame.
        /// The game builds two of them: FinishFrame for a plan that needs
        /// material, PlaceNoCostFrame for one that costs nothing (a sleeping
        /// spot). The WorkGiverDef names (ConstructFinishFrames,
        /// ConstructDeliverResources*) are not JobDefs, so a prefix test on
        /// "Construct" matched no job at all and skipped the build it was meant
        /// to protect (fox tickets #3706, #3708).
        /// </summary>
        public static bool IsConstructJob(string jobDefName)
        {
            return string.Equals(jobDefName, "FinishFrame", StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobDefName, "PlaceNoCostFrame", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// IsDeliverResourcesGiver reports whether a work giver carries material
        /// to a planned build rather than building it. The game has four of
        /// them: ConstructDeliverResourcesToBlueprints and
        /// ...ToFrames under Construction, and DeliverResourcesToBlueprints and
        /// ...ToFrames under Hauling, all named after the delivery they do
        /// (fox #3777).
        /// </summary>
        public static bool IsDeliverResourcesGiver(string giverDefName)
        {
            return !string.IsNullOrEmpty(giverDefName)
                && giverDefName.IndexOf("DeliverResources", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// IsDeliverResourcesJob reports whether a job hands material to a
        /// planned build: HaulToContainer puts the stack on the blueprint or
        /// frame, HaulToCell puts it on the cell the plan stands on. A
        /// blueprint that waits for its material is never built first, so this
        /// job is the build's first step (fox #3777).
        /// </summary>
        public static bool IsDeliverResourcesJob(string jobDefName)
        {
            return string.Equals(jobDefName, "HaulToContainer", StringComparison.OrdinalIgnoreCase)
                || string.Equals(jobDefName, "HaulToCell", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// IsBuildStepJob reports whether a job on a planned build is the build
        /// or the delivery that has to come first. The guard used to accept only
        /// the two build job defs, so a blueprint with no material on it was
        /// refused with "no work giver that builds any job" while the game's own
        /// first step, the delivery haul, sat in the offered list (fox #3777).
        /// The giver decides the delivery: a haul into some container that
        /// happens to stand on the same cell is not this build's step.
        /// </summary>
        public static bool IsBuildStepJob(string jobDefName, string giverDefName)
        {
            return IsConstructJob(jobDefName)
                || (IsDeliverResourcesJob(jobDefName) && IsDeliverResourcesGiver(giverDefName));
        }

        /// <summary>
        /// IsWayClearingJob reports whether a job is the game's own way-clearing
        /// deconstruct: the one a colonist does on his own when a building
        /// stands where a blueprint is planned. Its giver answers for any
        /// building once the call is forced, so an unnamed `prioritize` that
        /// let it win demolished a working solar generator; only a caller who
        /// names it gets it (fox #3706).
        /// </summary>
        public static bool IsWayClearingJob(string jobDefName)
        {
            return string.Equals(jobDefName, "DeconstructForBlueprint", StringComparison.OrdinalIgnoreCase);
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
