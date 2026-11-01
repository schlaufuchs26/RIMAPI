using System.Collections.Generic;

namespace RIMAPI.Services
{
    // The game-free half of a growth letter's answer (ticket #3714).
    //
    // A child's "became an adult" letter is a ChoiceLetter_GrowthMoment, which
    // is not a ChoiceLetter: it derives from LetterWithTimeout and opens a
    // Dialog_GrowthMomentChoices, a window with no DiaOptions at all. Its two
    // halves are a skill that gains a passion (passionGainsCount of them, one
    // in practice) and at most one trait, so `letter_answer` cannot press
    // anything and the letter stays on the stack, blocking every wait, until
    // the choice is made.
    //
    // The rules below decide which offered entry a caller named and how the
    // answer reads. The service does the game calls; nothing here touches the
    // game, so the matching and its wording are testable.
    public static class GrowthChoiceRules
    {
        // NoTraitWord is what a caller says when the child should gain no
        // trait: the letter's own "no trait" option. Only a letter that shows
        // that option accepts it.
        public const string NoTraitWord = "none";

        // IsNoTrait says whether the word means "no trait". The game draws the
        // option as "No trait", and a caller may also write "nothing".
        public static bool IsNoTrait(string wanted)
        {
            var word = (wanted ?? "").Trim().ToLowerInvariant();
            return word == NoTraitWord || word == "no trait" || word == "nothing" || word == "skip";
        }

        // Words splits what a caller named into the words it may hold: one
        // entry per offered choice, written either as an index or as part of
        // the label. A comma separates several (a letter that grants two
        // passions asks for two). Empty words are dropped, so "Shooting," and
        // "Shooting" read the same.
        public static List<string> Words(string wanted)
        {
            var words = new List<string>();
            if (string.IsNullOrEmpty(wanted))
                return words;
            foreach (var part in wanted.Split(','))
            {
                var word = part.Trim();
                if (word.Length > 0)
                    words.Add(word);
            }
            return words;
        }

        // Resolve finds the entry that wanted names in labels and answers its
        // 0-based index, or -1 with a problem saying why nothing matched. An
        // index the read prints comes first (so "1" is the second entry even
        // when a label happens to start with a digit), then an exact label,
        // then the one label that contains the word. Two labels that both
        // contain it are a problem, not a guess: a growth choice is permanent.
        public static int Resolve(IList<string> labels, string wanted, out string problem)
        {
            problem = null;
            var word = (wanted ?? "").Trim();
            if (labels == null || labels.Count == 0)
            {
                problem = "the letter offers no choice to pick";
                return -1;
            }
            if (word.Length == 0)
            {
                problem = "no choice was named";
                return -1;
            }

            int index;
            if (int.TryParse(word, out index))
            {
                if (index >= 0 && index < labels.Count)
                    return index;
                problem = "there is no choice " + word + ": the letter offers "
                    + OfferList(labels) + " (0-based)";
                return -1;
            }

            for (var i = 0; i < labels.Count; i++)
            {
                if (string.Equals((labels[i] ?? "").Trim(), word, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            var hits = new List<int>();
            for (var i = 0; i < labels.Count; i++)
            {
                if ((labels[i] ?? "").IndexOf(word, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    hits.Add(i);
            }
            if (hits.Count == 1)
                return hits[0];
            if (hits.Count > 1)
            {
                var names = new List<string>();
                foreach (var hit in hits)
                    names.Add(hit + " " + labels[hit]);
                problem = "\"" + word + "\" matches more than one offered choice: "
                    + string.Join(", ", names.ToArray());
                return -1;
            }

            problem = "\"" + word + "\" is not one of the offered choices: "
                + OfferList(labels);
            return -1;
        }

        // OfferList writes the choices the way the reads print them: the index
        // a caller passes back, then the label.
        public static string OfferList(IList<string> labels)
        {
            if (labels == null || labels.Count == 0)
                return "none";
            var parts = new List<string>();
            for (var i = 0; i < labels.Count; i++)
                parts.Add(i + " " + labels[i]);
            return string.Join(", ", parts.ToArray());
        }
    }
}
