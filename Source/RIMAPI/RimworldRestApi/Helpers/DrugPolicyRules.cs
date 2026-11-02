using System.Collections.Generic;
using System.Globalization;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The wording and the bounds of a drug policy write (fox ticket #4212, gap
    /// 3). Every number here is the dialog's own slider range, so a caller cannot
    /// set a value the player could not.
    ///
    /// Plain C#, no game types, so the refusals are testable without a colony.
    /// </summary>
    public static class DrugPolicyRules
    {
        /// <summary>Widgets.FrequencyHorizontalSlider's bounds in the dialog.</summary>
        public const float MinDaysFrequency = 0.1f;
        public const float MaxDaysFrequency = 25f;

        /// <summary>The dialog's mood and joy sliders run from 1% to 100%.</summary>
        public const float MinLimit = 0.01f;

        public static bool DaysFrequencyValid(float days)
        {
            return days >= MinDaysFrequency && days <= MaxDaysFrequency;
        }

        /// <summary>
        /// A mood or joy limit is a fraction of a bar, the same 0..1 the pawn's
        /// needs read answers; the dialog's slider stops at 1%.
        /// </summary>
        public static bool LimitValid(float limit)
        {
            return limit >= MinLimit && limit <= 1f;
        }

        public static bool TakeToInventoryValid(int count, int max)
        {
            return count >= 0 && count <= max;
        }

        public static string DaysFrequencyProblem(float days)
        {
            return "the schedule runs from "
                + Text(MinDaysFrequency) + " to " + Text(MaxDaysFrequency)
                + " days, as the dialog's slider does; got " + Text(days);
        }

        public static string LimitProblem(string field, float limit)
        {
            return field + " runs from "
                + Text(MinLimit) + " to 1, a fraction of the bar; got " + Text(limit);
        }

        public static string TakeToInventoryProblem(int count, int max)
        {
            return "take_to_inventory runs from 0 to " + max + ", what one pawn can carry; got " + count;
        }

        public static string UnknownPolicy(int id, IEnumerable<int> known)
        {
            var ids = new List<string>();
            foreach (int knownId in known)
                ids.Add(knownId.ToString(CultureInfo.InvariantCulture));
            return "no drug policy with id " + id + ": the drug policies read prints the ids ("
                + string.Join(", ", ids) + ")";
        }

        public static string UnknownPawn(int id)
        {
            return "no pawn with id " + id + ": the colonists read prints the ids";
        }

        public static string NoDrugTracker(string pawnLabel)
        {
            return pawnLabel + " follows no drug policy: only humanlike pawns have one";
        }

        public static string UnknownDrug(string name)
        {
            return "no drug def called \"" + name + "\": the drug policy defs read lists the drugs";
        }

        public static string NotADrug(string name)
        {
            return name + " is not a drug, so a drug policy cannot hold it";
        }

        public static string NoEntryFor(string policyLabel, string drug)
        {
            return "\"" + policyLabel + "\" holds no line for " + drug
                + ": a policy has one line per drug at the moment it is created";
        }

        public static string NoLabel()
        {
            return "a new drug policy needs a label, e.g. \"No hard drugs\"";
        }

        public static string NoEntries()
        {
            return "the call names no entry to change: send entries with a drug and the fields to set";
        }

        public static string AlreadyFollowed(string pawnLabel, string label)
        {
            return pawnLabel + " already follows \"" + label + "\"";
        }

        public static string AlreadyDefault(string label)
        {
            return "\"" + label + "\" is already the default policy";
        }

        private static string Text(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
