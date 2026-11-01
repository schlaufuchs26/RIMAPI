using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;

// Roofing is pawn work, and a play run could not see that (fox ticket #3418):
// a sealed room stays at the outside temperature, the Build roof area only
// marks cells, and a constructor has to build each 3x3 block. Colony 12 read
// "barracks 25 cells, 20 open roofs" for a day after the area covered it, until
// a colonist happened to get to it. The order below does what a player does
// with the area painted: it marks the area and queues the game's own
// build-roof job, so the work starts on this call instead of whenever the work
// giver reaches it.
public partial class OrderService
{
    // A roof job covers a 3x3 block, so a room of 35 cells needs about four
    // jobs; more constructors than that would just queue work that waits.
    private const int MaxRoofJobs = 4;

    // Rooms named in the answer; a rectangle over a whole base touches many,
    // and the caller only needs the ones it is trying to warm.
    private const int MaxRoomsNamed = 4;

    public ApiResult<RoofResultDto> BuildRoof(RoofRequestDto request)
    {
        try
        {
            Map map = MapLookup.Resolve(request.MapId, MapHelper.GetMapByID, out string mapError);
            if (map == null)
            {
                return ApiResult<RoofResultDto>.Fail(mapError);
            }
            if (request.PointA == null || request.PointB == null)
            {
                return ApiResult<RoofResultDto>.Fail(
                    "point_a and point_b are required: the rectangle to roof.");
            }

            CellRect rect = CellRect.FromLimits(
                Math.Min(request.PointA.X, request.PointB.X),
                Math.Min(request.PointA.Z, request.PointB.Z),
                Math.Max(request.PointA.X, request.PointB.X),
                Math.Max(request.PointA.Z, request.PointB.Z));

            RoofResultDto result = new RoofResultDto
            {
                Jobs = new List<RoofJobDto>(),
                Rooms = new List<RoofRoomDto>(),
            };

            // Every in-bounds cell is one of five cases, and the counts are what
            // a caller reads instead of the map. The area is written for the
            // cells a roof can stand on, because the job driver fails on a cell
            // outside it (JobDriver_BuildRoof.MakeNewToils) and roofs only area
            // cells around it.
            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                {
                    result.Outside++;
                    continue;
                }
                result.Cells++;
                if (map.roofGrid.Roofed(cell))
                {
                    result.AlreadyRoofed++;
                    continue;
                }
                // A tree or bush under the cell blocks the roof; the game's own
                // work giver cuts it first and roofs afterwards, so the cell
                // keeps its area flag.
                if (RoofUtility.FirstBlockingThing(cell, map) != null)
                {
                    result.Blocked++;
                    MarkRoofArea(map, cell, result);
                    continue;
                }
                // No wall or roof holder within about seven cells: a roof there
                // would collapse, which is the game's own rule and not a
                // missing lever.
                if (!RoofCollapseUtility.WithinRangeOfRoofHolder(
                        cell, map, assumeNonNoRoofCellsAreRoofed: true))
                {
                    result.Unsupported++;
                    continue;
                }
                result.RoofWork++;
                MarkRoofArea(map, cell, result);
            }

            AddRoofRooms(map, rect, result);

            if (result.RoofWork == 0 && result.Blocked == 0)
            {
                result.Refusal = result.AlreadyRoofed > 0
                    ? "every cell of the rectangle is already roofed"
                    : "no cell of the rectangle can carry a roof: no wall or other roof holder within about seven cells";
                return ApiResult<RoofResultDto>.Ok(result);
            }

            string pickRefusal;
            List<Pawn> builders = PickRoofBuilders(map, request.PawnId, out pickRefusal);
            if (builders.Count == 0)
            {
                result.Refusal = pickRefusal;
                return ApiResult<RoofResultDto>.Ok(result);
            }

            // The game's own work giver and job, so its checks (reservation,
            // reach, roof support, a blocking plant) decide instead of a second
            // copy of them.
            WorkGiver_BuildRoof giver = new WorkGiver_BuildRoof();
            List<IntVec3> candidates = RoofCandidates(map, rect);
            HashSet<int> taken = new HashSet<int>();
            List<string> refused = new List<string>();
            foreach (Pawn builder in builders)
            {
                if (taken.Count >= candidates.Count)
                {
                    break;
                }
                IntVec3 chosen = IntVec3.Invalid;
                foreach (IntVec3 cell in candidates)
                {
                    if (taken.Contains(map.cellIndices.CellToIndex(cell)))
                    {
                        continue;
                    }
                    if (!giver.HasJobOnCell(builder, cell, true))
                    {
                        continue;
                    }
                    chosen = cell;
                    break;
                }
                if (!chosen.IsValid)
                {
                    refused.Add(builder.LabelShort);
                    continue;
                }
                Job job = giver.JobOnCell(builder, chosen, true);
                if (job == null)
                {
                    refused.Add(builder.LabelShort);
                    continue;
                }
                if (!builder.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    refused.Add(builder.LabelShort);
                    continue;
                }
                taken.Add(map.cellIndices.CellToIndex(chosen));
                result.Jobs.Add(new RoofJobDto
                {
                    PawnId = builder.thingIDNumber,
                    Pawn = builder.LabelShort,
                    X = chosen.x,
                    Z = chosen.z,
                });
            }

            if (result.Jobs.Count == 0)
            {
                result.Refusal = refused.Count > 0
                    ? "the game's own build-roof check refuses every cell of the rectangle for "
                        + string.Join(", ", refused)
                        + ": another pawn holds the cell, the cell is out of reach, or no roof holder stands within about seven cells"
                    : "no cell of the rectangle needs a roof job";
            }
            return ApiResult<RoofResultDto>.Ok(result);
        }
        catch (Exception ex)
        {
            return ApiResult<RoofResultDto>.Fail(ex.ToString());
        }
    }

    // MarkRoofArea turns one cell on in the Build roof area, the game's own
    // expand designator applied at once, and counts what it changed.
    private static void MarkRoofArea(Map map, IntVec3 cell, RoofResultDto result)
    {
        if (map.areaManager.BuildRoof[cell])
        {
            return;
        }
        map.areaManager.BuildRoof[cell] = true;
        result.AreaAdded++;
    }

    // RoofCandidates lists the cells a job should target: the 3x3 tiling of the
    // rectangle first, so the queued jobs do not overlap, then every other cell
    // of the rectangle as a fallback, because a tiling cell can be a wall a pawn
    // cannot stand next to or a cell another pawn already holds.
    private static List<IntVec3> RoofCandidates(Map map, CellRect rect)
    {
        List<IntVec3> cells = new List<IntVec3>();
        HashSet<int> seen = new HashSet<int>();
        foreach (RoofTiling.Cell tiled in RoofTiling.JobCells(rect.minX, rect.minZ, rect.maxX, rect.maxZ))
        {
            IntVec3 cell = new IntVec3(tiled.X, 0, tiled.Z);
            if (cell.InBounds(map) && seen.Add(map.cellIndices.CellToIndex(cell)))
            {
                cells.Add(cell);
            }
        }
        foreach (IntVec3 cell in rect)
        {
            if (seen.Add(map.cellIndices.CellToIndex(cell)))
            {
                cells.Add(cell);
            }
        }
        return cells;
    }

    // AddRoofRooms names the rooms the rectangle touches with the numbers a
    // caller compares the next `rooms` read against: a roofed room warms, an
    // open one tracks the outside temperature.
    private static void AddRoofRooms(Map map, CellRect rect, RoofResultDto result)
    {
        HashSet<int> seen = new HashSet<int>();
        foreach (IntVec3 cell in rect)
        {
            if (result.Rooms.Count >= MaxRoomsNamed)
            {
                return;
            }
            if (!cell.InBounds(map))
            {
                continue;
            }
            Room room = cell.GetRoom(map);
            if (room == null || room.Dereferenced || !seen.Add(room.ID))
            {
                continue;
            }
            result.Rooms.Add(new RoofRoomDto
            {
                RoomId = room.ID,
                Label = room.GetRoomRoleLabel(),
                Cells = room.CellCount,
                OpenRoofs = room.OpenRoofCount,
                TemperatureC = room.Temperature,
            });
        }
    }

    // PickRoofBuilders answers who takes the roof jobs: the named pawn when the
    // caller names one, otherwise the colony's constructors, the ones with
    // Construction on first. A pawn whose backstory disables construction is
    // refused by name, because that is the reason no roof appears at all.
    private static List<Pawn> PickRoofBuilders(Map map, int pawnId, out string refusal)
    {
        refusal = null;
        if (pawnId > 0)
        {
            Pawn pawn = map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.thingIDNumber == pawnId);
            if (pawn == null)
            {
                refusal = $"Pawn with ID={pawnId} not found";
                return new List<Pawn>();
            }
            // IsColonistPlayerControlled is false for a colonist in a mental
            // state, so the membership test alone refused a builder with
            // "not one of the colony's colonists" (ticket #3433).
            string state = CombatRefusal.StateOf(PawnHelper.RefusalStateOf(pawn));
            if (state != null)
            {
                refusal = $"{pawn.LabelShort} is {state}, so he cannot build";
                return new List<Pawn>();
            }
            if (pawn.WorkTagIsDisabled(WorkTags.Constructing))
            {
                refusal = $"{pawn.LabelShort} cannot construct: the backstory or a trait disables construction";
                return new List<Pawn>();
            }
            return new List<Pawn> { pawn };
        }

        List<Pawn> builders = map.mapPawns.FreeColonistsSpawned
            .Where(p => !p.Downed && !p.InMentalState)
            .Where(p => !p.WorkTagIsDisabled(WorkTags.Constructing))
            .OrderByDescending(p => p.workSettings != null && p.workSettings.WorkIsActive(WorkTypeDefOf.Construction))
            .ThenByDescending(p => p.GetStatValue(StatDefOf.ConstructionSpeed))
            .Take(MaxRoofJobs)
            .ToList();
        if (builders.Count == 0)
        {
            refusal = "no colonist can build a roof right now: every colonist is downed, in a mental state, or cannot construct";
        }
        return builders;
    }
}
