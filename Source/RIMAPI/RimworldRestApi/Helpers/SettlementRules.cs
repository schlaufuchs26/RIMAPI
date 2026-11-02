namespace RIMAPI.Helpers
{
    /// <summary>
    /// The rules of settling a new map, abandoning one and trading with another
    /// faction's settlement (fox ticket #4226, wave 2). Every lever is the game's
    /// own: TileFinder decides whether a tile may hold a home, the Settle command
    /// carries the two limit sentences, SettlementAbandonUtility refuses while
    /// every colonist is home, and CaravanArrivalAction_Trade decides whether a
    /// caravan may trade at all.
    ///
    /// Those levers hand back the game's translated strings where they have one;
    /// where a check answers only yes or no (Visitable, CanTradeNow), this file
    /// names the fact that stopped it. Plain C#, so the wording is tested without
    /// a running game.
    /// </summary>
    public static class SettlementRules
    {
        /// <summary>
        /// The Settle command's two limit sentences: with one settlement allowed
        /// it says a base exists, with several it says the maximum is reached.
        /// </summary>
        public static string SettleLimitKey(bool moreThanOneAllowed)
        {
            return moreThanOneAllowed
                ? "CommandSettleFailReachedMaximumNumberOfBases"
                : "CommandSettleFailAlreadyHaveBase";
        }

        /// <summary>The Abandon command's own refusal while every colonist is home.</summary>
        public const string AbandonAllColonistsKey = "CommandAbandonHomeFailAllColonistsThere";

        /// <summary>
        /// The game asks this before it abandons an archotech structure's
        /// settlement, because the structure is lost with it. A route has no
        /// dialog, so it refuses and names the warning instead.
        /// </summary>
        public const string ArchotechWarningKey = "AbandonArchotechStructuresWarningDialog";

        public static string UnknownSettlement(int id)
        {
            return "no settlement with id " + id + " on the world map: /api/v1/world/settlements prints the ids";
        }

        public static string NotPlayerSettlement(string label, string factionName)
        {
            return label + " belongs to " + (factionName ?? "no faction")
                + ", so the player cannot abandon it; /api/v1/world/player/settlements lists the player's own";
        }

        public static string NoSettlementOnTile(int tile)
        {
            return "no settlement stands on tile " + tile + ", so there is nothing to visit or trade with: "
                + "/api/v1/world/settlements prints the settlements and their tiles";
        }

        /// <summary>
        /// Why the game offers no Visit for a settlement. CanVisit answers only
        /// yes or no, so the reason is named here.
        /// </summary>
        public static string VisitProblem(bool spawned, bool visitable)
        {
            if (!spawned)
                return "it is not on the world map any more";
            if (!visitable)
                return "the game does not offer Visit for it: it is the player's own settlement, its faction is "
                    + "hostile to the player, or it lies in space";
            return null;
        }

        /// <summary>
        /// Why the game offers no Trade with a settlement, in the order
        /// CaravanArrivalAction_Trade.CanTradeWith checks the facts. The two
        /// levers that answer only yes or no (CanTradeNow, HasNegotiator) name
        /// what they mean instead.
        /// </summary>
        public static string TradeProblem(bool spawned, bool hasMap, bool playerFaction, bool permanentEnemy,
            bool hostile, bool canTradeNow, bool hasNegotiator)
        {
            if (!spawned)
                return "it is not on the world map any more";
            if (hasMap)
                return "its map is open, so its trader is an on-map trader: /api/v1/traders lists those and "
                    + "/api/v1/traders/trade runs their deal";
            if (playerFaction)
                return "it is the player's own settlement; a caravan trades with another faction's";
            if (permanentEnemy)
                return "its faction is a permanent enemy, so the game offers no trade with it";
            if (hostile)
                return "its faction is hostile, so the game offers gifts rather than trade there";
            if (!canTradeNow)
                return "its trader has nothing to trade right now (out of stock or restocking)";
            if (!hasNegotiator)
                return "no pawn of the caravan can negotiate: the game's negotiator has Social, and the skill is "
                    + "not disabled for him";
            return null;
        }

        /// <summary>
        /// What a steer or trade call answers when the arrival action's own
        /// acceptance report carries no sentence: the game rejected it without
        /// saying why, so the answer says that instead of inventing a cause.
        /// </summary>
        public static string NoReasonGiven(string settlementLabel, string action)
        {
            return "the game does not offer " + action + " for " + settlementLabel
                + " and gave no reason; the settlement read names its faction and its trader";
        }
    }
}
