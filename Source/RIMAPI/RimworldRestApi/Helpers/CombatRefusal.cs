namespace RIMAPI.Helpers
{
    /// <summary>
    /// Why a pawn cannot be drafted, ordered or put to work (fox ticket
    /// #3433). The game's own Pawn.IsColonistPlayerControlled is
    /// Spawned &amp;&amp; IsColonist &amp;&amp; MentalStateDef == null &amp;&amp; HostFaction == null,
    /// so a colonist in a tantrum fails it, and a refusal built on that flag
    /// read "not one of the colony's colonists" for a pawn the colony plainly
    /// has. The checks are plain flags, so a test can state the rule without a
    /// game.
    /// </summary>
    public static class CombatRefusal
    {
        /// <summary>One pawn's state, as the game reports it.</summary>
        public struct PawnState
        {
            public bool Dead;
            public bool Spawned;
            /// <summary>Label of the pawn carrying this one, empty when nobody does.</summary>
            public string CarriedBy;
            public bool Colonist;
            /// <summary>The mental state's name ("tantrum"), empty when the pawn is sane.</summary>
            public string MentalState;
            public bool Downed;
            public bool HasDrafter;
        }

        /// <summary>
        /// The first condition that keeps the pawn from doing anything, as a
        /// phrase that reads after "Gray (id 628): " and after "Gray is ", or
        /// null when nothing keeps him. The order is the one a run reads: dead
        /// before off the map, and the mental state before the downed state it
        /// may have put him in.
        /// </summary>
        public static string StateOf(PawnState pawn)
        {
            if (pawn.Dead) return "dead";
            if (!string.IsNullOrEmpty(pawn.CarriedBy)) return "being carried by " + pawn.CarriedBy;
            if (!pawn.Spawned) return "not on the map (a caravan, a pod or a cryptosleep casket)";
            if (!pawn.Colonist) return "not one of the colony's colonists";
            if (!string.IsNullOrEmpty(pawn.MentalState))
                return "in a mental state (" + pawn.MentalState + ")";
            if (pawn.Downed) return "downed";
            return null;
        }

        /// <summary>
        /// The note a draft answer puts after "Gray (id 628): ", or null when
        /// the pawn can be drafted.
        /// </summary>
        public static string DraftNote(PawnState pawn)
        {
            string state = StateOf(pawn);
            if (state != null) return state + ", so he cannot be drafted";
            if (!pawn.HasDrafter) return "without a draft controller, so he cannot be drafted";
            return null;
        }

        /// <summary>
        /// The whole sentence an order is refused with, or null when the pawn
        /// can take one.
        /// </summary>
        public static string OrderRefusal(string name, PawnState pawn)
        {
            string state = StateOf(pawn);
            if (state != null) return name + " is " + state + ", so he cannot act";
            if (!pawn.HasDrafter) return name + " is without a draft controller, so he cannot act";
            return null;
        }

        /// <summary>
        /// The state's own name: its label when it has one ("tantrum") and its
        /// defName otherwise ("Berserk"), so a state without a label still
        /// reads.
        /// </summary>
        public static string MentalStateName(string label, string defName)
        {
            if (!string.IsNullOrEmpty(label)) return label;
            return string.IsNullOrEmpty(defName) ? "unknown" : defName;
        }
    }
}
