using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;

// Fighting a named fire (fox ticket #3444). A wildfire took colony 12 apart on
// day 7: the fires read listed 175 burning cells and then 637, and the only
// lever was the Firefighter work priority, which sends a colonist to whatever
// fire the game picks. When the barracks stood at 249 C with a downed colonist
// inside, the answer was to open a door, which needs a free colonist the same
// fire kept busy elsewhere. This order does what a player's right-click does:
// it puts one colonist on one cell with the game's own BeatFire job.
public partial class OrderService
{
    // More assignments than this queue work that waits: a field fire has
    // hundreds of cells and no colony has eight firefighters standing idle.
    private const int MaxFirefightAssignments = 8;

    public ApiResult<FirefightResultDto> Firefight(FirefightRequestDto request)
    {
        try
        {
            Map map = MapHelper.GetMapByID(request.MapId);
            if (map == null)
            {
                return ApiResult<FirefightResultDto>.Fail($"Map with id: {request.MapId} not found");
            }

            string area = (request.Area ?? "").Trim();
            bool home = string.Equals(area, "home", StringComparison.OrdinalIgnoreCase);
            if (!home && area.Length > 0)
            {
                return ApiResult<FirefightResultDto>.Fail(
                    $"area \"{request.Area}\" is not a scope this order knows: give area=home for the colony's "
                    + "Home area, or the rectangle corners point_a and point_b");
            }
            bool hasRect = request.PointA != null && request.PointB != null;
            if (home && hasRect)
            {
                return ApiResult<FirefightResultDto>.Fail(
                    "area=home and a rectangle are two scopes: give one of them");
            }
            if (!home && !hasRect)
            {
                return ApiResult<FirefightResultDto>.Fail(
                    "point_a and point_b, or area=home, are required: the order fights the fire of a named cell");
            }

            CellRect rect = default(CellRect);
            if (hasRect)
            {
                rect = CellRect.FromLimits(
                    Math.Min(request.PointA.X, request.PointB.X),
                    Math.Min(request.PointA.Z, request.PointB.Z),
                    Math.Max(request.PointA.X, request.PointB.X),
                    Math.Max(request.PointA.Z, request.PointB.Z));
            }

            FirefightResultDto result = new FirefightResultDto
            {
                Home = home,
                Assignments = new List<FirefightAssignmentDto>(),
                Skipped = new List<FirefightSkipDto>(),
            };

            // The game's own fire list, one Fire thing per burning cell. Two
            // fires can share a cell; the bigger one is the job's target, and
            // the cell is counted once, because one colonist is what a cell
            // needs.
            Dictionary<IntVec3, Fire> cells = new Dictionary<IntVec3, Fire>();
            foreach (Thing thing in map.listerThings.ThingsOfDef(ThingDefOf.Fire))
            {
                Fire fire = thing as Fire;
                if (fire == null || fire.Destroyed || !fire.Spawned)
                {
                    continue;
                }
                IntVec3 pos = fire.Position;
                if (!pos.InBounds(map))
                {
                    continue;
                }
                if (home ? !map.areaManager.Home[pos] : !rect.Contains(pos))
                {
                    continue;
                }
                result.Fires++;
                Fire known;
                if (cells.TryGetValue(pos, out known))
                {
                    if (fire.fireSize > known.fireSize)
                    {
                        cells[pos] = fire;
                    }
                    continue;
                }
                cells[pos] = fire;
            }
            result.Cells = cells.Count;
            if (cells.Count == 0)
            {
                return ApiResult<FirefightResultDto>.Ok(result);
            }

            string pickRefusal;
            List<Pawn> colonists = Firefighters(map, request.PawnId, out pickRefusal);
            if (colonists.Count == 0)
            {
                result.Refusal = pickRefusal;
                return ApiResult<FirefightResultDto>.Ok(result);
            }

            List<FirefightPlan.BurningCell> burning = cells.Keys
                .OrderBy(c => c.z)
                .ThenBy(c => c.x)
                .Select(c => new FirefightPlan.BurningCell { X = c.x, Z = c.z })
                .ToList();
            List<FirefightPlan.FreePawn> free = colonists
                .Select(p => new FirefightPlan.FreePawn
                {
                    Id = p.thingIDNumber,
                    Name = p.LabelShort,
                    X = p.Position.x,
                    Z = p.Position.z,
                    FirefighterActive = p.workSettings != null
                        && p.workSettings.WorkIsActive(WorkTypeDefOf.Firefighter),
                })
                .ToList();
            int limit = request.Limit > 0 ? Math.Min(request.Limit, MaxFirefightAssignments) : MaxFirefightAssignments;

            // The plan pairs colonists with cells; the game still has the last
            // word on each pair (reservation, reach), so a refused pair is
            // reported instead of counted as queued.
            HashSet<int> assigned = new HashSet<int>();
            foreach (FirefightPlan.Assignment assignment in FirefightPlan.Assign(burning, free, limit))
            {
                IntVec3 pos = new IntVec3(assignment.FireX, 0, assignment.FireZ);
                Fire fire;
                if (!cells.TryGetValue(pos, out fire))
                {
                    continue;
                }
                Pawn pawn = colonists.FirstOrDefault(p => p.thingIDNumber == assignment.PawnId);
                if (pawn == null)
                {
                    continue;
                }
                string refusal = FirefightRefusal(pawn, fire);
                if (refusal == null)
                {
                    Job job = JobMaker.MakeJob(JobDefOf.BeatFire, fire);
                    if (!pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                    {
                        refusal = $"{pawn.LabelShort} did not take the job: the game refused the order";
                    }
                }
                if (refusal != null)
                {
                    result.Skipped.Add(new FirefightSkipDto { X = pos.x, Z = pos.z, Reason = refusal });
                    continue;
                }
                assigned.Add(map.cellIndices.CellToIndex(pos));
                result.Assignments.Add(new FirefightAssignmentDto
                {
                    X = pos.x,
                    Z = pos.z,
                    PawnId = pawn.thingIDNumber,
                    Pawn = pawn.LabelShort,
                });
            }

            // Every burning cell the call left alone gets a reason, so a run
            // reads "nobody free" instead of a shorter list it cannot explain.
            foreach (FirefightPlan.BurningCell cell in burning)
            {
                if (assigned.Contains(map.cellIndices.CellToIndex(new IntVec3(cell.X, 0, cell.Z))))
                {
                    continue;
                }
                if (result.Skipped.Any(s => s.X == cell.X && s.Z == cell.Z))
                {
                    continue;
                }
                string reason = result.Assignments.Count >= limit
                    ? $"beyond this call's limit of {limit} jobs; call again for the next cells"
                    : "no free colonist was left for this cell";
                result.Skipped.Add(new FirefightSkipDto { X = cell.X, Z = cell.Z, Reason = reason });
            }

            return ApiResult<FirefightResultDto>.Ok(result);
        }
        catch (Exception ex)
        {
            return ApiResult<FirefightResultDto>.Fail(ex.ToString());
        }
    }

    // Firefighters is the pool an order draws from: one named colonist, or
    // every free colonist who can fight a fire at all. The refusal names each
    // colonist's own reason, because "nobody is free" hides that the barracks'
    // firefighter is drafted.
    private static List<Pawn> Firefighters(Map map, int pawnId, out string refusal)
    {
        refusal = null;
        if (pawnId > 0)
        {
            Pawn named = map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber == pawnId);
            if (named == null)
            {
                refusal = $"no pawn with id {pawnId} is on this map";
                return new List<Pawn>();
            }
            string why = CannotFirefight(named);
            if (why != null)
            {
                refusal = $"{named.LabelShort} cannot fight a fire: {why}";
                return new List<Pawn>();
            }
            return new List<Pawn> { named };
        }

        List<Pawn> able = map.mapPawns.FreeColonistsSpawned
            .Where(p => CannotFirefight(p) == null)
            .ToList();
        if (able.Count == 0)
        {
            List<string> reasons = map.mapPawns.FreeColonistsSpawned
                .Select(p => p.LabelShort + " " + CannotFirefight(p))
                .ToList();
            refusal = reasons.Count == 0
                ? "the colony has no free colonist on this map"
                : "no colonist can fight a fire right now: " + string.Join("; ", reasons);
        }
        return able;
    }

    // CannotFirefight states why a pawn cannot beat a fire, in the game's own
    // terms, or null when he can.
    private static string CannotFirefight(Pawn pawn)
    {
        if (pawn.Dead)
        {
            return "is dead";
        }
        if (pawn.Downed)
        {
            return "is downed";
        }
        if (pawn.InMentalState)
        {
            return "is in a mental state";
        }
        if (pawn.Drafted)
        {
            return "is drafted, and a drafted pawn only takes move, attack and hold orders (undraft him first)";
        }
        if (!pawn.IsColonistPlayerControlled)
        {
            return "is not one of the colony's colonists";
        }
        if (pawn.WorkTagIsDisabled(WorkTags.Firefighting))
        {
            return "cannot fight fires (his backstory or a trait disables it)";
        }
        return null;
    }

    // FirefightRefusal is the game's own check on one pair, so the answer never
    // claims a job the colonist cannot walk to.
    private static string FirefightRefusal(Pawn pawn, Fire fire)
    {
        if (fire == null || fire.Destroyed)
        {
            return "the fire went out before the order was queued";
        }
        if (!pawn.CanReserve(fire, 1, -1, null, true))
        {
            return $"{pawn.LabelShort} cannot reserve the fire: another colonist already has it";
        }
        if (!pawn.CanReach(fire, PathEndMode.Touch, Danger.Deadly))
        {
            return $"{pawn.LabelShort} cannot reach the cell: the path is blocked or the area is forbidden";
        }
        return null;
    }
}
