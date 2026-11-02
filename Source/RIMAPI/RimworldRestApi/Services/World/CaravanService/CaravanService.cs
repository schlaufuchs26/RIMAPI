using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    /// <summary>
    /// Forming a caravan (fox ticket #4212, wave 2). The world map had eleven
    /// read routes and no write, so a colony could read its neighbours and never
    /// visit them.
    ///
    /// The lever is the one the dialog's Send button pulls:
    /// CaravanFormingUtility.StartFormingCaravan. It creates the game's own
    /// LordJob_FormAndSendCaravan, so the pawns walk to the packing spot, haul
    /// the named goods and leave through the game's own exit cell; the caravan
    /// then exists on the world map and the game moves it. Nothing is teleported
    /// and nothing is created: the same jobs a player's caravan runs.
    ///
    /// The refusals are the dialog's own: the game's translated strings for a
    /// caravan without a colonist and one heavier than it can carry, plus the
    /// fact that stopped a named pawn.
    /// </summary>
    public class CaravanService : ICaravanService
    {
        public ApiResult<FormCaravanResultDto> Form(FormCaravanRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResult<FormCaravanResultDto>.Fail(
                        "a caravan call needs a body: pawn_ids, items and destination_tile"
                    );
                if (request.PawnIds == null || request.PawnIds.Count == 0)
                    return ApiResult<FormCaravanResultDto>.Fail(CaravanRules.NoPawns());

                Map map = MapLookup.Resolve(request.MapId, MapHelper.GetMapByID, out string error);
                if (map == null)
                    return ApiResult<FormCaravanResultDto>.Fail(error);

                PlanetTile destination;
                string destinationProblem = ResolveDestination(request.DestinationTile, map, out destination);
                if (destinationProblem != null)
                    return ApiResult<FormCaravanResultDto>.Fail(destinationProblem);

                List<Pawn> pawns;
                string pawnProblem = ResolvePawns(request.PawnIds, map, out pawns);
                if (pawnProblem != null)
                    return ApiResult<FormCaravanResultDto>.Fail(pawnProblem);

                // The dialog's own check, in the game's words: one colonist who
                // is not a slave and not down has to go.
                if (!pawns.Any(p => CaravanUtility.IsOwner(p, Faction.OfPlayer) && !p.Downed))
                {
                    return ApiResult<FormCaravanResultDto>.Fail(
                        CaravanRules.ColonistRequiredKey(ModsConfig.IdeologyActive).Translate().ToString()
                    );
                }

                PlanetTile directionTile;
                string tileProblem = ResolveDirectionTile(map, destination, out directionTile);
                if (tileProblem != null)
                    return ApiResult<FormCaravanResultDto>.Fail(tileProblem);

                List<TransferableOneWay> transferables;
                string itemProblem = BuildTransferables(request.Items, map, pawns, out transferables);
                if (itemProblem != null)
                    return ApiResult<FormCaravanResultDto>.Fail(itemProblem);

                // Mass is the dialog's arithmetic: what the goods weigh against
                // what the pawns and their pack animals carry.
                float usage = CollectionsMassCalculator.MassUsageTransferables(
                    transferables,
                    IgnorePawnsInventoryMode.IgnoreIfAssignedToUnloadOrPlayerPawn,
                    includePawnsMass: false,
                    ignoreSpawnedCorpsesGearAndInventory: false
                );
                var capacityExplanation = new StringBuilder();
                float capacity = CollectionsMassCalculator.CapacityTransferables(transferables, capacityExplanation);
                if (!CaravanRules.Fits(usage, capacity))
                {
                    return ApiResult<FormCaravanResultDto>.Fail(
                        CaravanRules.OverloadedKey.Translate().ToString()
                            + " (the goods weigh "
                            + usage.ToString("0.#")
                            + " kg against "
                            + capacity.ToString("0.#")
                            + " kg of capacity)"
                    );
                }

                IntVec3 exitSpot;
                string exitProblem = ResolveExitSpot(pawns, map, out exitSpot);
                if (exitProblem != null)
                    return ApiResult<FormCaravanResultDto>.Fail(exitProblem);

                IntVec3 meetingPoint;
                string meetingProblem = ResolveMeetingPoint(exitSpot, map, out meetingPoint);
                if (meetingProblem != null)
                    return ApiResult<FormCaravanResultDto>.Fail(meetingProblem);

                // The dialog's Send button, line for line: the pawns who walk
                // form the caravan, a downed one is carried by the others.
                CaravanFormingUtility.StartFormingCaravan(
                    pawns.Where(p => !p.Downed).ToList(),
                    pawns.Where(p => p.Downed).ToList(),
                    Faction.OfPlayer,
                    transferables,
                    meetingPoint,
                    exitSpot,
                    map.Tile,
                    destination
                );

                return ApiResult<FormCaravanResultDto>.Ok(
                    Answer(pawns, request.Items, exitSpot, meetingPoint, map.Tile, directionTile, destination, usage, capacity)
                );
            }
            catch (Exception ex)
            {
                return ApiResult<FormCaravanResultDto>.Fail(ex.ToString());
            }
        }

        /// <summary>
        /// Changes a formed caravan's destination (fox ticket #4226, wave 2). The
        /// lever is the one the world map's right-click pulls: a plain tile is
        /// WorldSelector.AutoOrderToTileNow (the nearest reachable tile, then the
        /// pather's StartPath with no arrival action), a settlement is the game's
        /// own arrival action for Visit or Trade, which is what decides what
        /// happens when the caravan gets there.
        /// </summary>
        public ApiResult<CaravanSteerResultDto> Steer(SteerCaravanRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResult<CaravanSteerResultDto>.Fail(
                        "a steer call needs a body: caravan_id and tile"
                    );

                Caravan caravan = CaravanHelper.GetCaravanById(request.CaravanId);
                if (caravan == null)
                    return ApiResult<CaravanSteerResultDto>.Fail(
                        CaravanRules.UnknownCaravan(request.CaravanId)
                    );
                if (!caravan.IsPlayerControlled)
                    return ApiResult<CaravanSteerResultDto>.Fail(CaravanRules.NotOurs(caravan.Label));
                if (!CaravanRules.TryParseAction(request.Action, out string action))
                    return ApiResult<CaravanSteerResultDto>.Fail(
                        CaravanRules.UnknownAction(request.Action)
                    );
                if (request.Tile < 0 || request.Tile >= Find.WorldGrid.TilesCount)
                    return ApiResult<CaravanSteerResultDto>.Fail(CaravanRules.BadTile(request.Tile));

                PlanetTile clicked = new PlanetTile(request.Tile);
                if (clicked == caravan.Tile && !caravan.pather.Moving)
                    return ApiResult<CaravanSteerResultDto>.Fail(
                        CaravanRules.AlreadyThere(request.Tile)
                    );

                if (action == CaravanRules.TravelAction)
                    return Travel(caravan, clicked);

                Settlement settlement = Find.WorldObjects.SettlementBaseAt(clicked);
                if (settlement == null)
                    return ApiResult<CaravanSteerResultDto>.Fail(
                        SettlementRules.NoSettlementOnTile(request.Tile)
                    );

                string problem = SettlementProblem(caravan, settlement, action);
                if (problem != null)
                    return ApiResult<CaravanSteerResultDto>.Fail(problem);

                CaravanArrivalAction arrival = ArrivalAction(settlement, action);
                if (!arrival.StillValid(caravan, settlement.Tile).Accepted)
                {
                    return ApiResult<CaravanSteerResultDto>.Fail(
                        SettlementRules.NoReasonGiven(settlement.Label, action)
                    );
                }

                if (!caravan.pather.StartPath(settlement.Tile, arrival, repathImmediately: true))
                {
                    return ApiResult<CaravanSteerResultDto>.Fail(
                        CaravanRules.NoPath(settlement.Tile.tileId)
                    );
                }

                return ApiResult<CaravanSteerResultDto>.Ok(
                    SteerAnswer(caravan, settlement.Tile, action, arrival.Label,
                        "the game's own arrival action is set: the caravan walks to the settlement and "
                        + (action == CaravanRules.TradeAction
                            ? "the trade dialog opens when it arrives (/api/v1/ui/dialog reads it)"
                            : "enters its map when it arrives"))
                );
            }
            catch (Exception ex)
            {
                return ApiResult<CaravanSteerResultDto>.Fail(ex.ToString());
            }
        }

        /// <summary>
        /// The trade order with a settlement (fox ticket #4226, wave 2). The game's
        /// own CaravanArrivalAction_Trade is the whole lever: the caravan walks to
        /// the settlement, and the arrival opens Dialog_Trade, the window a player
        /// trades in. A caravan that already stands there arrives at once, which is
        /// what the player's own click does too (StartPath sees it is at the
        /// destination and runs the arrival action immediately).
        /// </summary>
        public ApiResult<CaravanTradeResultDto> Trade(CaravanTradeRequest request)
        {
            try
            {
                if (request == null)
                    return ApiResult<CaravanTradeResultDto>.Fail(
                        "a trade call needs a body: caravan_id and settlement_id"
                    );

                Caravan caravan = CaravanHelper.GetCaravanById(request.CaravanId);
                if (caravan == null)
                    return ApiResult<CaravanTradeResultDto>.Fail(
                        CaravanRules.UnknownCaravan(request.CaravanId)
                    );
                if (!caravan.IsPlayerControlled)
                    return ApiResult<CaravanTradeResultDto>.Fail(CaravanRules.NotOurs(caravan.Label));

                Settlement settlement = Find
                    .WorldObjects.AllWorldObjects.OfType<Settlement>()
                    .FirstOrDefault(s => s.ID == request.SettlementId);
                if (settlement == null)
                    return ApiResult<CaravanTradeResultDto>.Fail(
                        SettlementRules.UnknownSettlement(request.SettlementId)
                    );

                // CanTradeWith answers yes or no; the facts behind it are named
                // here, so a refusal says what is missing instead of only "no".
                string problem = SettlementProblem(caravan, settlement, CaravanRules.TradeAction);
                if (problem != null)
                    return ApiResult<CaravanTradeResultDto>.Fail(problem);

                var arrival = new CaravanArrivalAction_Trade(settlement);
                bool atSettlement = caravan.Tile == settlement.Tile && !caravan.pather.Moving;
                if (!caravan.pather.StartPath(settlement.Tile, arrival, repathImmediately: true))
                {
                    return ApiResult<CaravanTradeResultDto>.Fail(
                        CaravanRules.NoPath(settlement.Tile.tileId)
                    );
                }

                bool dialogOpen = Find.WindowStack.Windows.Any(w => w is Dialog_Trade);
                int ticks = atSettlement
                    ? 0
                    : CaravanArrivalTimeEstimator.EstimatedTicksToArrive(caravan, allowCaching: true);
                return ApiResult<CaravanTradeResultDto>.Ok(
                    new CaravanTradeResultDto
                    {
                        CaravanId = caravan.ID,
                        CaravanName = caravan.Name,
                        SettlementId = settlement.ID,
                        SettlementName = settlement.Label,
                        Tile = settlement.Tile.tileId,
                        AtSettlement = atSettlement,
                        DialogOpen = dialogOpen,
                        Negotiator = BestCaravanPawnUtility
                            .FindBestNegotiator(caravan, settlement.Faction, settlement.TraderKind)
                            ?.LabelShortCap,
                        TicksToArrive = ticks,
                        DaysToArrive = ticks / 60000f,
                        Note = dialogOpen
                            ? "the game's trade dialog is open: /api/v1/ui/dialog reads it, and the window "
                                + "endpoints press its buttons"
                            : "the caravan walks to the settlement now; the game opens its trade dialog on "
                                + "arrival, which /api/v1/ui/dialog then reads",
                    }
                );
            }
            catch (Exception ex)
            {
                return ApiResult<CaravanTradeResultDto>.Fail(ex.ToString());
            }
        }

        /// <summary>
        /// The plain travel order, exactly what WorldSelector.AutoOrderToTileNow
        /// runs for a right-click on an empty tile: the nearest reachable tile to
        /// the one clicked, the pather with no arrival action, and the game's own
        /// "ordered to tile" mote so the order is visible.
        /// </summary>
        private static ApiResult<CaravanSteerResultDto> Travel(Caravan caravan, PlanetTile clicked)
        {
            PlanetTile destination = CaravanUtility.BestGotoDestNear(clicked, caravan);
            if (!destination.Valid)
                return ApiResult<CaravanSteerResultDto>.Fail(CaravanRules.NoPath(clicked.tileId));

            if (!caravan.pather.StartPath(destination, null, repathImmediately: true))
                return ApiResult<CaravanSteerResultDto>.Fail(CaravanRules.NoPath(destination.tileId));

            caravan.gotoMote?.OrderedToTile(destination);
            return ApiResult<CaravanSteerResultDto>.Ok(
                SteerAnswer(caravan, destination, CaravanRules.TravelAction, null,
                    "the caravan walks there and nothing happens on arrival; pass action=visit or "
                    + "action=trade for a settlement, which runs the game's own arrival action")
            );
        }

        /// <summary>
        /// Why the game would not send the caravan to this settlement for this
        /// action. Visit and Trade each check their own facts; the wording of the
        /// ones that answer only yes or no lives in SettlementRules, so it is
        /// tested without the game.
        /// </summary>
        private static string SettlementProblem(Caravan caravan, Settlement settlement, string action)
        {
            bool playerFaction = settlement.Faction == Faction.OfPlayer;
            if (action == CaravanRules.VisitAction)
            {
                string visitProblem = SettlementRules.VisitProblem(settlement.Spawned, settlement.Visitable);
                return visitProblem == null ? null : settlement.Label + ": " + visitProblem;
            }

            string tradeProblem = SettlementRules.TradeProblem(
                settlement.Spawned,
                settlement.HasMap,
                playerFaction,
                settlement.Faction != null && settlement.Faction.def.permanentEnemy,
                settlement.Faction != null && settlement.Faction.HostileTo(Faction.OfPlayer),
                settlement.CanTradeNow,
                CaravanArrivalAction_Trade.HasNegotiator(caravan, settlement)
            );
            return tradeProblem == null ? null : settlement.Label + ": " + tradeProblem;
        }

        private static CaravanArrivalAction ArrivalAction(Settlement settlement, string action)
        {
            return action == CaravanRules.VisitAction
                ? (CaravanArrivalAction)new CaravanArrivalAction_VisitSettlement(settlement)
                : new CaravanArrivalAction_Trade(settlement);
        }

        private static CaravanSteerResultDto SteerAnswer(Caravan caravan, PlanetTile destination,
            string action, string label, string note)
        {
            int ticks = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(caravan, allowCaching: true);
            return new CaravanSteerResultDto
            {
                CaravanId = caravan.ID,
                CaravanName = caravan.Name,
                Tile = destination.tileId,
                Action = action,
                Label = label,
                TilesInPath = CaravanHelper.GetFullCaravanPath(caravan).Count,
                TicksToArrive = ticks,
                DaysToArrive = ticks / 60000f,
                Note = note,
            };
        }

        /// <summary>
        /// The tile the caravan travels to, validated against the planet the
        /// game loaded. A tile id the planet does not hold is refused instead of
        /// handed to the pathfinder.
        /// </summary>
        private static string ResolveDestination(int tileId, Map map, out PlanetTile destination)
        {
            destination = PlanetTile.Invalid;
            if (tileId < 0 || tileId >= Find.WorldGrid.TilesCount)
                return CaravanRules.BadDestination(tileId);
            if (tileId == map.Tile.tileId)
                return CaravanRules.SameTile(tileId);
            destination = new PlanetTile(tileId);
            return null;
        }

        /// <summary>
        /// The named pawns, each checked against the game's own sendable list so
        /// the refusal names the fact that stops him instead of failing later
        /// inside the lord job.
        /// </summary>
        private static string ResolvePawns(List<int> ids, Map map, out List<Pawn> pawns)
        {
            pawns = new List<Pawn>();
            // The list the dialog offers, with lodgers allowed: a guest who
            // joined the colony is a pawn the player may send too.
            List<Pawn> sendable = CaravanFormingUtility.AllSendablePawns(
                map,
                allowEvenIfDowned: false,
                allowEvenIfInMentalState: false,
                allowEvenIfPrisonerNotSecure: false,
                allowCapturableDownedPawns: false,
                allowLodgers: true,
                allowLoadAndEnterTransportersLordForGroupID: -1
            );

            foreach (int id in ids)
            {
                Pawn pawn = PawnHelper.FindPawnById(id);
                if (pawn == null)
                    return CaravanRules.UnknownPawn(id);
                if (pawns.Contains(pawn))
                    continue;

                bool onThisMap = pawn.Spawned && pawn.MapHeld == map;
                if (!onThisMap)
                    return CaravanRules.NotSendable(pawn.LabelShortCap, "he is not on this map");

                if (!sendable.Contains(pawn))
                {
                    string why = CaravanRules.PawnProblem(
                        pawn.Dead,
                        pawn.Downed,
                        pawn.IsPrisoner,
                        pawn.InMentalState,
                        pawn.IsFormingCaravan(),
                        onThisMap
                    );
                    return CaravanRules.NotSendable(pawn.LabelShortCap, why);
                }
                pawns.Add(pawn);
            }
            return null;
        }

        /// <summary>
        /// A neighbour of the colony's tile in the direction of the destination:
        /// what the dialog's route planner hands the game as the starting tile,
        /// which is how the exit cell is picked.
        /// </summary>
        private static string ResolveDirectionTile(Map map, PlanetTile destination, out PlanetTile directionTile)
        {
            directionTile = PlanetTile.Invalid;
            var neighbors = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(map.Tile, neighbors);
            if (neighbors.Count == 0)
                return CaravanRules.NoValidExitTileKey.Translate().ToString();

            Direction8Way heading = Find.WorldGrid.GetDirection8WayFromTo(map.Tile, destination);
            foreach (PlanetTile neighbor in neighbors)
            {
                if (Find.WorldGrid.GetDirection8WayFromTo(map.Tile, neighbor) == heading)
                {
                    directionTile = neighbor;
                    return null;
                }
            }
            // A destination straight above or below has no neighbour of its own
            // heading; the first neighbour is still a valid exit direction.
            directionTile = neighbors[0];
            return null;
        }

        /// <summary>
        /// The goods to load, as the dialog's transferables. The pawns are in the
        /// list too, because the mass arithmetic needs their carrying capacity;
        /// StartFormingCaravan drops them itself.
        /// </summary>
        private static string BuildTransferables(List<CaravanItemRequest> items, Map map, List<Pawn> pawns,
            out List<TransferableOneWay> transferables)
        {
            transferables = new List<TransferableOneWay>();

            foreach (Pawn pawn in pawns)
            {
                var pawnTransfer = new TransferableOneWay();
                pawnTransfer.things.Add(pawn);
                pawnTransfer.ForceTo(1);
                transferables.Add(pawnTransfer);
            }

            if (items == null)
                return null;

            foreach (CaravanItemRequest item in items)
            {
                if (item == null || item.Count <= 0)
                    continue;
                Thing thing = map.listerThings.AllThings.FirstOrDefault(t => t.thingIDNumber == item.ThingId);
                if (thing == null)
                    return CaravanRules.UnknownThing(item.ThingId);

                var transfer = new TransferableOneWay();
                transfer.things.Add(thing);
                transfer.ForceTo(Math.Min(item.Count, thing.stackCount));
                transferables.Add(transfer);
            }
            return null;
        }

        /// <summary>
        /// The cell the caravan leaves through: the closest map edge to where the
        /// pawns stand, the same search the dialog runs before it starts the
        /// gather jobs.
        /// </summary>
        private static string ResolveExitSpot(List<Pawn> pawns, Map map, out IntVec3 exitSpot)
        {
            IntVec3 root = pawns.Aggregate(IntVec3.Zero, (acc, p) => acc + p.Position) / pawns.Count;
            if (!RCellFinder.TryFindClosestEdgeCellTo(root, map, out exitSpot))
                return "the game found no map edge to leave through (CaravanCouldNotFindExitSpot)";
            return null;
        }

        /// <summary>
        /// Where the pawns gather: a reachable caravan packing spot, else a spot
        /// just outside the colony, the dialog's own two-step search.
        /// </summary>
        private static string ResolveMeetingPoint(IntVec3 exitSpot, Map map, out IntVec3 meetingPoint)
        {
            var spots = new List<Thing>();
            TraverseParms parms = TraverseParms.For(TraverseMode.PassDoors);
            foreach (Thing thing in map.listerThings.ThingsOfDef(ThingDefOf.CaravanPackingSpot))
            {
                if (map.reachability.CanReach(exitSpot, thing, PathEndMode.OnCell, parms))
                    spots.Add(thing);
            }
            if (spots.Count > 0)
            {
                meetingPoint = spots.RandomElement().Position;
                return null;
            }
            if (RCellFinder.TryFindRandomSpotJustOutsideColony(exitSpot, map, out meetingPoint))
                return null;
            return "the game found no spot where the caravan can gather (CaravanCouldNotFindPackingSpot)";
        }

        private static FormCaravanResultDto Answer(List<Pawn> pawns, List<CaravanItemRequest> items,
            IntVec3 exitSpot, IntVec3 meetingPoint, PlanetTile startingTile, PlanetTile directionTile,
            PlanetTile destination, float usage, float capacity)
        {
            return new FormCaravanResultDto
            {
                Pawns = pawns
                    .Select(p => new CaravanPawnDto
                    {
                        Id = p.thingIDNumber,
                        Name = p.LabelShortCap,
                        Downed = p.Downed,
                        X = p.Position.x,
                        Z = p.Position.z,
                    })
                    .ToList(),
                Items = (items ?? new List<CaravanItemRequest>())
                    .Where(i => i != null && i.Count > 0)
                    .Select(i => i.Count + " x thing " + i.ThingId)
                    .ToList(),
                ExitX = exitSpot.x,
                ExitZ = exitSpot.z,
                MeetingX = meetingPoint.x,
                MeetingZ = meetingPoint.z,
                StartingTile = startingTile.tileId,
                DirectionTile = directionTile.tileId,
                DestinationTile = destination.tileId,
                MassUsage = usage,
                MassCapacity = capacity,
                Note =
                    "the game's own gather jobs run now: the pawns walk to the meeting cell, haul the named goods "
                    + "and leave through the exit cell, and the caravan appears in /api/v1/world/caravans once it "
                    + "has left the map, which takes game time",
            };
        }
    }
}
