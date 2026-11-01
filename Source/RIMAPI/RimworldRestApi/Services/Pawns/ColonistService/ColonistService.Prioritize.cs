using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    public partial class ColonistService
    {
        // MakeJobPrioritize orders one job on one named colonist now, the way
        // the player's right-click "Prioritize X" entry does (fox ticket
        // #3626). Every other order in the tool queues work for whoever the
        // colony's AI picks; this one names the pawn, which is the only lever
        // for an idle colonist next to a standing designation and for a wild
        // man no handler ever walks to.
        //
        // The job is not built from a table here: the pawn's own work givers
        // are asked, the same list the float menu is drawn from, so the game's
        // checks decide and its refusal is what a run reads back. A cell target
        // goes through the things standing on it first, its blueprints and
        // frames ahead of everything else (ticket #3708): the construct givers
        // answer only for a thing, so a cell-only scan reached the roof giver
        // and left a wall frame standing.
        public ApiResult MakeJobPrioritize(
            int mapId,
            int pawnId,
            string jobName,
            int thingId,
            int x,
            int z,
            string workName)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult.Fail($"Map with ID={mapId} not found");
                Pawn pawn = PawnHelper.FindPawnForOrder(map, pawnId);
                if (pawn == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                // A carried colonist takes no order, and the old lookup answered
                // "not found" for him (fox #3719).
                string carried = CombatRefusal.CarriedRefusal(
                    Describe(pawn), pawn.CarriedBy?.LabelShort, "order");
                if (carried != null) return ApiResult.Fail(carried);

                PriorityJobResultDto result = new PriorityJobResultDto
                {
                    PawnId = pawn.thingIDNumber,
                    Pawn = pawn.LabelShort,
                    X = pawn.Position.x,
                    Z = pawn.Position.z,
                    Offered = new List<string>(),
                };

                if (pawn.Dead || pawn.Downed)
                {
                    result.Refusal = $"{Describe(pawn)} is downed, so he takes no order";
                    return ApiResult.Ok(result);
                }
                if (!pawn.Spawned)
                {
                    result.Refusal = $"{Describe(pawn)} is not on the map";
                    return ApiResult.Ok(result);
                }

                Thing thing = null;
                IntVec3 cell = IntVec3.Invalid;
                if (thingId > 0)
                {
                    thing = FindThingOnMap(map, thingId);
                    if (thing == null)
                    {
                        return ApiResult.Fail($"Thing with ID={thingId} not found on map {mapId}");
                    }
                }
                else
                {
                    cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(map))
                    {
                        return ApiResult.Fail(
                            $"Cell {x},{z} is outside map {mapId} (it is {map.Size.x}x{map.Size.z})");
                    }
                }

                WorkTypeDef workType = null;
                if (!string.IsNullOrEmpty(workName))
                {
                    workType = FindWorkType(workName);
                    if (workType == null)
                    {
                        return ApiResult.Fail($"No work type is called \"{workName}\"");
                    }
                    result.WorkType = workType.labelShort;
                }

                string target = thing != null
                    ? $"thing {thing.thingIDNumber} ({thing.LabelShort})"
                    : $"cell {x},{z}";

                List<string> offered = new List<string>();
                Job chosen = null;
                WorkGiver chosenGiver = null;
                string removalRefusal = null;
                foreach (JobTarget candidate in JobTargets(map, thing, cell))
                {
                    // The colony's own blueprint or frame is there to be built.
                    // A deconstruct on it takes the plan away, and no other job
                    // is what a prioritize call on a planned build means, so a
                    // named removal is refused instead of queued (fox #3706).
                    if (IsPlannedBuild(candidate.Thing) && PriorityTargetRules.NamesRemoval(jobName))
                    {
                        removalRefusal = removalRefusal ?? RemovalRefusal(candidate.Thing);
                        continue;
                    }
                    foreach (WorkGiver giver in OrderableGivers(pawn, workType))
                    {
                        Job job = OrderedJob(giver, pawn, candidate.Thing, candidate.Cell);
                        if (job == null || job.def == null) continue;
                        offered.Add(job.def.defName);
                        if (!string.IsNullOrEmpty(jobName) && !PriorityJobRules.Matches(jobName, job.def.defName))
                        {
                            continue;
                        }
                        // A blueprint or frame is what the build jobs are for;
                        // a caller who named no job there gets that build, not
                        // the roof above the cell and not the deconstruct of
                        // the player's own plan (tickets #3708, #3706).
                        if (IsPlannedBuild(candidate.Thing)
                            && string.IsNullOrEmpty(jobName)
                            && !PriorityTargetRules.IsConstructJob(job.def.defName))
                        {
                            continue;
                        }
                        chosen = job;
                        chosenGiver = giver;
                        break;
                    }
                    if (chosen != null) break;
                }

                if (chosen == null)
                {
                    result.Offered = PriorityJobRules.OfferList(offered);
                    result.Refusal = removalRefusal ?? NoOfferReason(pawn, jobName, workType, target);
                    return ApiResult.Ok(result);
                }

                if (!pawn.jobs.TryTakeOrderedJob(chosen, JobTag.Misc, true))
                {
                    result.Refusal =
                        $"{Describe(pawn)} would not take the {chosen.def.defName} job now: {StateReason(pawn)}";
                    return ApiResult.Ok(result);
                }

                result.Job = chosen.def.defName;
                result.WorkType = result.WorkType
                    ?? chosenGiver.def.workType?.labelShort
                    ?? string.Empty;
                result.Queued = true;
                result.X = pawn.Position.x;
                result.Z = pawn.Position.z;
                return ApiResult.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        // OrderableGivers is the list the game's own right-click menu is built
        // from: the pawn's work givers in his priority order, the ones flagged
        // directOrderable, and the work type the call asked for.
        private static IEnumerable<WorkGiver> OrderableGivers(Pawn pawn, WorkTypeDef workType)
        {
            IEnumerable<WorkGiver> givers = pawn.workSettings != null && pawn.workSettings.EverWork
                ? pawn.workSettings.WorkGiversInOrderNormal
                : DefDatabase<WorkGiverDef>.AllDefsListForReading.Select(def => def.Worker);
            return givers
                .Where(giver => giver != null && giver.def != null && giver.def.directOrderable)
                .Where(giver => workType == null || giver.def.workType == workType)
                // ShouldSkip with forced=true asks only whether he can do that
                // kind of work at all (capability, awake, no mental state); a
                // work type switched off in the work tab does not stop an
                // explicit order, which is what the menu does too.
                .Where(giver => !giver.ShouldSkip(pawn, true));
        }

        // OrderedJob asks one work giver for the job it would give this pawn on
        // that thing or cell. Both job entry points live on the scanner base
        // class, so a giver that is not a scanner offers nothing here, the same
        // way it offers no float menu entry.
        private static Job OrderedJob(WorkGiver giver, Pawn pawn, Thing thing, IntVec3 cell)
        {
            WorkGiver_Scanner scanner = giver as WorkGiver_Scanner;
            if (scanner == null)
            {
                return null;
            }
            return thing != null
                ? scanner.JobOnThing(pawn, thing, true)
                : scanner.JobOnCell(pawn, cell, true);
        }

        // JobTarget is one thing or one cell the order is tried against. A
        // named thing is a target of its own; a cell is tried through the
        // things standing on it first, because the construct givers answer only
        // for a thing (fox ticket #3708).
        private struct JobTarget
        {
            public Thing Thing;
            public IntVec3 Cell;
        }

        // JobTargets is the order the game's own menu would offer at the target:
        // a named thing alone, or the things on the cell with its blueprints and
        // frames first and the cell itself last, so a wall frame's cell builds
        // the wall instead of the roof above it. Pawns are left out: a cell that
        // holds one is not a target for his own jobs.
        private static List<JobTarget> JobTargets(Map map, Thing thing, IntVec3 cell)
        {
            List<JobTarget> targets = new List<JobTarget>();
            if (thing != null)
            {
                targets.Add(new JobTarget { Thing = thing });
                return targets;
            }
            List<Thing> standing = map.thingGrid.ThingsListAt(cell)
                .Where(t => t != null && !(t is Pawn))
                .ToList();
            foreach (Thing onCell in PriorityTargetRules.ConstructiblesFirst(standing, IsPlannedBuild))
            {
                targets.Add(new JobTarget { Thing = onCell });
            }
            targets.Add(new JobTarget { Cell = cell });
            return targets;
        }

        // IsPlannedBuild reports whether a thing is a build the colony planned:
        // a blueprint that waits for its materials, or the frame a colonist has
        // started. Both are what the construct givers answer for and what a
        // deconstruct would take away (fox #3706).
        private static bool IsPlannedBuild(Thing thing)
        {
            return thing is Blueprint_Build || thing is Frame;
        }

        // RemovalRefusal says why the deconstruct the caller named is not the
        // job on a planned build, and names the calls that do what the caller
        // meant (fox #3706).
        private static string RemovalRefusal(Thing thing)
        {
            string kind = thing is Frame ? "frame" : "blueprint";
            string label = thing.def != null ? thing.def.label : "build";
            return $"thing {thing.thingIDNumber} is the colony's own {label} {kind}: a deconstruct takes the plan "
                + "away instead of building it. Name `job=build` to finish it, or `cancel` to drop the plan.";
        }

        private static WorkTypeDef FindWorkType(string name)
        {
            string wanted = PriorityJobRules.Normalize(name);
            return DefDatabase<WorkTypeDef>.AllDefsListForReading.FirstOrDefault(def =>
                PriorityJobRules.Normalize(def.defName) == wanted
                || PriorityJobRules.Normalize(def.labelShort) == wanted
                || PriorityJobRules.Normalize(def.label) == wanted);
        }

        private static Thing FindThingOnMap(Map map, int thingId)
        {
            return map.listerThings.AllThings.FirstOrDefault(t => t.thingIDNumber == thingId);
        }

        // NoOfferReason says why no giver built a job, in the game's terms.
        private static string NoOfferReason(Pawn pawn, string jobName, WorkTypeDef workType, string target)
        {
            string wanted = string.IsNullOrEmpty(jobName) ? "any job" : $"the {jobName} job";
            string scope = workType != null ? $" under {workType.labelShort}" : string.Empty;
            if (workType != null && pawn.WorkTypeIsDisabled(workType))
            {
                return $"{Describe(pawn)} is incapable of {workType.labelShort}, so the game offers no job there";
            }
            return $"{Describe(pawn)} has no work giver that builds {wanted}{scope} on {target}: "
                + "the work type is switched off, he cannot reach it, or the thing is forbidden to him";
        }

        // StateReason names the pawn state that made the job tracker say no.
        private static string StateReason(Pawn pawn)
        {
            if (pawn.Downed) return "he is downed";
            if (pawn.InMentalState) return "he is in a mental state";
            if (pawn.CurJobDef != null) return $"he is already on the {pawn.CurJobDef.defName} job";
            return "the game's own job tracker refused it";
        }
    }
}
