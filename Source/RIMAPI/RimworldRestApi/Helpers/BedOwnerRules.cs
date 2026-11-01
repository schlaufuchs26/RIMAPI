using System.Collections.Generic;
using System.Linq;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Whether the game lets one pawn take one bed (fox tickets #3771, #3874).
    /// Colony 16 built three 5x5 bedrooms with a bed each and two of them stayed
    /// unclaimed for two game days: the game never moves a colonist off the bed
    /// he owns, and nothing in the API assigned one, so the only way out was to
    /// deconstruct the barracks beds. This is the bed tab's own "Assign" choice
    /// as a rule: which pawn may take which bed, and the sentence that says why
    /// not. Kept free of game types so a test can state it.
    /// </summary>
    public static class BedOwnerRules
    {
        /// <summary>One colonist a bed belongs to.</summary>
        public sealed class Owner
        {
            public string Name = "";
            public int Id;
        }

        /// <summary>What the mod read off the bed and the pawn it was asked about.</summary>
        public sealed class State
        {
            /// <summary>The bed is a humanlike bed (an animal bed takes no colonist).</summary>
            public bool BedIsHumanlike = true;

            /// <summary>The bed carries the game's "Set for prisoners" flag.</summary>
            public bool BedForPrisoners;

            /// <summary>The bed carries the game's slave flag.</summary>
            public bool BedForSlaves;

            /// <summary>
            /// Every colonist the bed belongs to now, in the game's order; empty
            /// when it is free. A double bed holds two, so a single name cannot
            /// answer whether the bed still has a slot (fox ticket #3874).
            /// </summary>
            public List<Owner> Owners = new List<Owner>();

            /// <summary>
            /// How many colonists the bed takes, read off
            /// CompAssignableToPawn.MaxAssignedPawnsCount: 2 for a double bed, 1
            /// for a single one. 0 or less is read as one slot, never as
            /// unlimited.
            /// </summary>
            public int Capacity = 1;

            /// <summary>The pawn the caller wants to put in the bed.</summary>
            public string Pawn = "";

            /// <summary>That pawn's id.</summary>
            public int PawnId;

            /// <summary>The pawn is one of the colony's own colonists.</summary>
            public bool PawnIsColonist;

            /// <summary>What the pawn is instead, for the refusal: "a prisoner", "an animal".</summary>
            public string PawnKind = "not a colonist";

            /// <summary>Whether the bed already belongs to this pawn.</summary>
            public bool Holds(int pawnId)
            {
                return Owners.Any(o => o.Id == pawnId);
            }

            /// <summary>The slots the bed still has free.</summary>
            public int FreeSlots()
            {
                int capacity = Capacity > 0 ? Capacity : 1;
                return capacity - Owners.Count;
            }
        }

        /// <summary>
        /// Why the game would refuse this assignment, or null when it takes it.
        /// A bed that already holds as many colonists as it has slots is refused
        /// rather than reassigned: the bed tab offers a bed's owner, and silently
        /// moving a second colonist in would leave the first one on a bed the
        /// answer no longer names. A double bed has two slots, so its second
        /// owner is taken (fox ticket #3874).
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
            if (!state.Holds(state.PawnId) && state.FreeSlots() <= 0)
            {
                int capacity = state.Capacity > 0 ? state.Capacity : 1;
                if (capacity <= 1)
                    return string.Format(
                        "the bed already belongs to {0}: clear the owner first, or the bed tab's own "
                            + "choice is what this call stands in for",
                        Names(state.Owners));
                return string.Format(
                    "the bed already belongs to {0}, all {1} of its slots: clear one owner first, or the bed "
                        + "tab's own choice is what this call stands in for",
                    Names(state.Owners),
                    capacity);
            }
            return null;
        }

        private static string Names(List<Owner> owners)
        {
            if (owners == null || owners.Count == 0) return "nobody";
            List<string> names = owners.Select(o => Named(o.Name, o.Id)).ToList();
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
        }

        private static string Named(string name, int id)
        {
            if (string.IsNullOrEmpty(name)) return string.Format("pawn {0}", id);
            return string.Format("{0} (id {1})", name, id);
        }
    }
}
