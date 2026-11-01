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
            "spawn_pawn",
            "mental",
            "skill",
            "trait",
            "incident",
            "quest",
            "wealth",
            "ticks",
            "debug_actions",
            "debug_action",
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
                case "spawn_pawn":
                case "mental":
                case "skill":
                case "trait":
                case "incident":
                case "debug_action":
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
                case "spawn_pawn":
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

        // ---- the dev-action menu (ticket #3799) ---------------------------

        /// <summary>Paths one `debug_actions` call prints; the menu holds a few thousand.</summary>
        public const int MaxListed = 500;

        /// <summary>
        /// Entries one walk may visit before it gives up: a guard against a
        /// sub-menu that generates entries forever, not a routine stop. The
        /// whole menu held 60,456 entries and walked in 8.2 s on 2026-11-01
        /// (ticket #3855), so a filtered walk reaches every entry a game has;
        /// the listing stops earlier only when it holds the matches it prints.
        /// </summary>
        public const int MaxNodes = 120000;

        /// <summary>
        /// A dev-action path with one separator and no leading or trailing slash,
        /// so a caller may write Actions\Spawning\WildMan, Actions/Spawning/WildMan
        /// or just Spawning/WildMan and all three name the same entry.
        /// </summary>
        public static string ActionPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string trimmed = path.Replace('\\', '/').Trim();
            while (trimmed.StartsWith("/", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1);
            }
            while (trimmed.EndsWith("/", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }
            return trimmed;
        }

        /// <summary>
        /// True when a menu entry's own path is the one the call names, or ends
        /// with it: the entries sit under the Actions root, and a caller should
        /// not have to type it.
        /// </summary>
        public static bool MatchesPath(string nodePath, string wanted)
        {
            string node = ActionPath(nodePath);
            string want = ActionPath(wanted);
            if (want.Length == 0) return false;
            if (string.Equals(node, want, StringComparison.OrdinalIgnoreCase)) return true;
            return node.EndsWith("/" + want, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>How many paths a listing prints: the request's, else 100, never past MaxListed.</summary>
        public static int ListLimit(int? count)
        {
            if (count == null || count < 1) return 100;
            return count.Value > MaxListed ? MaxListed : count.Value;
        }

        /// <summary>
        /// True when a menu entry belongs in a filtered listing: the filter is a
        /// substring of its path, either separator and either case. An empty
        /// filter matches every entry.
        /// </summary>
        public static bool MatchesFilter(string nodePath, string filter)
        {
            string want = ActionPath(filter);
            if (want.Length == 0) return true;
            string node = ActionPath(nodePath);
            return node.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
