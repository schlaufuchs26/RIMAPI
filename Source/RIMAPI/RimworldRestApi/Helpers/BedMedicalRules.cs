namespace RIMAPI.Helpers
{
    /// <summary>
    /// A bed's own medical setting (fox ticket #4235). The health tab's
    /// medicine policy is the pawn's (Pawn_PlayerSettings.medCare) and has its
    /// route; the game holds no per-bed medicine policy at all. What a bed
    /// carries itself is one flag, Building_Bed.Medical, the gizmo that turns
    /// a bed into a hospital bed. The building read answered it as
    /// BedDto.Medical and nothing set it, so a run could read that its
    /// hospital stood full of normal beds and not fix it.
    ///
    /// No RimWorld type appears here, so Source/RIMAPI.Tests states the rules
    /// outside the game (see BedMedicalRulesTests).
    /// </summary>
    public static class BedMedicalRules
    {
        /// <summary>
        /// The refusal for a building that is no bed: a wall, a workbench or a
        /// bed frame has no medical flag to set.
        /// </summary>
        public static string NotABed(string label, int id, string defName)
        {
            return label + " (id " + id + ") is not a bed: it is a " + defName
                + ", and only a bed has a medical setting";
        }

        /// <summary>
        /// The refusal for a bed whose def has no medical toggle (an animal
        /// bed, a sleeping spot). The game's own setter drops the flag for such
        /// a def without a word, so the call refuses instead of reporting a
        /// hospital bed the game does not hold.
        /// </summary>
        public static string NoMedicalSetting(string label, int id, string defName)
        {
            return label + " (id " + id + ") cannot be a medical bed: its def " + defName
                + " has no medical setting, and the game's bed tab shows no toggle for it";
        }

        /// <summary>
        /// True when the call may write the flag: the game lets any bed drop
        /// the flag, and only a bed whose def allows it take the flag.
        /// </summary>
        public static bool CanTake(bool defAllowsMedical, bool wanted)
        {
            return !wanted || defAllowsMedical;
        }

        /// <summary>True when the bed reads what the call asked for.</summary>
        public static bool Took(bool wanted, bool reads)
        {
            return wanted == reads;
        }

        /// <summary>
        /// The refusal for a write the game did not keep, naming what the bed
        /// reads instead: "the call succeeded" is not "the bed is medical now".
        /// </summary>
        public static string DidNotTake(string label, int id, bool wanted, bool reads)
        {
            return label + " (id " + id + ") did not take the medical setting: it reads "
                + Describe(reads) + ", the call asked for " + Describe(wanted);
        }

        /// <summary>The setting in words, for a refusal or a summary line.</summary>
        public static string Describe(bool medical)
        {
            return medical ? "a medical bed" : "a normal bed";
        }
    }
}
