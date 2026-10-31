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
    // Combat is the one thing the tool could not do (ticket #3154): colonists
    // never fight by themselves, and every raid ended with people standing in
    // the open. Every order here is the game's own: drafting is
    // Pawn_DraftController.Drafted, a move is JobDefOf.Goto, an attack is the
    // job the game's own attack gizmo queues, and holding is an idle combat
    // wait. Nothing spawns, teleports or damages anything.
    public class CombatService : ICombatService
    {
        // The two attack jobs the game's attack gizmo picks between: a ranged
        // pawn shoots (AttackStatic), a melee pawn closes in (AttackMelee).
        private const string AttackStaticDef = "AttackStatic";
        private const string AttackMeleeDef = "AttackMelee";
        private const string WaitCombatDef = "Wait_Combat";

        public ApiResult<CombatStateDto> GetCombat(int mapId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult<CombatStateDto>.Fail($"Map {mapId} not found.");

                CombatStateDto state = new CombatStateDto
                {
                    MapId = mapId,
                    Hostiles = new List<CombatantDto>(),
                    Colonists = new List<CombatantDto>(),
                };
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned) continue;
                    bool colonist = pawn.IsColonistPlayerControlled;
                    bool hostile = !colonist && IsHostile(pawn);
                    if (!colonist && !hostile) continue;
                    CombatantDto dto = CombatantOf(pawn, colonist ? "colonist" : KindOf(pawn), hostile);
                    if (colonist)
                    {
                        state.Colonists.Add(dto);
                        if (dto.Drafted) state.DraftedCount++;
                    }
                    else
                    {
                        state.Hostiles.Add(dto);
                    }
                }
                state.Hostiles.Sort((a, b) => a.Id.CompareTo(b.Id));
                state.Colonists.Sort((a, b) => a.Id.CompareTo(b.Id));
                state.HostilesCount = state.Hostiles.Count;
                state.FightRunning = state.HostilesCount > 0;
                return ApiResult<CombatStateDto>.Ok(state);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error reading combat on map {mapId}: {ex}");
                return ApiResult<CombatStateDto>.Fail(ex.Message);
            }
        }

        // SetDrafted drafts or undrafts every named colonist and reports each
        // pawn's state after the change, so a caller sees a refusal per pawn
        // instead of one failed list.
        public ApiResult<List<DraftResultDto>> SetDrafted(DraftRequestDto request)
        {
            try
            {
                if (request == null || request.PawnIds == null || request.PawnIds.Count == 0)
                {
                    return ApiResult<List<DraftResultDto>>.Fail(
                        "no pawn ids given: the `colonists` read reports them");
                }
                List<DraftResultDto> results = new List<DraftResultDto>();
                foreach (int id in request.PawnIds)
                {
                    DraftResultDto dto = new DraftResultDto { PawnId = id, Drafted = false };
                    Pawn pawn = PawnHelper.FindPawnById(id);
                    if (pawn == null)
                    {
                        dto.Note = $"no pawn with id {id}";
                        results.Add(dto);
                        continue;
                    }
                    dto.Name = pawn.LabelShort;
                    if (!pawn.IsColonistPlayerControlled)
                    {
                        dto.Note = "not one of the colony's colonists";
                        results.Add(dto);
                        continue;
                    }
                    if (pawn.Downed || pawn.Dead)
                    {
                        dto.Note = "downed or dead, so he cannot be drafted";
                        results.Add(dto);
                        continue;
                    }
                    if (pawn.drafter == null)
                    {
                        dto.Note = "this pawn has no draft controller";
                        results.Add(dto);
                        continue;
                    }
                    pawn.drafter.Drafted = request.Drafted;
                    dto.Drafted = pawn.Drafted;
                    if (dto.Drafted != request.Drafted)
                    {
                        dto.Note = "the game did not take the change";
                    }
                    results.Add(dto);
                }
                return ApiResult<List<DraftResultDto>>.Ok(results);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error drafting: {ex}");
                return ApiResult<List<DraftResultDto>>.Fail(ex.Message);
            }
        }

        // Order gives one drafted colonist one order: move to a cell, attack a
        // named pawn, hold position, or switch fire-at-will. It answers with the
        // pawn's position and current job after the order, which is the
        // read-back the caller checks: an order that did not take shows a job
        // that is not the one asked for.
        public ApiResult<OrderResultDto> Order(OrderRequestDto request)
        {
            try
            {
                if (request == null) return ApiResult<OrderResultDto>.Fail("no order body");
                Pawn pawn = PawnHelper.FindPawnById(request.PawnId);
                if (pawn == null) return ApiResult<OrderResultDto>.Fail($"no pawn with id {request.PawnId}");
                if (!pawn.IsColonistPlayerControlled)
                {
                    return ApiResult<OrderResultDto>.Fail($"{pawn.LabelShort} is not one of the colony's colonists");
                }
                if (pawn.Downed || pawn.Dead)
                {
                    return ApiResult<OrderResultDto>.Fail($"{pawn.LabelShort} is downed or dead, so he cannot act");
                }
                if (pawn.drafter == null)
                {
                    return ApiResult<OrderResultDto>.Fail($"{pawn.LabelShort} has no draft controller");
                }
                if (!pawn.Drafted)
                {
                    return ApiResult<OrderResultDto>.Fail(
                        $"{pawn.LabelShort} is not drafted: an order is a draft order, so `draft` him first");
                }
                if (pawn.jobs == null)
                {
                    return ApiResult<OrderResultDto>.Fail($"{pawn.LabelShort} has no job tracker");
                }

                OrderResultDto result = new OrderResultDto
                {
                    PawnId = pawn.thingIDNumber,
                    Name = pawn.LabelShort,
                    Order = request.Order,
                    Drafted = pawn.Drafted,
                };

                switch (request.Order)
                {
                    case "move":
                        return Move(pawn, request, result);
                    case "attack":
                        return Attack(pawn, request, result);
                    case "hold":
                        return Hold(pawn, result);
                    case "fire_at_will":
                        pawn.drafter.FireAtWill = request.FireAtWill;
                        result.FireAtWill = pawn.drafter.FireAtWill;
                        if (result.FireAtWill != request.FireAtWill)
                        {
                            result.Note = "the game did not take the change";
                        }
                        return ApiResult<OrderResultDto>.Ok(Fill(pawn, result));
                    default:
                        return ApiResult<OrderResultDto>.Fail(
                            $"unknown order {request.Order}: use move, attack, hold or fire_at_will");
                }
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error ordering pawn {request?.PawnId}: {ex}");
                return ApiResult<OrderResultDto>.Fail(ex.Message);
            }
        }

        private ApiResult<OrderResultDto> Move(Pawn pawn, OrderRequestDto request, OrderResultDto result)
        {
            Map map = pawn.Map;
            IntVec3 cell = new IntVec3(request.X, 0, request.Z);
            if (!cell.InBounds(map))
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"cell {request.X},{request.Z} is outside the map ({map.Size.x}x{map.Size.z})");
            }
            if (!cell.Standable(map))
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"cell {request.X},{request.Z} is not standable (a wall, water, a pawn or a building)");
            }
            // A standable cell is not necessarily a cell the pawn can walk to
            // (ticket #3175): a sealed room, a walled-off valley or a target
            // across water takes the job and then drops it, and the caller
            // only sees a pawn that never moved. So the check comes before the
            // job, and its answer carries the distance.
            if (!map.reachability.CanReach(pawn.Position, cell, PathEndMode.OnCell, TraverseMode.ByPawn, Danger.Some))
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"{pawn.LabelShort} cannot reach {request.X},{request.Z} from {pawn.Position.x},{pawn.Position.z}: " +
                    "no walkable path there (a sealed room, a wall or water in the way), so the game would drop the job");
            }
            Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
            if (!pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                return ApiResult<OrderResultDto>.Fail($"{pawn.LabelShort} refused the move order");
            }
            result.X = request.X;
            result.Z = request.Z;
            result.Job = job.def.defName;
            result.Reachable = true;
            result.Reach = PathText(pawn, cell);
            return ApiResult<OrderResultDto>.Ok(Fill(pawn, result));
        }

        private ApiResult<OrderResultDto> Attack(Pawn pawn, OrderRequestDto request, OrderResultDto result)
        {
            Pawn target = PawnHelper.FindPawnById(request.TargetId);
            if (target == null)
            {
                return ApiResult<OrderResultDto>.Fail($"no pawn with id {request.TargetId} to attack");
            }
            if (target.Dead || !target.Spawned)
            {
                return ApiResult<OrderResultDto>.Fail($"{target.LabelShort} is dead or gone");
            }
            if (target.IsColonistPlayerControlled)
            {
                return ApiResult<OrderResultDto>.Fail($"{target.LabelShort} is a colonist; the tool does not order friendly fire");
            }
            if (!target.HostileTo(Faction.OfPlayer) && !IsManhunter(target))
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"{target.LabelShort} is not hostile, so the colony has no fight with him");
            }
            // The same reachability check as a move (ticket #3175): a raider
            // behind a sealed wall or across water takes the attack job and
            // then drops it, so the pawn stands still and the caller learns
            // nothing. A ranged pawn needs a path to its firing position too.
            if (!pawn.Map.reachability.CanReach(pawn.Position, target, PathEndMode.Touch, TraverseMode.ByPawn, Danger.Some))
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"{pawn.LabelShort} cannot reach {target.LabelShort} (id {target.thingIDNumber}) from " +
                    $"{pawn.Position.x},{pawn.Position.z}: no walkable path there, so the game would drop the attack job");
            }
            JobDef def = HasRangedWeapon(pawn)
                ? DefDatabase<JobDef>.GetNamedSilentFail(AttackStaticDef)
                : DefDatabase<JobDef>.GetNamedSilentFail(AttackMeleeDef);
            if (def == null)
            {
                return ApiResult<OrderResultDto>.Fail("the game has no attack job for this pawn");
            }
            Job job = JobMaker.MakeJob(def, target);
            if (!pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                return ApiResult<OrderResultDto>.Fail($"{pawn.LabelShort} refused the attack order");
            }
            result.TargetId = target.thingIDNumber;
            result.Target = target.LabelShort;
            result.Job = job.def.defName;
            result.Reachable = true;
            result.Reach = PathText(pawn, target);
            return ApiResult<OrderResultDto>.Ok(Fill(pawn, result));
        }

        // PathText measures the walk the game's own pathfinder would take, in
        // cells, so an order's answer carries the distance it checked instead
        // of a bare "reachable". Empty when no path is found, which the
        // reachability check has already refused.
        private static string PathText(Pawn pawn, LocalTargetInfo dest)
        {
            PawnPath path = pawn.Map.pathFinder.FindPathNow(pawn.Position, dest, pawn);
            if (path == null) return "";
            try
            {
                return path.Found ? $"{path.TotalCost:0} cells of path" : "";
            }
            finally
            {
                path.ReleaseToPool();
            }
        }

        // Hold stops the pawn where it stands: the game's idle combat wait
        // keeps a drafted pawn in place and firing at will, instead of walking
        // on to the last order's cell.
        private ApiResult<OrderResultDto> Hold(Pawn pawn, OrderResultDto result)
        {
            pawn.jobs.StopAll();
            JobDef wait = DefDatabase<JobDef>.GetNamedSilentFail(WaitCombatDef);
            if (wait != null)
            {
                Job job = JobMaker.MakeJob(wait);
                // A long expiry: the hold lasts until the next order, not
                // until a timer runs out mid-fight.
                job.expiryInterval = 600;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                result.Job = job.def.defName;
            }
            return ApiResult<OrderResultDto>.Ok(Fill(pawn, result));
        }

        // Fill adds the read-back every order answer carries: where the pawn
        // stands and what it is doing now.
        private static OrderResultDto Fill(Pawn pawn, OrderResultDto result)
        {
            result.Position = PositionOf(pawn);
            result.Activity = pawn.CurJobDef != null ? pawn.CurJobDef.defName : "";
            result.Drafted = pawn.Drafted;
            result.FireAtWill = pawn.drafter != null && pawn.drafter.FireAtWill;
            return result;
        }

        private static CombatantDto CombatantOf(Pawn pawn, string kind, bool hostile)
        {
            CombatantDto dto = new CombatantDto
            {
                Id = pawn.thingIDNumber,
                Name = pawn.LabelShort,
                Kind = kind,
                Faction = pawn.Faction != null ? pawn.Faction.Name : "",
                Hostile = hostile,
                Downed = pawn.Downed,
                Dead = pawn.Dead,
                Health = pawn.health != null && pawn.health.summaryHealth != null
                    ? pawn.health.summaryHealth.SummaryHealthPercent
                    : 1f,
                Position = PositionOf(pawn),
                Weapon = WeaponLabel(pawn),
                WeaponRange = WeaponRange(pawn),
                Drafted = pawn.Drafted,
                FireAtWill = pawn.drafter != null && pawn.drafter.FireAtWill,
                Activity = pawn.CurJobDef != null ? pawn.CurJobDef.defName : "",
                Moving = pawn.pather != null && pawn.pather.Moving,
                Fighting = IsFightJob(pawn.CurJobDef),
            };
            if (pawn.CurJob != null && pawn.CurJob.targetA.HasThing)
            {
                Thing thing = pawn.CurJob.targetA.Thing;
                dto.TargetId = thing.thingIDNumber;
                dto.Target = thing.LabelShort;
            }
            return dto;
        }

        private static bool IsFightJob(JobDef def)
        {
            if (def == null) return false;
            string name = def.defName;
            return name == AttackStaticDef || name == AttackMeleeDef
                || name.IndexOf("Attack", StringComparison.Ordinal) >= 0;
        }

        private static PositionDto PositionOf(Pawn pawn)
        {
            IntVec3 p = pawn.Position;
            return new PositionDto { X = p.x, Y = p.y, Z = p.z };
        }

        private static string WeaponLabel(Pawn pawn)
        {
            ThingWithComps weapon = pawn.equipment != null ? pawn.equipment.Primary : null;
            return weapon != null ? weapon.LabelShort : "";
        }

        private static float WeaponRange(Pawn pawn)
        {
            Verb verb = pawn.equipment != null && pawn.equipment.PrimaryEq != null
                ? pawn.equipment.PrimaryEq.PrimaryVerb
                : null;
            return verb != null && verb.verbProps != null ? verb.verbProps.range : 0f;
        }

        private static bool HasRangedWeapon(Pawn pawn)
        {
            return WeaponRange(pawn) > 1.6f;
        }

        // IsHostile is the side test the tool uses: an enemy faction, or an
        // animal the game has turned manhunter.
        private static bool IsHostile(Pawn pawn)
        {
            if (IsManhunter(pawn)) return true;
            return pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer);
        }

        private static bool IsManhunter(Pawn pawn)
        {
            if (!pawn.RaceProps.Animal) return false;
            MentalStateDef state = pawn.MentalStateDef;
            return state == MentalStateDefOf.Manhunter || state == MentalStateDefOf.ManhunterPermanent;
        }

        // KindOf names the side a pawn is on for a hostile line.
        private static string KindOf(Pawn pawn)
        {
            if (IsManhunter(pawn)) return "manhunter";
            if (pawn.IsPrisonerOfColony) return "prisoner";
            if (pawn.IsSlaveOfColony) return "slave";
            if (pawn.HostFaction == Faction.OfPlayer) return "guest";
            if (pawn.Faction == null) return "factionless";
            if (pawn.Faction.HostileTo(Faction.OfPlayer)) return "hostile";
            return "neutral";
        }

    }
}
