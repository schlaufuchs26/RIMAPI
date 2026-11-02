using System.Collections.Generic;
using System.Globalization;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The wording of an apparel policy's refusals (fox ticket #4212, gap 2).
    /// The dialog's own sentences are used where the game has one; the rest name
    /// the fact and the ids that exist, because a caller that guessed a def name
    /// should learn which names there are.
    ///
    /// Plain C#, no game types: the service parses defs and enums, this file says
    /// what a refusal reads.
    /// </summary>
    public static class ApparelPolicyRules
    {
        /// <summary>The quality names the game knows, lowest first.</summary>
        public static readonly string[] QualityNames =
        {
            "Awful",
            "Poor",
            "Normal",
            "Good",
            "Excellent",
            "Masterwork",
            "Legendary",
        };

        /// <summary>
        /// Whether a hit-point range is one the game can hold: both ends inside
        /// 0..1 and min not above max, the same order ThingFilter checks when the
        /// dialog's sliders move.
        /// </summary>
        public static bool HitPointsValid(float min, float max)
        {
            return min >= 0f && max <= 1f && min <= max;
        }

        public static string HitPointsProblem(float min, float max)
        {
            // Invariant: the answer is read by a client, not by the OS locale, so
            // a German machine must not print 0,9 where the API says 0.9.
            return "the hit points run on the read's own scale, 0 to 1, with min not above max; got "
                + min.ToString("0.##", CultureInfo.InvariantCulture) + " to "
                + max.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static string QualityProblem(string name)
        {
            return "no quality called \"" + name + "\": the game knows "
                + string.Join(", ", QualityNames);
        }

        public static string UnknownPolicy(int id, IEnumerable<int> known)
        {
            var ids = new List<string>();
            foreach (int knownId in known)
                ids.Add(knownId.ToString());
            return "no apparel policy with id " + id + ": the outfits read prints the ids ("
                + string.Join(", ", ids) + ")";
        }

        public static string UnknownPawn(int id)
        {
            return "no pawn with id " + id + ": the colonists read prints the ids";
        }

        public static string NoOutfitTracker(string pawnLabel)
        {
            return pawnLabel + " wears no apparel policy: only humanlike pawns have one";
        }

        public static string UnknownThingDef(string defName)
        {
            return "no apparel def called \"" + defName + "\": the outfits read lists the def names it allows";
        }

        public static string NotApparel(string defName)
        {
            return defName + " is not apparel, so an apparel policy cannot hold it";
        }

        public static string UnknownSpecialFilter(string defName)
        {
            return "no special filter called \"" + defName + "\": the outfits read lists the ones it disallows";
        }

        public static string NothingToChange()
        {
            return "the call names nothing to change: send a label, a filter list, hit_points or quality";
        }

        public static string NoLabel()
        {
            return "a new apparel policy needs a label, e.g. \"Worker\"";
        }

        /// <summary>Assigning the policy the pawn already wears changes nothing.</summary>
        public static string AlreadyWorn(string pawnLabel, string label)
        {
            return pawnLabel + " already wears \"" + label + "\"";
        }
    }
}
