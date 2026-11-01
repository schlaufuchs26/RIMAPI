using System;
using System.Collections.Generic;
using System.Globalization;
using RimWorld;
using Verse;
using Verse.AI;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    // Cover answers the question the fight loop could not (ticket #3174): the
    // combat read names where everyone stands, but a run told to put shooters
    // behind a corner had to guess which cells shield them and which of those
    // can still shoot. Every number here is the game's own: the block chance is
    // CoverUtility.CalculateOverallBlockChance (directional, so a wall on the
    // wrong side reads 0), the cover thing is what the game's GridsUtility
    // reports for the neighbouring cell, and line of sight is GenSight.
    public class CoverService : ICoverService
    {
        private const int DefaultRadius = 6;
        private const int MaxRadius = 12;
        // A radius of 12 holds 400+ cells; a run picks a cell, it does not read
        // a map, so the answer carries the best few and the counts for the rest.
        private const int MaxCells = 12;

        public ApiResult<CoverStateDto> GetCover(
            int mapId,
            int pawnId,
            int x,
            int z,
            int radius,
            int targetId,
            int targetX,
            int targetZ
        )
        {
            try
            {
                Map map = MapLookup.Resolve(mapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult<CoverStateDto>.Fail(mapError);

                Pawn pawn = null;
                IntVec3 center;
                if (pawnId > 0)
                {
                    pawn = PawnHelper.FindPawnById(pawnId);
                    if (pawn == null) return ApiResult<CoverStateDto>.Fail($"no pawn with id {pawnId}");
                    if (!pawn.Spawned || pawn.Map != map)
                    {
                        return ApiResult<CoverStateDto>.Fail(
                            $"{pawn.LabelShort} (id {pawnId}) is not on map {mapId}");
                    }
                    center = pawn.Position;
                }
                else
                {
                    center = new IntVec3(x, 0, z);
                    if (!center.InBounds(map))
                    {
                        return ApiResult<CoverStateDto>.Fail(
                            $"cell {x},{z} is outside the map ({map.Size.x}x{map.Size.z})");
                    }
                }

                // The target is the shooter the cover is measured against: a
                // pawn (a raider) or a bare cell. Without it there is no
                // direction, and a cover chance without a direction is a guess.
                IntVec3 targetCell;
                Pawn target = null;
                if (targetId > 0)
                {
                    target = PawnHelper.FindPawnById(targetId);
                    if (target == null) return ApiResult<CoverStateDto>.Fail($"no pawn with id {targetId} to read cover against");
                    if (!target.Spawned || target.Map != map)
                    {
                        return ApiResult<CoverStateDto>.Fail(
                            $"{target.LabelShort} (id {targetId}) is not on map {mapId}");
                    }
                    targetCell = target.Position;
                }
                else
                {
                    targetCell = new IntVec3(targetX, 0, targetZ);
                    if (!targetCell.InBounds(map))
                    {
                        return ApiResult<CoverStateDto>.Fail(
                            $"target cell {targetX},{targetZ} is outside the map ({map.Size.x}x{map.Size.z})");
                    }
                }

                if (radius <= 0) radius = DefaultRadius;
                if (radius > MaxRadius) radius = MaxRadius;

                CoverStateDto state = new CoverStateDto
                {
                    MapId = mapId,
                    X = center.x,
                    Z = center.z,
                    PawnId = pawn != null ? pawn.thingIDNumber : 0,
                    PawnName = pawn != null ? pawn.LabelShort : "",
                    TargetId = target != null ? target.thingIDNumber : 0,
                    TargetName = target != null ? target.LabelShort : "",
                    TargetX = targetCell.x,
                    TargetZ = targetCell.z,
                    Radius = radius,
                    Cells = new List<CoverCellDto>(),
                };

                List<CoverCellDto> candidates = new List<CoverCellDto>();
                int radiusSquared = radius * radius;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        if (dx * dx + dz * dz > radiusSquared) continue;
                        IntVec3 cell = new IntVec3(center.x + dx, 0, center.z + dz);
                        if (!cell.InBounds(map)) continue;
                        if (!cell.Standable(map)) continue;
                        state.StandableCount++;

                        bool los = GenSight.LineOfSight(cell, targetCell, map);
                        if (los) state.LineOfSightCount++;

                        List<string> providers = CoverProviders(cell, targetCell, map);
                        if (providers.Count == 0) continue;
                        state.CoverNeighbourCount++;

                        float cover = CoverUtility.CalculateOverallBlockChance(
                            new LocalTargetInfo(cell), targetCell, map);
                        if (cover > 0f) state.CoverCount++;
                        if (cover > 0f && los) state.CoverAndLineOfSightCount++;
                        // A cell that neither shields the pawn nor has a shot
                        // is not a place to send anyone: the counts still say
                        // it exists, the list does not.
                        if (cover <= 0f && !los) continue;

                        candidates.Add(new CoverCellDto
                        {
                            X = cell.x,
                            Z = cell.z,
                            Cover = cover,
                            Providers = providers,
                            Standable = true,
                            LineOfSight = los,
                            Distance = cell.DistanceTo(targetCell),
                        });
                    }
                }

                // A cell that can shoot comes first, then the most cover, then
                // the closer one: a cell that only shields the pawn is no use to
                // a shooter, so it must not push the useful cells off the list.
                state.MatchingCount = candidates.Count;
                candidates.Sort((a, b) =>
                {
                    if (a.LineOfSight != b.LineOfSight) return a.LineOfSight ? -1 : 1;
                    int byCover = b.Cover.CompareTo(a.Cover);
                    if (byCover != 0) return byCover;
                    return a.Distance.CompareTo(b.Distance);
                });

                if (candidates.Count > MaxCells) candidates.RemoveRange(MaxCells, candidates.Count - MaxCells);
                // The walk check is a region search per cell, so it runs on the
                // cells the answer carries, never on the whole radius.
                if (pawn != null)
                {
                    foreach (CoverCellDto cell in candidates)
                    {
                        IntVec3 c = new IntVec3(cell.X, 0, cell.Z);
                        cell.Reachable = map.reachability.CanReach(
                            pawn.Position, c, PathEndMode.OnCell, WalkParms.For(pawn, Danger.Some));
                    }
                }
                state.Cells = candidates;
                return ApiResult<CoverStateDto>.Ok(state);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error reading cover on map {mapId}: {ex}");
                return ApiResult<CoverStateDto>.Fail(ex.Message);
            }
        }

        // CoverProviders names the things in the eight neighbouring cells that
        // the game counts as cover, with the cell each stands on, so a run can
        // tell a wall from a sandbag from a rock chunk. The target's own cell is
        // skipped: the raider is not the colonist's cover.
        private static List<string> CoverProviders(IntVec3 cell, IntVec3 targetCell, Map map)
        {
            List<string> providers = new List<string>();
            IntVec3[] adjacent = GenAdj.AdjacentCells;
            for (int i = 0; i < adjacent.Length; i++)
            {
                IntVec3 neighbour = cell + adjacent[i];
                if (!neighbour.InBounds(map)) continue;
                if (neighbour == targetCell) continue;
                Thing cover = neighbour.GetCover(map);
                if (cover == null) continue;
                providers.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} ({1},{2})",
                    cover.LabelShort,
                    neighbour.x,
                    neighbour.z));
            }
            return providers;
        }
    }
}
