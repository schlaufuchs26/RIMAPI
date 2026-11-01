namespace RIMAPI.Helpers
{
    /// <summary>
    /// Whether the game lets one pawn take one bed (fox ticket #3771). Colony 16
    /// built three 5x5 bedrooms with a bed each and two of them stayed
    /// unclaimed for two game days: the game never moves a colonist off the bed
    /// he owns, and nothing in the API assigned one, so the only way out was to
    /// deconstruct the barracks beds. This is the bed tab's own "Assign" choice
    /// as a rule: which pawn may take which bed, and the sentence that says why
    /// not. Kept free of game types so a test can state it.
    /// </summary>
    public static class BedOwnerRules
    {
        /// <summary>What the mod read off the bed and the pawn it was asked about.</summary>
        public sealed class State
        {
            /// <summary>The bed is a humanlike bed (an animal bed takes no colonist).</summary>
            public bool BedIsHumanlike = true;

            /// <summary>The bed carries the game's "Set for prisoners" flag.</summary>
            public bool BedForPrisoners;

            /// <summary>The bed carries the game's slave flag.</summary>
            public bool BedForSlaves;

            /// <summary>The colonist the bed belongs to now, empty when it is free.</summary>
            public string BedOwner = "";

            /// <summary>That owner's pawn id, 0 when the bed is free.</summary>
            public int BedOwnerId;

            /// <summary>The pawn the caller wants to put in the bed.</summary>
            public string Pawn = "";

            /// <summary>That pawn's id.</summary>
            public int PawnId;

            /// <summary>The pawn is one of the colony's own colonists.</summary>
            public bool PawnIsColonist;

            /// <summary>What the pawn is instead, for the refusal: "a prisoner", "an animal".</summary>
            public string PawnKind = "not a colonist";
        }

        /// <summary>
        /// Why the game would refuse this assignment, or null when it takes it.
        /// A bed that already belongs to a different colonist is refused rather
        /// than reassigned: the bed tab offers a bed's owner, and silently
        /// moving a second colonist in would leave the first one on a bed the
        /// answer no longer names.
        /// </summary>
        public static string Problem(State state)
        {
            if (state == null) return null;
            if (!state.BedIsHumanlike)
                return "only a humanlike bed takes a colonist as its owner: an animal bed takes an animal";
            if (state.BedForPrisoners)
                return "the bed is set for prisoners: the game gives it to a prisoner when one is carried in, "
                    + "so no colonist can own it (set bed_for=colonists first)";
            if (state.BedForSlaves)
                return "the bed is set for slaves: the game gives it to a slave, so no colonist can own it";
            if (!state.PawnIsColonist)
                return string.Format(
                    "{0} is {1}, not a colonist: only a colonist takes a bed",
                    Named(state.Pawn, state.PawnId),
                    state.PawnKind);
            if (state.BedOwner != "" && state.BedOwnerId != state.PawnId)
                return string.Format(
                    "the bed already belongs to {0} (id {1}): clear the owner first, or the bed tab's own "
                        + "choice is what this call stands in for",
                    state.BedOwner,
                    state.BedOwnerId);
            return null;
        }

        private static string Named(string name, int id)
        {
            if (string.IsNullOrEmpty(name)) return string.Format("pawn {0}", id);
            return string.Format("{0} (id {1})", name, id);
        }
    }
}
