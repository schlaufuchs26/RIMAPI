using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    /// <summary>
    /// Settling a new home and abandoning one (fox ticket #4226, wave 2). Both are
    /// commands of the player's own interface: the Settle and Abandon buttons of a
    /// caravan's and a settlement's gizmo bar.
    /// </summary>
    public interface ISettlementService
    {
        /// <summary>Settle a new home where the caravan stands.</summary>
        ApiResult<SettleCaravanResultDto> Settle(SettleCaravanRequest request);

        /// <summary>Abandon a player settlement.</summary>
        ApiResult<AbandonSettlementResultDto> Abandon(AbandonSettlementRequest request);
    }
}
