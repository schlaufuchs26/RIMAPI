namespace RIMAPI.Helpers
{
    /// <summary>
    /// Whether the game lets one pawn board one shuttle (fox ticket #3862).
    /// The game's own lever is the shuttle's right-click "Enter shuttle", which
    /// only appears when CompShuttle.IsAllowedNow accepts the pawn and the pawn
    /// can reach the shuttle; a quest lodger who had to be aboard was refused
    /// by nothing but the missing call, and a refusal has to name the fact that
    /// stops it instead of "failed". Kept free of game types so a test can
    /// state it.
    /// </summary>
    public static class ShuttleRules
    {
        /// <summary>What the mod read off the shuttle and the pawn it was asked about.</summary>
        public sealed class State
        {
            /// <summary>The shuttle's label and id, for the sentence.</summary>
            public string Shuttle = "";
            public int ShuttleId;

            /// <summary>The cell the shuttle stands on, written as "(x, z)".</summary>
            public string Cell = "";

            /// <summary>The shuttle is on the map.</summary>
            public bool ShuttleSpawned = true;

            /// <summary>The shuttle already loads or holds itself ready to launch.</summary>
            public bool Loading;

            public string Pawn = "";
            public int PawnId;

            /// <summary>The pawn stands on the shuttle's map.</summary>
            public bool PawnSpawned = true;

            /// <summary>Where the pawn is instead, for the refusal: "in a caravan".</summary>
            public string PawnWhere = "";

            public bool PawnDead;
            public bool PawnDowned;

            /// <summary>The pawn is inside the shuttle already.</summary>
            public bool AlreadyAboard;

            /// <summary>The shuttle lists this pawn among the people it waits for.</summary>
            public bool Required;

            /// <summary>The game's own CompShuttle.IsAllowedNow answer.</summary>
            public bool Allowed = true;

            /// <summary>The game's own reachability answer for the shuttle.</summary>
            public bool CanReach = true;

            /// <summary>The shuttle takes colonists at all (CompShuttle.acceptColonists).</summary>
            public bool AcceptsColonists;

            /// <summary>The shuttle takes colonists only, no lodgers or prisoners.</summary>
            public bool OnlyAcceptColonists;

            /// <summary>The shuttle takes only healthy people (CompShuttle.onlyAcceptHealthy).</summary>
            public bool OnlyAcceptHealthy;

            /// <summary>The pawn is a quest lodger, which OnlyAcceptColonists turns away.</summary>
            public bool PawnIsQuestLodger;

            /// <summary>The pawn is not healthy enough for a shuttle that asks for it.</summary>
            public bool PawnUnhealthy;

            /// <summary>
            /// The colonist who carries a pawn that cannot walk, empty when
            /// nobody can. The game's right-click offers "Carry to shuttle" for
            /// a downed pawn, and a hospitality lodger is often paralyzed, so
            /// the order is the same lever with a carrier in front.
            /// </summary>
            public string Carrier = "";

            /// <summary>The pawn is younger than the shuttle's minimum age.</summary>
            public bool BelowMinAge;

            /// <summary>The shuttle's own colonist cap, -1 when it has none.</summary>
            public int MaxColonistCount = -1;

            /// <summary>That cap is reached, counting the colonists already boarding.</summary>
            public bool AtColonistLimit;
        }

        /// <summary>
        /// Why the game would refuse this boarding, or null when it takes the
        /// order. The order of the checks is the order the game applies them,
        /// so the sentence names the first fact that stops the call.
        /// </summary>
        public static string Problem(State state)
        {
            if (state == null) return null;
            if (!state.ShuttleSpawned)
                return "the shuttle " + Named(state.Shuttle, state.ShuttleId)
                    + " is not on the map any more: it has left or been destroyed";
            if (state.PawnDead)
                return Named(state.Pawn, state.PawnId) + " is dead: only a living pawn boards a shuttle";
            if (!state.PawnSpawned)
                return Named(state.Pawn, state.PawnId) + " is not on the map"
                    + (string.IsNullOrEmpty(state.PawnWhere) ? "" : " (he is " + state.PawnWhere + ")")
                    + ": a pawn has to stand on the shuttle's map to board it";
            if (state.AlreadyAboard) return null;
            if (!state.Allowed)
            {
                if (!state.Required)
                {
                    if (!state.AcceptsColonists)
                        return "the shuttle " + Named(state.Shuttle, state.ShuttleId)
                            + " takes only the people its quest names, and "
                            + Named(state.Pawn, state.PawnId) + " is not one of them";
                    if (state.OnlyAcceptColonists && state.PawnIsQuestLodger)
                        return "the shuttle " + Named(state.Shuttle, state.ShuttleId)
                            + " takes only the colony's own colonists, and "
                            + Named(state.Pawn, state.PawnId) + " is a quest lodger";
                    if (state.OnlyAcceptHealthy && state.PawnUnhealthy)
                        return "the shuttle " + Named(state.Shuttle, state.ShuttleId)
                            + " takes only healthy people, and " + Named(state.Pawn, state.PawnId)
                            + " is not: he has to be tended and rested first";
                    if (state.BelowMinAge)
                        return Named(state.Pawn, state.PawnId)
                            + " is too young for the shuttle " + Named(state.Shuttle, state.ShuttleId);
                }
                if (state.AtColonistLimit)
                    return "the shuttle " + Named(state.Shuttle, state.ShuttleId)
                        + " takes at most " + state.MaxColonistCount
                        + " colonist(s) and that many are already boarding";
                return "the game's own check refuses " + Named(state.Pawn, state.PawnId)
                    + " for the shuttle " + Named(state.Shuttle, state.ShuttleId)
                    + ": he is neither one of the people it waits for nor one it accepts";
            }
            if (state.PawnDowned)
            {
                if (!string.IsNullOrEmpty(state.Carrier)) return null;
                return Named(state.Pawn, state.PawnId)
                    + " is down and cannot walk to the shuttle at " + state.Cell
                    + ", and no colonist can carry him there: he needs a colonist who can haul, "
                    + "who can reach him and whom the shuttle takes";
            }
            if (!state.CanReach)
                return Named(state.Pawn, state.PawnId) + " cannot reach the shuttle at " + state.Cell
                    + ": the game finds no path to it";
            return null;
        }

        /// <summary>
        /// Whether the call orders nothing because the pawn is aboard already;
        /// the answer then says so instead of queueing a second job.
        /// </summary>
        public static bool NothingToDo(State state)
        {
            return state != null && state.AlreadyAboard;
        }

        private static string Named(string name, int id)
        {
            if (string.IsNullOrEmpty(name)) return string.Format("thing {0}", id);
            return string.Format("{0} (id {1})", name, id);
        }
    }
}
