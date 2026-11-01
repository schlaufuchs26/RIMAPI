namespace RIMAPI.Helpers
{
    // The place a ritual is held at is what the Begin dialog asks the player
    // for, and the start asks the ritual's own target filter first. That filter
    // measures from an organizer, so a start that names no pawn and whose
    // ritual has no role holder has nothing to ask with and finds no place at
    // all (fox #3676). The refusal then has to name `pawn_id`, because the cell
    // the `rituals` read already prints was not what was missing.
    //
    // Kept free of RimWorld types so Source/RIMAPI.Tests can state both
    // sentences.
    public static class RitualTargetRefusal
    {
        /// <summary>
        /// The reason a start gives when it found no place. hasOrganizer is
        /// false when the call passed no pawn_id and no role holder filled one.
        /// </summary>
        public static string Text(bool hasOrganizer)
        {
            if (!hasOrganizer)
            {
                return "no target: the ritual's target filter needs an organizer, and this start has "
                    + "none (no pawn_id, and no role holder fills one); pass pawn_id to name one, or "
                    + "the place itself (x1/z1 or target_id) as the Begin dialog asks";
            }
            return "no target: pass the cell the ritual is held at (x1/z1) or the building "
                + "or spot (target_id), as the Begin dialog asks";
        }
    }
}
