using RIMAPI.Core;

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
        [ParameterDescription("The caravan's id, as /api/v1/world/caravans prints it.")]
        public int CaravanId { get; set; }

        /// <summary>The tile it should travel to, as /api/v1/world/tile prints it.</summary>
        [ParameterDescription("The tile it should travel to, as /api/v1/world/tile prints it.")]
        public int Tile { get; set; }

        /// <summary>
        /// What happens on arrival: "travel" (the default, a plain walk),
        /// "visit" (enter the settlement's map) or "trade" (the trade dialog
        /// opens there).
        /// </summary>
        [ParameterDescription("What happens on arrival: \"travel\" (the default, a plain walk), \"visit\" (enter the settlement's map) or \"trade\" (the trade dialog opens there).")]
        public string Action { get; set; }
    }

    /// <summary>
    /// The order that was set: where the caravan goes, what the game will do on
    /// arrival, and how long the trip takes. The caravan walks by itself from
    /// here; the game moves it one tile per its own ticks.
    /// </summary>
    public class CaravanSteerResultDto
    {
        [ParameterDescription("The caravan's numeric id.")]
        public int CaravanId { get; set; }
        [ParameterDescription("The caravan's name, as the game shows it.")]
        public string CaravanName { get; set; }
        [ParameterDescription("The tile the caravan now heads to.")]
        public int Tile { get; set; }
        [ParameterDescription("What the call did, e.g. steer or trade.")]
        public string Action { get; set; }

        /// <summary>The game's own label for the order ("Visit settlement X").</summary>
        public string Label { get; set; }

        [ParameterDescription("How many tiles the path holds.")]
        public int TilesInPath { get; set; }
        [ParameterDescription("How many ticks the walk needs.")]
        public int TicksToArrive { get; set; }
        [ParameterDescription("How many in-game days that is.")]
        public float DaysToArrive { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
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
        [ParameterDescription("The caravan's numeric id.")]
        public int CaravanId { get; set; }

        /// <summary>The settlement's id, as /api/v1/world/settlements prints it.</summary>
        [ParameterDescription("The settlement's id, as /api/v1/world/settlements prints it.")]
        public int SettlementId { get; set; }
    }

    /// <summary>
    /// What the trade call did: the game's trade dialog is open, or the caravan
    /// is on its way and the dialog opens on arrival. The dialog itself is read
    /// through /api/v1/ui/dialog, the same window a player sees.
    /// </summary>
    public class CaravanTradeResultDto
    {
        [ParameterDescription("The caravan's numeric id.")]
        public int CaravanId { get; set; }
        [ParameterDescription("The caravan's name, as the game shows it.")]
        public string CaravanName { get; set; }
        [ParameterDescription("The settlement's world tile id.")]
        public int SettlementId { get; set; }
        [ParameterDescription("The settlement's name, as the game shows it.")]
        public string SettlementName { get; set; }
        [ParameterDescription("The tile the caravan stands on.")]
        public int Tile { get; set; }

        /// <summary>Whether the caravan already stood at the settlement.</summary>
        [ParameterDescription("Whether the caravan already stood at the settlement.")]
        public bool AtSettlement { get; set; }

        /// <summary>Whether the game's trade dialog is open now.</summary>
        [ParameterDescription("Whether the game's trade dialog is open now.")]
        public bool DialogOpen { get; set; }

        /// <summary>The pawn the game picked to negotiate, when one was found.</summary>
        [ParameterDescription("The pawn the game picked to negotiate, when one was found.")]
        public string Negotiator { get; set; }

        [ParameterDescription("How many ticks until the caravan arrives.")]
        public int TicksToArrive { get; set; }
        [ParameterDescription("How many in-game days that is.")]
        public float DaysToArrive { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }

    /// <summary>
    /// Settling a new home where the caravan stands (fox ticket #4226, wave 2):
    /// the Settle command of the caravan's own gizmo bar.
    /// </summary>
    public class SettleCaravanRequest
    {
        [ParameterDescription("The caravan's numeric id.")]
        public int CaravanId { get; set; }
    }

    /// <summary>
    /// The settlement the game created. The map under it generates on the game's
    /// own long event, so it is queued here and appears a moment later.
    /// </summary>
    public class SettleCaravanResultDto
    {
        [ParameterDescription("The caravan's numeric id.")]
        public int CaravanId { get; set; }
        [ParameterDescription("The caravan's name, as the game shows it.")]
        public string CaravanName { get; set; }
        [ParameterDescription("The tile the colony settled on.")]
        public int Tile { get; set; }
        [ParameterDescription("The settlement's world tile id.")]
        public int SettlementId { get; set; }
        [ParameterDescription("The settlement's name, as the game shows it.")]
        public string SettlementName { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }

    /// <summary>
    /// Abandoning a settlement (fox ticket #4226, wave 2): the Abandon command of
    /// a player settlement's own gizmo bar.
    /// </summary>
    public class AbandonSettlementRequest
    {
        [ParameterDescription("The settlement's world tile id.")]
        public int SettlementId { get; set; }
    }

    /// <summary>What was abandoned: the settlement, its tile, and where its pawns are.</summary>
    public class AbandonSettlementResultDto
    {
        [ParameterDescription("The settlement's world tile id.")]
        public int SettlementId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The tile the settlement stood on.")]
        public int Tile { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }
}
