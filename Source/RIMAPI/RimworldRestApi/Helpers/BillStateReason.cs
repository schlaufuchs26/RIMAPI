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
            // Skill is the recipe's skill requirement, null when it has none;
            // SkilledColonists is how many free colonists meet it, BestSkill
            // the highest level any of them has.
            public string Skill;
            public int MinSkill;
            public int BestSkill;
            public int SkilledColonists;
        }

        public struct State
        {
            public string Name;
            public string Reason;
        }

        // Describe answers the state and, when the bill is not simply running,
        // the sentence that says what to fix. The order is the order a player
        // checks: the switch they flipped, the game's own pause, the target,
        // the material, the worker.
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
            if (f.MissingIngredients != null && f.MissingIngredients.Count > 0)
            {
                return new State { Name = MissingIngredients, Reason = MissingText(f) };
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
    }
}
