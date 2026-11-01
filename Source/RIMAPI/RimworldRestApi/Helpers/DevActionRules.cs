using System;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The request shape behind `dev` (fox ticket #3757): a check that needs a
    /// state no save holds (a pawn with an infection, a berserk colonist, a
    /// raid, a job that has to finish) can be built in the debug instance
    /// instead of hunting for a save that happens to carry it.
    ///
    /// Every op is refused rather than clamped when its number is off: a
    /// clamped damage value kills a pawn the caller wanted wounded, and a
    /// clamped tick count reports a span the game never ran. No RimWorld type
    /// appears here, so Source/RIMAPI.Tests states the rules outside the game
    /// (see DevActionRulesTests).
    /// </summary>
    public static class DevActionRules
    {
        /// <summary>One hit above a pawn's whole body kills him; past this a number is a typo.</summary>
        public const float MaxDamage = 1000f;

        /// <summary>Items or animals one call may place.</summary>
        public const int MaxSpawn = 500;

        /// <summary>The game's own skill ceiling.</summary>
        public const int MaxSkillLevel = 20;

        /// <summary>
        /// Ticks one call may run by hand: 20,000 ticks are eight in-game hours
        /// at roughly 0.3 ms per tick, so a call stays inside the tool's 120 s
        /// limit. A longer span is several calls.
        /// </summary>
        public const int MaxTicks = 20000;

        /// <summary>Silver one `wealth` call may add; the game counts silver at face value.</summary>
        public const float MaxWealth = 100000f;

        /// <summary>A hediff severity past 100 is past every disease the game defines.</summary>
        public const float MaxSeverity = 100f;

        /// <summary>Every op the route knows, in the order the answer lists them.</summary>
        public static readonly string[] Ops =
        {
            "damage",
            "heal",
            "hediff",
            "infection",
            "spawn",
            "spawn_animal",
            "mental",
            "skill",
            "trait",
            "incident",
            "quest",
            "wealth",
            "ticks",
        };

        /// <summary>True for an op this route implements.</summary>
        public static bool IsOp(string op)
        {
            if (string.IsNullOrEmpty(op)) return false;
            foreach (string known in Ops)
            {
                if (known == op) return true;
            }
            return false;
        }

        /// <summary>The op list, for a refusal that names what is available.</summary>
        public static string OpList()
        {
            return string.Join(", ", Ops);
        }

        /// <summary>True for the ops that act on one pawn.</summary>
        public static bool NeedsPawn(string op)
        {
            switch (op)
            {
                case "damage":
                case "heal":
                case "hediff":
                case "infection":
                case "mental":
                case "skill":
                case "trait":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>True for the ops whose `name` is a defName the game has to know.</summary>
        public static bool NeedsName(string op)
        {
            switch (op)
            {
                case "hediff":
                case "spawn":
                case "spawn_animal":
                case "mental":
                case "skill":
                case "trait":
                case "incident":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// The refusal for a request the game cannot run, or null when it can.
        /// It names the field the op needs, so a caller fixes the call instead
        /// of guessing.
        /// </summary>
        public static string Problem(
            string op,
            int? pawnId,
            string name,
            float? amount,
            int? level,
            int? count,
            int? ticks
        )
        {
            if (!IsOp(op))
            {
                return "unknown op "
                    + (op == null ? "(none)" : "\"" + op + "\"")
                    + "; use one of "
                    + OpList();
            }
            if (NeedsPawn(op) && (pawnId == null || pawnId <= 0))
            {
                return op
                    + " needs the pawn's id in `id`; the `colonists` read prints it";
            }
            if (NeedsName(op) && string.IsNullOrEmpty(name))
            {
                return op + " needs `name`, the defName the game knows (see `defs`)";
            }
            switch (op)
            {
                case "damage":
                    if (amount == null || amount <= 0)
                    {
                        return "damage needs `amount`, the damage to deal in hit points (above 0)";
                    }
                    if (amount > MaxDamage)
                    {
                        return "damage of "
                            + amount
                            + " is past the "
                            + MaxDamage
                            + " cap; one hit past a pawn's whole body kills him, so a bigger number is a typo";
                    }
                    break;
                case "hediff":
                case "infection":
                    if (amount != null && (amount < 0 || amount > MaxSeverity))
                    {
                        return "the severity must be 0 to "
                            + MaxSeverity
                            + ", not "
                            + amount
                            + "; past 100 no disease the game defines reaches";
                    }
                    break;
                case "spawn":
                case "spawn_animal":
                    if (count != null && (count < 1 || count > MaxSpawn))
                    {
                        return "count must be 1 to "
                            + MaxSpawn
                            + ", not "
                            + count
                            + "; place more in several calls";
                    }
                    break;
                case "skill":
                    if (level == null)
                    {
                        return "skill needs `level`, 0 to " + MaxSkillLevel;
                    }
                    if (level < 0 || level > MaxSkillLevel)
                    {
                        return "a skill level runs 0 to "
                            + MaxSkillLevel
                            + ", not "
                            + level;
                    }
                    break;
                case "wealth":
                    if (amount == null || amount < 0)
                    {
                        return "wealth needs `amount`, the silver value to add (0 or more)";
                    }
                    if (amount > MaxWealth)
                    {
                        return "wealth of "
                            + amount
                            + " is past the "
                            + MaxWealth
                            + " cap; add more in several calls";
                    }
                    break;
                case "ticks":
                    if (ticks == null || ticks <= 0)
                    {
                        return "ticks needs `ticks`, the number of ticks to run (above 0)";
                    }
                    if (ticks > MaxTicks)
                    {
                        return "ticks of "
                            + ticks
                            + " is past the "
                            + MaxTicks
                            + " cap (eight in-game hours per call); run the rest in a second call";
                    }
                    break;
            }
            return null;
        }

        /// <summary>The spawn count a request means: one when it names none.</summary>
        public static int SpawnCount(int? count)
        {
            if (count == null || count < 1) return 1;
            return count.Value > MaxSpawn ? MaxSpawn : count.Value;
        }

        /// <summary>
        /// The severity a hediff starts at: the request's, else the def's own
        /// default, which an infection puts at 0.2 and a disease at its own
        /// starting point.
        /// </summary>
        public static float? Severity(float? amount)
        {
            if (amount == null) return null;
            return amount < 0f ? 0f : amount;
        }
    }
}
