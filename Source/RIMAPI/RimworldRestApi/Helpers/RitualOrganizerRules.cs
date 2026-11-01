namespace RIMAPI.Helpers
{
    // The organizer a ritual start falls back to (fox ticket #3675). The
    // `rituals` read names the pawn who fills the ritual's own role, else the
    // first colonist, before it asks the ritual's target filter for a place. A
    // start without pawn_id stopped at the role holder, so with an empty role
    // BestTarget got a null organizer and named no place. Both sides now pick
    // the same pawn. Free of game types so Source/RIMAPI.Tests can state it;
    // the game calls live in IdeoService.StartRitual and DescribeRitual.
    public static class RitualOrganizerRules
    {
        // Pick returns the organizer: the pawn the call named, else the one who
        // fills the ritual's role (the leader for the speech), else the colony's
        // first colonist, as the read does. Null only for an empty colony.
        public static T Pick<T>(T named, T roleHolder, T firstColonist) where T : class
        {
            return named ?? roleHolder ?? firstColonist;
        }
    }
}
