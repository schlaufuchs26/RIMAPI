using System;
using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The rules the game's own zone and area dialogs enforce (fox ticket
    /// #4316): `Dialog_Rename&lt;T&gt;` refuses an empty name and stops the text
    /// field at 28 characters, and both `Dialog_RenameZone` and
    /// `Dialog_RenameArea` refuse a name another zone or area already carries.
    /// A route that writes a name has to refuse the same names, or the API
    /// could do something no player can do. No RimWorld type appears here, so
    /// RIMAPI.Tests covers it outside the game (ZoneEditRulesTests).
    /// </summary>
    public static class ZoneEditRules
    {
        /// <summary>
        /// The game's rename field (`Dialog_Rename&lt;T&gt;.MaxNameLength`) stops
        /// accepting input once the name reaches 28 characters, so 28 is the
        /// longest name a player can type.
        /// </summary>
        public const int MaxLabelLength = 28;

        /// <summary>
        /// LabelRefusal answers the game's refusal for a name, or null when the
        /// dialog would accept it. The comparison is ordinal: the game compares
        /// the stored label to the typed name with ==, so "Garden" and "garden"
        /// are two names there and two names here.
        /// </summary>
        public static string LabelRefusal(string label, IEnumerable<string> otherLabels)
        {
            if (label == null || label.Length == 0)
            {
                return "the name is empty; the game's rename dialog refuses an empty name";
            }

            if (label.Length > MaxLabelLength)
            {
                return "the name is "
                    + label.Length
                    + " characters long; the game's rename field stops at "
                    + MaxLabelLength;
            }

            if (otherLabels != null)
            {
                foreach (string taken in otherLabels)
                {
                    if (string.Equals(taken, label, StringComparison.Ordinal))
                    {
                        return "the name \""
                            + label
                            + "\" is already in use; the game's rename dialog refuses a name in use";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// GrowingRefusal answers why a growing-zone update asks for nothing,
        /// or null when the body names at least one setting. A call that
        /// changes nothing would answer "success" while the zone stayed as it
        /// was, which is the shape of lie this API avoids elsewhere.
        /// </summary>
        public static string GrowingRefusal(string plantDef, bool? allowSow, bool? allowCut)
        {
            if (allowSow == null && allowCut == null && string.IsNullOrWhiteSpace(plantDef))
            {
                return "the body names nothing to change: send plant_def, allow_sow or allow_cut";
            }

            return null;
        }

        /// <summary>
        /// PlantRefusal answers why a requested crop is not one the zone's Plant
        /// button offers, or null when the game would list it. The list is the
        /// game's own (PlantUtility.ValidPlantTypesForGrowers plus the research
        /// gate), so a caller sees the same choices the player sees.
        /// </summary>
        public static string PlantRefusal(string plantDef, IList<string> selectableDefNames)
        {
            if (string.IsNullOrWhiteSpace(plantDef))
            {
                return "plant_def is required: the defName of the crop the zone should grow";
            }

            if (selectableDefNames != null)
            {
                foreach (string defName in selectableDefNames)
                {
                    if (string.Equals(defName, plantDef, StringComparison.Ordinal))
                    {
                        return null;
                    }
                }
            }

            return "\""
                + plantDef
                + "\" is not a plant this zone can sow; GET /api/v1/map/zone/growing/plants lists the ones the game's Plant button offers here";
        }
    }
}
