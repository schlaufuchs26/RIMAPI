using System;
using System.Linq;
using System.Text;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RIMAPI.Services
{
    /// <summary>
    /// Settling a new home and abandoning one (fox ticket #4226, wave 2). The
    /// world map let a colony read its neighbours and, since #4212, leave; it could
    /// not move its own home, and it could not give one up.
    ///
    /// Both levers are the game's own commands, not a copy of what they do:
    /// SettleInEmptyTileUtility.Settle is the Settle button of a caravan's gizmo
    /// bar (it creates the settlement, queues the map generation on the game's own
    /// long event and walks the caravan in), and Settlement.Abandon is what the
    /// Abandon button calls once its confirmation is answered.
    ///
    /// The refusals are the commands' own: TileFinder's reason why a tile cannot
    /// hold a home, the Settle command's two limit sentences, the Abandon command's
    /// "all colonists are there", and the warning the game shows before it gives up
    /// an archotech structure.
    /// </summary>
    public class SettlementService : ISettlementService
    {
        public ApiResult<SettleCaravanResultDto> Settle(SettleCaravanRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResult<SettleCaravanResultDto>.Fail(
                        "a settle call needs a body: caravan_id"
                    );

                Caravan caravan = CaravanHelper.GetCaravanById(request.CaravanId);
                if (caravan == null)
                    return ApiResult<SettleCaravanResultDto>.Fail(
                        CaravanRules.UnknownCaravan(request.CaravanId)
                    );
                // The game's own Settle refuses a non-player faction with a log
                // line, so the route refuses first and says why.
                if (!caravan.IsPlayerControlled || caravan.Faction != Faction.OfPlayer)
                    return ApiResult<SettleCaravanResultDto>.Fail(CaravanRules.NotOurs(caravan.Label));

                PlanetTile tile = caravan.Tile;
                var reason = new StringBuilder();
                if (!TileFinder.IsValidTileForNewSettlement(tile, reason))
                {
                    string why = reason.Length > 0
                        ? reason.ToString()
                        : "the game does not allow a home on this tile";
                    return ApiResult<SettleCaravanResultDto>.Fail(
                        "tile " + tile.tileId + " cannot hold a home: " + why
                    );
                }
                if (SettleUtility.PlayerSettlementsCountLimitReached)
                {
                    return ApiResult<SettleCaravanResultDto>.Fail(
                        SettlementRules
                            .SettleLimitKey(Prefs.MaxNumberOfPlayerSettlements > 1)
                            .Translate()
                            .ToString()
                    );
                }

                // The command's own action: AddNewHome runs here, the map and the
                // caravan's walk-in follow on the game's long event queue.
                SettleInEmptyTileUtility.Settle(caravan);
                Settlement settled = Find.WorldObjects.SettlementBaseAt(tile);
                return ApiResult<SettleCaravanResultDto>.Ok(
                    new SettleCaravanResultDto
                    {
                        CaravanId = caravan.ID,
                        CaravanName = caravan.Name,
                        Tile = tile.tileId,
                        SettlementId = settled?.ID ?? 0,
                        SettlementName = settled?.Label,
                        Note =
                            "the game created the settlement and queues its map now (the game's own long "
                            + "event, which takes a moment of game time); /api/v1/world/player/settlements "
                            + "names it once the map exists",
                    }
                );
            }
            catch (Exception ex)
            {
                return ApiResult<SettleCaravanResultDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<AbandonSettlementResultDto> Abandon(AbandonSettlementRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResult<AbandonSettlementResultDto>.Fail(
                        "an abandon call needs a body: settlement_id"
                    );

                Settlement settlement = Find
                    .WorldObjects.AllWorldObjects.OfType<Settlement>()
                    .FirstOrDefault(s => s.ID == request.SettlementId);
                if (settlement == null)
                    return ApiResult<AbandonSettlementResultDto>.Fail(
                        SettlementRules.UnknownSettlement(request.SettlementId)
                    );
                if (settlement.Faction != Faction.OfPlayer)
                {
                    return ApiResult<AbandonSettlementResultDto>.Fail(
                        SettlementRules.NotPlayerSettlement(
                            settlement.Label,
                            settlement.Faction?.Name
                        )
                    );
                }
                if (SettlementAbandonUtility.AllColonistsThere(settlement))
                {
                    return ApiResult<AbandonSettlementResultDto>.Fail(
                        SettlementRules.AbandonAllColonistsKey.Translate().ToString()
                    );
                }
                // The game asks this before it gives up an archotech structure,
                // because the structure goes with the settlement. A route has no
                // dialog to answer, so it refuses and names the warning.
                if (
                    settlement is ArchotechSettlement archotech
                    && archotech.AnyArchotechBuildingRequiresStudy
                )
                {
                    return ApiResult<AbandonSettlementResultDto>.Fail(
                        SettlementRules.ArchotechWarningKey.Translate().ToString()
                    );
                }

                int id = settlement.ID;
                string name = settlement.Label;
                int tile = settlement.Tile.tileId;
                // The command's own action, minus the confirmation dialog: the
                // caller is the one asking, so no dialog stands between them.
                settlement.Abandon(wasGravshipLaunch: false);
                Find.GameEnder.CheckOrUpdateGameOver();

                return ApiResult<AbandonSettlementResultDto>.Ok(
                    new AbandonSettlementResultDto
                    {
                        SettlementId = id,
                        Name = name,
                        Tile = tile,
                        Note =
                            "the game abandoned the settlement: its map is gone, its pawns walk out on "
                            + "their own, and an abandoned-settlement world object stays on the tile",
                    }
                );
            }
            catch (Exception ex)
            {
                return ApiResult<AbandonSettlementResultDto>.Fail(ex.ToString());
            }
        }
    }
}
