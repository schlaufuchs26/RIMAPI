using System;
using System.Collections.Generic;
using System.Linq;

namespace RIMAPI.Core
{
    /// <summary>
    /// The description of a request parameter or response field, written once
    /// for the names that mean the same thing wherever they appear (fox ticket
    /// #4313).
    /// </summary>
    /// <remarks>
    /// The OpenAPI document must say what a parameter does, not repeat its
    /// name: units, range, default, and the game's own term. The text lives
    /// next to the thing it describes (a [ParameterDescription] on the
    /// parameter or property, or the `=text` part of a [QueryParameters]
    /// entry) and this registry holds only the sentences that would otherwise
    /// be copied dozens of times: `map_id` on 59 routes, `label` on 109
    /// fields. A name that means different things in different places (`id`,
    /// `type`, `priority`) is not in here on purpose; those carry their own
    /// text.
    ///
    /// <see cref="IsUseful"/> is the rule the gate applies, in one place: a
    /// description needs a few words and may not restate the name.
    /// </remarks>
    public static class ParameterDescriptions
    {
        /// <summary>Fewer words than this is a restated name, not a description.</summary>
        public const int MinimumWords = 4;

        private static readonly Dictionary<string, string> Shared =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // Ids the routes and the reads both use.
                ["map_id"] = "The map's numeric id; 0 is the player's home map.",
                ["pawn_id"] = "The pawn's numeric id, as the pawn list reports it.",
                ["thing_id"] = "The thing's numeric id, as a map or storage read reports it.",
                ["building_id"] = "The building's numeric id, as the building list reports it.",
                ["bill_id"] = "The bill's numeric id, as the building's bill list reports it.",
                ["zone_id"] = "The zone's numeric id, as the zone list reports it.",
                ["bed_id"] = "The bed's numeric id, as the building list reports it.",
                ["item_id"] = "The item stack's numeric id, as a map read reports it.",
                ["tile_id"] = "The world tile's numeric id, as the world grid numbers them.",

                // Fields every read repeats.
                ["label"] = "The label the game shows for this thing in its UI.",
                ["def_name"] = "The def's internal name, e.g. \"Steel\".",
                ["description"] = "The description the game shows for this thing.",
                ["name"] = "The name the game shows for this thing.",
                ["count"] = "How many of the item this entry holds.",
                ["position"] = "The cell the thing stands on, as x and z map coordinates.",
                ["x"] = "The cell's x coordinate, in map cells.",
                ["y"] = "The cell's y coordinate, in map cells; the game's maps are flat.",
                ["z"] = "The cell's z coordinate, in map cells.",

                // Units and bounds a reader cannot guess.
                ["radius"] = "How far from the target cell the call reaches, in map cells.",
                ["limit"] = "The most entries the answer may hold.",
                ["since"] = "Only entries at or after this game tick are returned.",
                ["enabled"] = "Whether the setting is on.",
                ["only_researched"] = "When true, only recipes the colony has researched are listed.",
                ["quality"] = "The minimum quality a bill accepts, one of Awful through Legendary.",
                ["detailed"] = "When true, the answer carries the detailed shape instead of the summary one.",
                ["format"] = "The answer's format: html, json, markdown or openapi.",
            };

        /// <summary>
        /// The shared sentence for a wire name, or null when the name means
        /// different things in different places and needs its own text.
        /// </summary>
        public static string For(string wireName)
        {
            if (string.IsNullOrWhiteSpace(wireName))
                return null;

            string text;
            return Shared.TryGetValue(wireName, out text) ? text : null;
        }

        /// <summary>
        /// Whether a text is a description a reader can use: at least
        /// <see cref="MinimumWords"/> words, and not the name written out
        /// again ("Pawn id" for `pawn_id`).
        /// </summary>
        public static bool IsUseful(string description, string wireName)
        {
            if (string.IsNullOrWhiteSpace(description))
                return false;

            List<string> words = Words(description);
            if (words.Count < MinimumWords)
                return false;

            List<string> named = Words((wireName ?? string.Empty).Replace('_', ' '));
            if (named.Count == 0)
                return true;

            string normalized = string.Join(" ", words);
            string asName = string.Join(" ", named);
            if (normalized == asName)
                return false;

            // "Pawn id of the pawn" is still the name with filler around it.
            int at = normalized.IndexOf(asName, StringComparison.Ordinal);
            return at != 0 || words.Count - named.Count > 2;
        }

        /// <summary>Why a text fails the rule, for a gate that has to say it.</summary>
        public static string Why(string description, string wireName)
        {
            if (IsUseful(description, wireName))
                return null;
            if (string.IsNullOrWhiteSpace(description))
                return "has no description";
            if (Words(description).Count < MinimumWords)
                return "describes \"" + wireName + "\" in fewer than " + MinimumWords + " words";
            return "only restates the name \"" + wireName + "\"";
        }

        private static List<string> Words(string text) =>
            new string(
                (text ?? string.Empty)
                    .ToLowerInvariant()
                    .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
                    .ToArray()
            )
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word != "the" && word != "a" && word != "an")
                .ToList();
    }
}
