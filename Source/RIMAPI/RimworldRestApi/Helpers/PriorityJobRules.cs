using System;
using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The word half of `prioritize` (fox ticket #3626): a caller names the job
    /// the way the game's right-click menu does ("tame", "harvest", "mine"),
    /// while the game builds a JobDef with its own defName (Tame, Harvest,
    /// CutPlant). No RimWorld type appears here, so RIMAPI.Tests covers the
    /// matching outside the game (see PriorityJobRulesTests).
    /// </summary>
    public static class PriorityJobRules
    {
        /// <summary>
        /// The words a caller uses and the defNames the game's work givers
        /// build for them. One word can name two jobs: a haul is HaulToCell on
        /// the ground and HaulToContainer into a shelf, and "harvest" is the
        /// growing work's Harvest while a tree falls to CutPlant.
        /// </summary>
        private static readonly Dictionary<string, string[]> Words =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "tame", new[] { "Tame" } },
                { "rescue", new[] { "Rescue" } },
                { "haul", new[] { "HaulToCell", "HaulToContainer" } },
                { "butcher", new[] { "DoBill" } },
                { "cook", new[] { "DoBill" } },
                { "craft", new[] { "DoBill" } },
                { "mine", new[] { "Mine" } },
                // Plain Deconstruct only. DeconstructForBlueprint is the game's
                // own way-clearing job and its giver answers for any building
                // with forced=true, so naming it here queued it on a working
                // generator (fox #3706). A run that wants it names the defName.
                { "deconstruct", new[] { "Deconstruct" } },
                { "tend", new[] { "TendPatient", "TendSelf" } },
                { "equip", new[] { "Equip" } },
                { "sow", new[] { "Sow" } },
                { "harvest", new[] { "Harvest" } },
                { "chop", new[] { "CutPlant" } },
                { "cut", new[] { "CutPlant" } },
                { "clean", new[] { "Clean" } },
                { "repair", new[] { "Repair" } },
                // The job that finishes a blueprint or a frame is FinishFrame,
                // and a plan that costs nothing is built by PlaceNoCostFrame;
                // the names in the WorkGiverDef files (ConstructFinishFrames and
                // the two delivery givers) are not JobDefs, so a table keyed on
                // them matched nothing and "build" fell through to the refusal
                // (fox #3706). A delivery is a haul, so it keeps its own word.
                { "build", new[] { "FinishFrame", "PlaceNoCostFrame" } },
                // "construct" is the work type's own name, which is what a
                // caller reads in the answer and then names back; the build
                // under it is the same jobs (fox tickets #3708, #3706).
                { "construct", new[] { "FinishFrame", "PlaceNoCostFrame" } },
                { "firefight", new[] { "BeatFire" } },
                { "extinguish", new[] { "BeatFire" } },
                { "arrest", new[] { "Arrest", "Capture" } },
                { "capture", new[] { "Capture" } },
                { "feed", new[] { "FeedPatient" } },
                { "refuel", new[] { "Refuel" } },
                { "research", new[] { "Research" } },
            };

        /// <summary>
        /// Normalize turns a caller's word into the form the table holds:
        /// trimmed, lower case, and without the spaces and hyphens a person
        /// writes into a two-word job, so "Fire Fight" and "fire-fight" both
        /// reach BeatFire.
        /// </summary>
        public static string Normalize(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return string.Empty;
            }
            return word.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("-", string.Empty);
        }

        /// <summary>
        /// Matches reports whether the job the game built is the one the caller
        /// named. A defName counts as itself, so "TendPatient" works, and the
        /// menu's word counts through the table.
        /// </summary>
        public static bool Matches(string requested, string jobDefName)
        {
            if (string.IsNullOrEmpty(requested) || string.IsNullOrEmpty(jobDefName))
            {
                return false;
            }
            if (string.Equals(requested.Trim(), jobDefName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            string[] defNames;
            if (!Words.TryGetValue(Normalize(requested), out defNames))
            {
                return false;
            }
            foreach (string defName in defNames)
            {
                if (string.Equals(defName, jobDefName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// OfferList is what the float menu would show a player at that target:
        /// the jobs the pawn's own work givers build there, deduplicated,
        /// ordered as the givers were scanned and capped, because an answer a
        /// run reads has to stay one line.
        /// </summary>
        public static List<string> OfferList(IEnumerable<string> defNames, int cap = 6)
        {
            List<string> offers = new List<string>();
            if (defNames == null)
            {
                return offers;
            }
            foreach (string defName in defNames)
            {
                if (string.IsNullOrEmpty(defName) || offers.Contains(defName))
                {
                    continue;
                }
                offers.Add(defName);
                if (offers.Count >= cap)
                {
                    break;
                }
            }
            return offers;
        }
    }
}
