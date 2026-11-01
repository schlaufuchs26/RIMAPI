using System;
using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The word half of `allowed_area` (fox ticket #3645): a caller names an
    /// area the way the game's own dropdown does ("Home", the label of a named
    /// area, or Unrestricted), while the map holds Area objects whose label
    /// moves with the install's language and whose type name does not. No
    /// RimWorld type appears here, so RIMAPI.Tests covers the matching outside
    /// the game (see AllowedAreaRulesTests).
    /// </summary>
    public static class AllowedAreaRules
    {
        /// <summary>
        /// The word that lifts a restriction. It is the game's own word for
        /// "no area", so an answer can use it without translating.
        /// </summary>
        public const string Unrestricted = "Unrestricted";

        /// <summary>
        /// IsUnrestricted says whether the caller asked to lift the
        /// restriction instead of naming an area. An empty word is not one of
        /// these: an omitted area must never quietly unrestrict a pawn that a
        /// raid is supposed to find behind walls.
        /// </summary>
        public static bool IsUnrestricted(string word)
        {
            string w = Normalize(word);
            return w == "unrestricted" || w == "none" || w == "anywhere";
        }

        /// <summary>
        /// IndexOf finds the area a caller named: its label, its type name
        /// (Area_Home), or the type name the game would build from the word
        /// (Home → Area_Home). Case and surrounding space do not matter. -1
        /// means no area of the given list matches.
        /// </summary>
        public static int IndexOf(string word, IList<string> labels, IList<string> typeNames)
        {
            if (labels == null || typeNames == null || labels.Count != typeNames.Count)
            {
                throw new ArgumentException("IndexOf needs one type name per label");
            }
            string w = Normalize(word);
            if (w.Length == 0) return -1;
            string asType = Normalize("Area_" + word);
            for (int i = 0; i < labels.Count; i++)
            {
                if (Normalize(labels[i]) == w) return i;
                if (Normalize(typeNames[i]) == w) return i;
                if (Normalize(typeNames[i]) == asType) return i;
            }
            return -1;
        }

        /// <summary>
        /// OptionsText lists what a caller may name, in the game's own words,
        /// so a wrong label answers with the right ones instead of only
        /// failing.
        /// </summary>
        public static string OptionsText(IList<string> labels)
        {
            if (labels == null || labels.Count == 0) return "none";
            return string.Join(", ", labels);
        }

        private static string Normalize(string word)
        {
            return (word ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
