using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Which precepts a founded colony's ideoligion may carry (fox ticket
    /// #3927). The fast start builds the ideo itself, so the precept set is a
    /// decision the caller makes instead of one the dice made. This is the
    /// part of that decision that needs no game: whether a pick can stand at
    /// all, and how one effect reads in an answer.
    ///
    /// Pure on purpose. IdeoSetup reads the defs and hands the facts in, so
    /// the rules are a unit test with no game, no defs and no map.
    /// </summary>
    public static class IdeoChoiceRules
    {
        /// <summary>One precept as the game's def describes it.</summary>
        public class PreceptFacts
        {
            public string Def;
            /// <summary>The "Issue: label" the ideoligion window shows.</summary>
            public string TipLabel;
            /// <summary>The issue's label, e.g. "Research"; two precepts of one issue cannot stand together.</summary>
            public string Issue;
            public List<string> RequiredMemes = new List<string>();
            public List<string> ConflictingMemes = new List<string>();
        }

        /// <summary>
        /// Problem returns the sentence that stops this pick, or null when the
        /// set can stand. It checks the three ways a hand-picked set breaks:
        /// two precepts of one issue, a precept whose meme the ideo does not
        /// carry, and a precept whose conflicting meme it does carry. The
        /// game would take the first two silently and then behave in ways
        /// nobody asked for, which is exactly what this ticket removes.
        /// </summary>
        public static string Problem(string meme, IEnumerable<string> ideoMemes, IReadOnlyList<PreceptFacts> picks)
        {
            if (picks == null || picks.Count == 0)
            {
                return null;
            }
            var byIssue = new Dictionary<string, PreceptFacts>(StringComparer.OrdinalIgnoreCase);
            foreach (PreceptFacts pick in picks)
            {
                if (string.IsNullOrEmpty(pick.Issue))
                {
                    continue;
                }
                if (byIssue.TryGetValue(pick.Issue, out PreceptFacts other))
                {
                    return $"two precepts of one issue cannot stand together: {other.TipLabel} and {pick.TipLabel} " +
                        $"both decide the {pick.Issue} issue; name one of them";
                }
                byIssue[pick.Issue] = pick;
            }

            var memes = new HashSet<string>(
                (ideoMemes ?? Enumerable.Empty<string>()).Where(m => !string.IsNullOrEmpty(m)),
                StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(meme))
            {
                memes.Add(meme);
            }
            foreach (PreceptFacts pick in picks)
            {
                List<string> missing = pick.RequiredMemes
                    .Where(m => !string.IsNullOrEmpty(m) && !memes.Contains(m))
                    .ToList();
                if (missing.Count > 0)
                {
                    return $"{pick.TipLabel} needs the meme {string.Join(" and ", missing)}, " +
                        $"and this ideo carries only {string.Join(", ", memes.OrderBy(m => m))}";
                }
                List<string> conflicts = pick.ConflictingMemes
                    .Where(m => !string.IsNullOrEmpty(m) && memes.Contains(m))
                    .ToList();
                if (conflicts.Count > 0)
                {
                    return $"{pick.TipLabel} conflicts with the meme {string.Join(" and ", conflicts)}";
                }
            }
            return null;
        }

        /// <summary>
        /// StatFactor reads a multiplicative effect: "research speed x1.5".
        /// The value is what the game's own stat factor holds, so a reader can
        /// compare it with the same stat on another precept.
        /// </summary>
        public static string StatFactor(string statLabel, float value)
        {
            return $"{statLabel} x{Number(value)}";
        }

        /// <summary>
        /// StatOffset reads an additive effect: "mining yield +0.1". The game
        /// stores offsets in the stat's own unit; the sign is spelled out
        /// because "+0.1" and "0.1" read differently in a decision.
        /// </summary>
        public static string StatOffset(string statLabel, float value)
        {
            string sign = value > 0 ? "+" : "";
            return $"{statLabel} {sign}{Number(value)}";
        }

        /// <summary>
        /// MoodRange reads a thought's stages as the spread a pawn can feel:
        /// "mood -4..+9". One stage is one number, and a thought with no mood
        /// effect at all is not worth a line.
        /// </summary>
        public static string MoodRange(IEnumerable<float> stageMoods)
        {
            List<float> moods = (stageMoods ?? Enumerable.Empty<float>()).ToList();
            if (moods.Count == 0)
            {
                return null;
            }
            float low = moods.Min();
            float high = moods.Max();
            if (low == 0f && high == 0f)
            {
                return null;
            }
            if (low == high)
            {
                return $"mood {(high > 0 ? "+" : "")}{Number(high)}";
            }
            return $"mood {(low > 0 ? "+" : "")}{Number(low)}..{(high > 0 ? "+" : "")}{Number(high)}";
        }

        /// <summary>Number renders a game value without a trailing ".0" and with at most two decimals.</summary>
        public static string Number(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
