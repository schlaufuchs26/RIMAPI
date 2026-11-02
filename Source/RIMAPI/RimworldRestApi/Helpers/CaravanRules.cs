using System;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The rules of forming a caravan (fox ticket #4212, wave 2). The dialog's
    /// Send button runs a handful of checks before it hands the caravan to
    /// CaravanFormingUtility, and every refusal it shows is one of the game's own
    /// translated strings. This file keeps those keys and the arithmetic apart
    /// from the game types, so the tests drive them without a running game.
    ///
    /// The keys are the game's, not ours: a caller reads the same sentence a
    /// player sees in the dialog ("You must have at least one colonist in the
    /// caravan." and so on).
    /// </summary>
    public static class CaravanRules
    {
        /// <summary>The key Dialog_FormCaravan uses for "no colonist goes".</summary>
        public static string ColonistRequiredKey(bool ideologyActive)
        {
            // With Ideology a slave is not enough; the game words the two cases
            // differently and a caller should read the one that applies.
            return ideologyActive
                ? "CaravanMustHaveAtLeastOneNonSlaveColonist"
                : "CaravanMustHaveAtLeastOneColonist";
        }

        /// <summary>The key for a caravan heavier than its animals and pawns carry.</summary>
        public const string OverloadedKey = "TooBigCaravanMassUsage";

        /// <summary>The key for a destination tile the game cannot route to.</summary>
        public const string NoValidExitTileKey = "MessageNoValidExitTile";

        /// <summary>The key for an item no colonist can reach.</summary>
        public static string UnreachableItemKey(int count)
        {
            return count == 1 ? "CaravanItemIsUnreachableSingle" : "CaravanItemIsUnreachableMulti";
        }

        /// <summary>
        /// Whether the mass fits. The game compares the two floats exactly
        /// (MassUsage &gt; MassCapacity), so an exact fit still goes.
        /// </summary>
        public static bool Fits(float usage, float capacity)
        {
            return !(usage > capacity);
        }

        /// <summary>A pawn the call named who cannot leave with this caravan.</summary>
        public static string NotSendable(string pawnLabel, string why)
        {
            return pawnLabel + " cannot leave with a caravan: " + why + ".";
        }

        public static string UnknownPawn(int id)
        {
            return "no pawn with id " + id + ": the colonists read prints the ids";
        }

        public static string UnknownThing(int id)
        {
            return "no thing with id " + id + " on this map: the things read prints the ids";
        }

        public static string BadDestination(int tile)
        {
            return "tile " + tile + " is not a tile of this planet: the world read prints the tile ids";
        }

        public static string SameTile(int tile)
        {
            return "the caravan already stands on tile " + tile + "; name the tile it should travel to";
        }

        public static string NoPawns()
        {
            return "a caravan needs at least one pawn: pass the ids in pawn_ids";
        }

        /// <summary>
        /// Why the game would not send this pawn, in the order the game's own
        /// sendable list drops him. The caller checks the facts it can see; the
        /// fallback names the list instead of guessing a cause.
        /// </summary>
        public static string PawnProblem(bool dead, bool downed, bool prisoner, bool mentalState,
            bool formingCaravan, bool onThisMap)
        {
            if (dead)
                return "he is dead";
            if (downed)
                return "he is down";
            if (prisoner)
                return "he is a prisoner";
            if (mentalState)
                return "he is in a mental state";
            if (formingCaravan)
                return "he is already forming a caravan";
            if (!onThisMap)
                return "he is not on this map";
            return "the game does not list him as sendable (a slave, a lodger or unable to walk)";
        }
    }
}
