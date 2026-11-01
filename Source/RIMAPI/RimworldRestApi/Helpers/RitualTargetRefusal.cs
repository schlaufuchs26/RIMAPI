namespace RIMAPI.Helpers
{
    // The place a ritual is held at is what the Begin dialog asks the player
    // for, and the start asks the ritual's own target filter first. That filter
    // measures from an organizer, so a start with no pawn at all finds no place
    // to ask about (fox #3676). A start that names no pawn otherwise takes the
    // ritual's role holder, else the colony's first colonist, as the read does
    // (#3675), so this case is an empty colony (fox #3804). The refusal then
    // says that, instead of naming `pawn_id`: the field cannot supply an
    // organizer a colony does not have.
    //
    // Kept free of RimWorld types so Source/RIMAPI.Tests can state both
    // sentences.
    public static class RitualTargetRefusal
    {
        /// <summary>
        /// The reason a start gives when it found no place. hasOrganizer is
        /// false when the call passed no pawn_id and the colony had no colonist
        /// to fall back to.
        /// </summary>
        public static string Text(bool hasOrganizer)
        {
            if (!hasOrganizer)
            {
                return "no target: this colony has no colonist at all, so nothing can hold the "
                    + "ritual: its target filter measures from the organizer and picks no place, "
                    + "and no role can be filled either";
            }
            return "no target: pass the cell the ritual is held at (x1/z1) or the building "
                + "or spot (target_id), as the Begin dialog asks";
        }
    }
}
