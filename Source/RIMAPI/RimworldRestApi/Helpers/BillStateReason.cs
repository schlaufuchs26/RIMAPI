using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    // What a bill is doing, in the game's own terms (ticket #3306). The bills
    // read named the recipe and the target but never said whether the bill can
    // run, so a caller could not tell a bill waiting for material from one
    // nobody is assigned to: a tailor bench sat on "make parka, do until you
    // have 3" for three days with no leather in the colony, and the answer
    // looked the same as a bill that was simply working.
    //
    // Game-free: the caller gathers the facts (the game's IsSatisfied, the
    // ingredients it is short of, the recipe's skill requirement and how many
    // colonists meet it) and this decides the wording, so the states are
    // tested without the game.
    public static class BillStateReason
    {
        // The game's own words, as the bill UI uses them.
        public const string Running = "running";
        public const string Suspended = "suspended";
        public const string Paused = "paused";
        public const string Satisfied = "target met";
        public const string MissingIngredients = "missing ingredients";
        public const string NoWorker = "no worker with the skill";
        // NoColonistAssigned is the bench's own work type with nobody on it
        // (ticket #3362): the material and the skill are there, but no free
        // colonist has that type switched on, so the bill never gets a job.
        public const string NoColonistAssigned = "no colonist assigned";
        // NoPower is the bench's own power: a machine that draws power and has
        // none runs no bill at all, and the bill read said "running" for four
        // game hours on a stove no conduit reached (fox ticket #3762).
        public const string NoPower = "no power";

        public struct Facts
        {
            // Suspended is the player's own switch; Paused is the game's.
            public bool Suspended;
            public bool Paused;
            // TargetMet is Bill.IsSatisfied(); TargetText says what the count
            // is, e.g. "3 of 10 parkas in the colony", or null.
            public bool TargetMet;
            public string TargetText;
            // MissingIngredients names the ingredients of which the colony has
            // none at all, in the recipe's own labels.
            public IList<string> MissingIngredients;
            // FilterExcluded names what the colony has in stock and the recipe
            // takes, but the bill's own ingredient filter drops (fox ticket
            // #3931). Without it "nothing to work with: no twisted meat" reads
            // the same whether the colony is out of twisted meat or the filter
            // keeps it out, and the fixes are opposite.
            public IList<ExcludedMaterial> FilterExcluded;
            // Skill is the recipe's skill requirement, null when it has none;
            // SkilledColonists is how many free colonists meet it, BestSkill
            // the highest level any of them has.
            public string Skill;
            public int MinSkill;
            public int BestSkill;
            public int SkilledColonists;
            // WorkTypes are the defNames of the work types that can run this
            // bench (a stove is Cooking, a machining table Smithing), null
            // when the game names none; AssignedColonists is how many free
            // colonists have one of them switched on.
            public IList<string> WorkTypes;
            public int AssignedColonists;
            // PowerFault is the sentence that says why the bench has no power
            // (PowerFault.Describe), null when the bench is powered or draws
            // none. It is the bench's fact, so every bill on that bench carries
            // it.
            public string PowerFault;
        }

        public struct State
        {
            public string Name;
            public string Reason;
        }

        // ExcludedMaterial is one thing the colony has in stock that the bill's
        // own filter keeps out of the recipe. DefName is the form the filter
        // read and `bill op=set allow=` use, so the line names the lever and
        // not only the label.
        public struct ExcludedMaterial
        {
            public string DefName;
            public int Count;
        }

        // Describe answers the state and, when the bill is not simply running,
        // the sentence that says what to fix. The order is the order a player
        // checks: the switch they flipped, the game's own pause, the target,
        // the material, the work type, the worker.
        public static State Describe(Facts f)
        {
            if (f.Suspended)
            {
                return new State { Name = Suspended, Reason = Join("suspended by you", MissingText(f)) };
            }
            if (f.Paused)
            {
                return new State { Name = Paused, Reason = "paused" };
            }
            if (f.TargetMet)
            {
                return new State
                {
                    Name = Satisfied,
                    Reason = f.TargetText == null ? "you have enough" : f.TargetText,
                };
            }
            // A bench without power runs nothing, whatever the material says:
            // the fault comes before the ingredients because it blocks every
            // bill on that bench (fox ticket #3762).
            if (f.PowerFault != null)
            {
                return new State { Name = NoPower, Reason = f.PowerFault };
            }
            if (f.MissingIngredients != null && f.MissingIngredients.Count > 0)
            {
                return new State
                {
                    Name = MissingIngredients,
                    Reason = Join(MissingText(f), FilterExcludedText(f)),
                };
            }
            // The assignment comes before the skill: a bench whose work type
            // nobody works gives no job to anybody, however good they are.
            if (f.WorkTypes != null && f.WorkTypes.Count > 0 && f.AssignedColonists == 0)
            {
                return new State
                {
                    Name = NoColonistAssigned,
                    Reason = "nobody is assigned to " + string.Join(" or ", f.WorkTypes),
                };
            }
            if (f.Skill != null && f.SkilledColonists == 0)
            {
                return new State
                {
                    Name = NoWorker,
                    Reason = string.Format(
                        "needs {0} {1}; the best colonist has {2}",
                        f.Skill, f.MinSkill, f.BestSkill),
                };
            }
            return new State { Name = Running, Reason = null };
        }

        // MissingText names what the colony has none of, which is the one
        // material fact that decides whether hauling helps.
        private static string MissingText(Facts f)
        {
            if (f.MissingIngredients == null || f.MissingIngredients.Count == 0)
            {
                return null;
            }
            return "nothing to work with: no " + string.Join(", no ", f.MissingIngredients);
        }

        private static string Join(string first, string second)
        {
            return second == null ? first : first + "; " + second;
        }

        // FilterExcludedText names what the filter keeps out of a bill that has
        // no material (fox ticket #3931): the defName the filter read and
        // `bill op=set allow=` take, with how much of it sits in the colony, so
        // one line says whether hauling helps or the filter has to change.
        private static string FilterExcludedText(Facts f)
        {
            if (f.FilterExcluded == null || f.FilterExcluded.Count == 0)
            {
                return null;
            }

            var parts = new List<string>();
            int named = 0;
            foreach (ExcludedMaterial material in f.FilterExcluded)
            {
                if (named == FilterExcludedCap)
                {
                    parts.Add(string.Format("+{0} more", f.FilterExcluded.Count - named));
                    break;
                }
                parts.Add(string.Format("{0} ({1} in the colony)", material.DefName, material.Count));
                named++;
            }
            return "the filter keeps out " + string.Join(", ", parts);
        }

        // FilterExcludedCap keeps the sentence short: a broad filter can drop
        // dozens of things the colony has, and the rest is counted.
        private const int FilterExcludedCap = 3;
    }
}
