using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    // The order that keeps the range (fox ticket #4175). `attack` charges a
    // target beyond the weapon's reach because that is what the game's own
    // right-click does (#4163); a gunner ordered onto a raider 120 cells away
    // then walks into contact instead of firing. `shoot` picks the cell that has
    // the shot and walks the pawn there first, so the shot follows the approach
    // instead of a swing.
    public partial class CombatService
    {
        private ApiResult<OrderResultDto> Shoot(Pawn pawn, OrderRequestDto request, OrderResultDto result)
        {
            Pawn target = PawnHelper.FindPawnById(request.TargetId);
            if (target == null)
            {
                return ApiResult<OrderResultDto>.Fail($"no pawn with id {request.TargetId} to shoot at");
            }
            if (target.Dead || !target.Spawned)
            {
                return ApiResult<OrderResultDto>.Fail($"{target.LabelShort} is dead or gone");
            }
            if (target.IsColonist)
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"{target.LabelShort} is a colonist; the tool does not order friendly fire");
            }
            if (!Hostility.IsHostileToColony(target))
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"{target.LabelShort} is not hostile, so the colony has no fight with him");
            }
            Verb ranged = RangedVerb(pawn);
            if (ranged == null)
            {
                return ApiResult<OrderResultDto>.Fail(ShootRules.NoRanged(pawn.LabelShort));
            }
            Danger maxDanger;
            string dangerError;
            if (!TryParseDanger(request.MaxDanger, out maxDanger, out dangerError))
            {
                return ApiResult<OrderResultDto>.Fail(dangerError);
            }
            ShotPlan plan = PlanShot(pawn, target, ranged);
            if (!plan.Facts.InRange && !plan.Facts.Found)
            {
                return ApiResult<OrderResultDto>.Fail(ShootRules.NoCellReason(plan.Facts));
            }
            Job shot = JobMaker.MakeJob(JobDefOf.AttackStatic, target);
            // The verb is named and the job ends when the target comes too close
            // to shoot, exactly as the ranged half of an attack order (#4160).
            shot.verbToUse = ranged;
            shot.endIfCantShootInMelee = true;
            result.TargetId = target.thingIDNumber;
            result.Target = target.LabelShort;
            if (plan.Facts.InRange)
            {
                if (!pawn.jobs.TryTakeOrderedJob(shot, JobTag.Misc))
                {
                    return ApiResult<OrderResultDto>.Fail($"{pawn.LabelShort} refused the attack order");
                }
                result.Job = shot.def.defName;
                result.X = pawn.Position.x;
                result.Z = pawn.Position.z;
                result.Note = ShootRules.PlanNote(plan.Facts);
                return ApiResult<OrderResultDto>.Ok(Fill(pawn, result));
            }
            // The cell is one the pawn can reach (PlanShot checked it with the
            // same call a move uses), so the walk is read for its hazards and
            // length before the jobs go out.
            WalkReport walk = InspectWalk(pawn, plan.Cell);
            if (walk.Worst > maxDanger)
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"{pawn.LabelShort} cannot reach {plan.Facts.Cell} from {plan.Facts.From} at " +
                    $"max_danger={DangerText(maxDanger)}: the walk the game would take runs through " +
                    $"{walk.HazardText()}, so the pawn would take damage on the way; " +
                    "pass max_danger=deadly to order it anyway");
            }
            plan.Facts.Walk = walk.Cells;
            Job approach = JobMaker.MakeJob(JobDefOf.Goto, plan.Cell);
            if (!pawn.jobs.TryTakeOrderedJob(approach, JobTag.Misc))
            {
                return ApiResult<OrderResultDto>.Fail($"{pawn.LabelShort} refused the walk to {plan.Facts.Cell}");
            }
            // The shot waits behind the walk in the pawn's own job queue, so the
            // game starts it when the pawn arrives instead of needing a second
            // order from the run.
            pawn.jobs.jobQueue.EnqueueLast(shot);
            result.Job = approach.def.defName;
            result.X = plan.Cell.x;
            result.Z = plan.Cell.z;
            result.Reachable = true;
            result.Reach = walk.ReachText();
            result.PathDanger = DangerText(walk.Worst);
            result.PathHazards = walk.HazardText();
            result.Note = ShootRules.PlanNote(plan.Facts);
            if (walk.Worst != Danger.None)
            {
                result.Note += $"; the path crosses {walk.HazardText()}, tolerated at max_danger={DangerText(maxDanger)}";
            }
            return ApiResult<OrderResultDto>.Ok(Fill(pawn, result));
        }

        // ShotPlan is the cell a shot order walks to, with the facts the answer
        // words. The cell stays out of ShootRules, which the game-free tests
        // compile.
        private sealed class ShotPlan
        {
            public ShootRules.Plan Facts;
            public IntVec3 Cell;
        }

        // PlanShot finds the cell a shot order walks to: every cell within the
        // weapon's reach of the target that has it in sight, nearest to the pawn
        // first, so the walk is as short as it can be. The pawn's own cell is
        // tried first: a weapon that can hit from where it stands needs no walk.
        private static ShotPlan PlanShot(Pawn pawn, Pawn target, Verb ranged)
        {
            ShotPlan plan = new ShotPlan();
            plan.Facts = new ShootRules.Plan
            {
                From = $"{pawn.Position.x},{pawn.Position.z}",
                Target = $"{target.Position.x},{target.Position.z}",
                Range = ranged.EffectiveRange,
                Distance = pawn.Position.DistanceTo(target.Position),
            };
            if (ranged.CanHitTargetFrom(pawn.Position, target))
            {
                plan.Facts.InRange = true;
                return plan;
            }
            Map map = pawn.Map;
            float minRange = ranged.verbProps != null ? ranged.verbProps.EffectiveMinRange(target, pawn) : 0f;
            List<IntVec3> candidates = new List<IntVec3>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Position, plan.Facts.Range, true))
            {
                if (!cell.InBounds(map)) continue;
                if (cell.DistanceTo(target.Position) < minRange) continue;
                if (!cell.Standable(map)) continue;
                candidates.Add(cell);
            }
            // Nearest to the pawn first: the shortest walk that still has the
            // shot, which is the cell a player picks by hand.
            IntVec3 from = pawn.Position;
            candidates.Sort((a, b) => from.DistanceTo(a).CompareTo(from.DistanceTo(b)));
            int tried = 0;
            foreach (IntVec3 cell in candidates)
            {
                if (!ranged.CanHitTargetFrom(cell, target)) continue;
                plan.Facts.CellsWithShot++;
                // A path search per candidate is the expensive part, so only the
                // nearest ones are tried; the refusal names that limit. The
                // reach test takes Danger.Deadly because a walk through fire is
                // refused by the hazard check afterwards, with its own wording.
                if (tried >= ShootRules.ReachChecks) continue;
                tried++;
                if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly)) continue;
                plan.Facts.ReachableCells++;
                plan.Facts.Found = true;
                plan.Facts.Cell = $"{cell.x},{cell.z}";
                plan.Facts.Distance = cell.DistanceTo(target.Position);
                plan.Cell = cell;
                return plan;
            }
            return plan;
        }
    }
}
