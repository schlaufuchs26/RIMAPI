namespace RIMAPI.Models
{
    /// <summary>
    /// Steering a formed caravan (fox ticket #4226, wave 2). A caravan that left
    /// the map could not be told where to go: the form route sent it once and the
    /// game moved it, and nothing outside the game could change the order. This
    /// request sets the destination the way the player's right-click does.
    /// </summary>
    public class SteerCaravanRequest
    {
        /// <summary>The caravan's id, as /api/v1/world/caravans prints it.</summary>
        public int CaravanId { get; set; }

        /// <summary>The tile it should travel to, as /api/v1/world/tile prints it.</summary>
        public int Tile { get; set; }

        /// <summary>
        /// What happens on arrival: "travel" (the default, a plain walk),
        /// "visit" (enter the settlement's map) or "trade" (the trade dialog
        /// opens there).
        /// </summary>
        public string Action { get; set; }
    }

    /// <summary>
    /// The order that was set: where the caravan goes, what the game will do on
    /// arrival, and how long the trip takes. The caravan walks by itself from
    /// here; the game moves it one tile per its own ticks.
    /// </summary>
    public class CaravanSteerResultDto
    {
        public int CaravanId { get; set; }
        public string CaravanName { get; set; }
        public int Tile { get; set; }
        public string Action { get; set; }

        /// <summary>The game's own label for the order ("Visit settlement X").</summary>
        public string Label { get; set; }

        public int TilesInPath { get; set; }
        public int TicksToArrive { get; set; }
        public float DaysToArrive { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// Trading with a settlement the caravan reached (fox ticket #4226, wave 2).
    /// The settlement is named by its id; its tile is where the caravan has to
    /// be, and a caravan still on the way is sent there with the trade order, so
    /// the dialog opens when it arrives.
    /// </summary>
    public class CaravanTradeRequest
    {
        public int CaravanId { get; set; }

        /// <summary>The settlement's id, as /api/v1/world/settlements prints it.</summary>
        public int SettlementId { get; set; }
    }

    /// <summary>
    /// What the trade call did: the game's trade dialog is open, or the caravan
    /// is on its way and the dialog opens on arrival. The dialog itself is read
    /// through /api/v1/ui/dialog, the same window a player sees.
    /// </summary>
    public class CaravanTradeResultDto
    {
        public int CaravanId { get; set; }
        public string CaravanName { get; set; }
        public int SettlementId { get; set; }
        public string SettlementName { get; set; }
        public int Tile { get; set; }

        /// <summary>Whether the caravan already stood at the settlement.</summary>
        public bool AtSettlement { get; set; }

        /// <summary>Whether the game's trade dialog is open now.</summary>
        public bool DialogOpen { get; set; }

        /// <summary>The pawn the game picked to negotiate, when one was found.</summary>
        public string Negotiator { get; set; }

        public int TicksToArrive { get; set; }
        public float DaysToArrive { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// Settling a new home where the caravan stands (fox ticket #4226, wave 2):
    /// the Settle command of the caravan's own gizmo bar.
    /// </summary>
    public class SettleCaravanRequest
    {
        public int CaravanId { get; set; }
    }

    /// <summary>
    /// The settlement the game created. The map under it generates on the game's
    /// own long event, so it is queued here and appears a moment later.
    /// </summary>
    public class SettleCaravanResultDto
    {
        public int CaravanId { get; set; }
        public string CaravanName { get; set; }
        public int Tile { get; set; }
        public int SettlementId { get; set; }
        public string SettlementName { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// Abandoning a settlement (fox ticket #4226, wave 2): the Abandon command of
    /// a player settlement's own gizmo bar.
    /// </summary>
    public class AbandonSettlementRequest
    {
        public int SettlementId { get; set; }
    }

    /// <summary>What was abandoned: the settlement, its tile, and where its pawns are.</summary>
    public class AbandonSettlementResultDto
    {
        public int SettlementId { get; set; }
        public string Name { get; set; }
        public int Tile { get; set; }
        public string Note { get; set; }
    }
}
