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

        // ---- Steering a formed caravan (fox ticket #4226) ----
        //
        // A formed caravan's destination is what the player sets by
        // right-clicking a tile with the caravan selected: a plain tile is a
        // travel order, a settlement offers Visit and Trade, and the game's own
        // arrival action decides what happens on arrival.

        /// <summary>The plain travel order: walk to the tile, do nothing there.</summary>
        public const string TravelAction = "travel";

        /// <summary>Visit a settlement: enter its map, the game's own arrival action.</summary>
        public const string VisitAction = "visit";

        /// <summary>Trade with a settlement: the game's trade dialog opens on arrival.</summary>
        public const string TradeAction = "trade";

        /// <summary>
        /// The three actions the steer route takes, in the order a caller reads
        /// them. Travel is the default, because it is the one that needs nothing
        /// but a tile.
        /// </summary>
        public static readonly string[] Actions = { TravelAction, VisitAction, TradeAction };

        /// <summary>
        /// Reads the action name a caller passed. An empty name is a travel
        /// order (the route's default); anything else has to be one of the three.
        /// </summary>
        public static bool TryParseAction(string raw, out string action)
        {
            action = TravelAction;
            if (raw == null || raw.Trim().Length == 0)
                return true;
            string name = raw.Trim().ToLowerInvariant();
            foreach (string known in Actions)
            {
                if (known == name)
                {
                    action = known;
                    return true;
                }
            }
            return false;
        }

        public static string UnknownAction(string raw)
        {
            return "unknown caravan action \"" + raw + "\": the steer route takes "
                + string.Join(", ", Actions);
        }

        public static string UnknownCaravan(int id)
        {
            return "no caravan with id " + id + " on the world map: /api/v1/world/caravans prints the ids";
        }

        public static string NotOurs(string label)
        {
            return label + " is not the player's caravan, so it cannot be steered";
        }

        public static string BadTile(int tile)
        {
            return "tile " + tile + " is not a tile of this planet: /api/v1/world/tile reads one by id";
        }

        public static string AlreadyThere(int tile)
        {
            return "the caravan already stands on tile " + tile + "; name the tile it should travel to";
        }

        /// <summary>
        /// The game's pathfinder refused the path. It is its own gate (an
        /// impassable ocean or mountain range between here and there), so the
        /// refusal names it instead of blaming the call.
        /// </summary>
        public static string NoPath(int tile)
        {
            return "the game found no passable route to tile " + tile + ", so it would not start the caravan";
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
