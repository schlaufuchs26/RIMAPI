namespace RIMAPI.Helpers
{
    /// <summary>
    /// Why a colonist will not take a piece of work (fox ticket #3625). A
    /// designation is not an ordered job: the work loop hands it out only to a
    /// colonist whose work type is on and who is capable of it, so "the order
    /// stands" could mean "wait for a free colonist" or "nobody will ever take
    /// it", and those two need opposite decisions.
    ///
    /// The checks stay free of game types so Source/RIMAPI.Tests can state
    /// them; the game walk lives in WorkAcceptance.
    /// </summary>
    public static class WorkAcceptanceRules
    {
        /// <summary>One colonist's state, as the game reports it.</summary>
        public struct State
        {
            public bool Downed;
            public bool MentalState;
            public bool Drafted;
            public bool HasWorkSettings;
            /// <summary>The work type is on in the colonist's work tab.</summary>
            public bool WorkTypeActive;
            /// <summary>The colonist's work tags allow this work type.</summary>
            public bool WorkTagsAllowed;
            /// <summary>The capacity the work giver needs and the colonist lacks, empty when none.</summary>
            public string MissingCapacity;
            /// <summary>The work type's defName, e.g. PlantCutting.</summary>
            public string WorkTypeName;
        }

        /// <summary>
        /// The first check that keeps the colonist off this work type, as a
        /// phrase that reads after "Stench (Plants 1): ", or null when the
        /// colonist can take it.
        /// </summary>
        public static string Refusal(State state)
        {
            if (state.Downed) return "downed";
            if (state.MentalState) return "in a mental state";
            if (state.Drafted) return "drafted";
            if (!state.WorkTagsAllowed) return "incapable of this work";
            if (!string.IsNullOrEmpty(state.MissingCapacity))
                return "cannot do it (needs " + state.MissingCapacity + ")";
            if (!state.HasWorkSettings || !state.WorkTypeActive)
                return state.WorkTypeName + " work is off";
            return null;
        }
    }
}
