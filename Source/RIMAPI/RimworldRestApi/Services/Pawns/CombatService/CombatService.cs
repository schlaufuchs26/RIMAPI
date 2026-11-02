using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RIMAPI.Services
{
    // Combat is the one thing the tool could not do (ticket #3154): colonists
    // never fight by themselves, and every raid ended with people standing in
    // the open. Every order here is the game's own: drafting is
    // Pawn_DraftController.Drafted, a move is JobDefOf.Goto, an attack is the
    // job the game's own attack gizmo queues, and holding is an idle combat
    // wait. Nothing spawns, teleports or damages anything.
    public partial class CombatService : ICombatService
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
                Map map = MapLookup.Resolve(mapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult<CombatStateDto>.Fail(mapError);

                CombatStateDto state = new CombatStateDto
                {
                    MapId = mapId,
                    Hostiles = new List<CombatantDto>(),
                    Colonists = new List<CombatantDto>(),
                };
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned) continue;
                    // IsColonist rather than IsColonistPlayerControlled: a
                    // colonist in a mental state belongs to this list too, and
                    // the game's player-controlled flag is false for him
                    // (ticket #3433).
                    bool colonist = pawn.IsColonist;
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
                    // The game's own IsColonistPlayerControlled also fails for
                    // a colonist in a mental state or off the map, so each
                    // condition is named instead (ticket #3433).
                    string refusal = CombatRefusal.DraftNote(PawnHelper.RefusalStateOf(pawn));
                    if (refusal != null)
                    {
                        dto.Note = refusal;
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
                string refusal = CombatRefusal.OrderRefusal(pawn.LabelShort, PawnHelper.RefusalStateOf(pawn));
                if (refusal != null) return ApiResult<OrderResultDto>.Fail(refusal);
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
                        // A pawn the game disables violence for never fires
                        // (fox, #3360): the order queued AttackStatic and the
                        // raid walked up unopposed, so it is refused here with
                        // the reason instead of queued as if it could fight.
                        if (!PawnHelper.CanFight(pawn))
                        {
                            return ApiResult<OrderResultDto>.Fail(
                                $"{pawn.LabelShort} is incapable of violence (the game disables the violent work tags for him), "
                                + "so no attack order can make him fight; move and hold still work");
                        }
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
            Danger maxDanger;
            string dangerError;
            if (!TryParseDanger(request.MaxDanger, out maxDanger, out dangerError))
            {
                return ApiResult<OrderResultDto>.Fail(dangerError);
            }
            string destText = $"{request.X},{request.Z}";
            string refusal = ReachRefusal(pawn, cell, PathEndMode.OnCell, maxDanger, destText);
            if (refusal != null) return ApiResult<OrderResultDto>.Fail(refusal);
            // The game's danger scale is temperature, so a path through fire
            // or toxic waste still reads none there (ticket #3209): the walk is
            // scanned for those, and the order is refused when it crosses more
            // than the caller tolerates.
            WalkReport walk = InspectWalk(pawn, cell);
            if (walk.Worst > maxDanger)
            {
                return ApiResult<OrderResultDto>.Fail(
                    $"{pawn.LabelShort} cannot reach {destText} from {pawn.Position.x},{pawn.Position.z} at " +
                    $"max_danger={DangerText(maxDanger)}: the walk the game would take runs through " +
                    $"{walk.HazardText()}, so the pawn would take damage on the way; " +
                    "pass max_danger=deadly to order it anyway");
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
            result.Reach = walk.ReachText();
            result.PathDanger = DangerText(walk.Worst);
            result.PathHazards = walk.HazardText();
            if (walk.Worst != Danger.None)
            {
                result.Note = $"the path crosses {walk.HazardText()}, tolerated at max_danger={DangerText(maxDanger)}";
            }
            return ApiResult<OrderResultDto>.Ok(Fill(pawn, result));
        }


        // WalkReport is what a walk to a destination costs (ticket #3209): the
        // path length in cells and the hazards along it, so an order's answer
        // says "fire (3 cells)" instead of a bare "reachable".
        private sealed class WalkReport
        {
            public int Cells;
            public Danger Worst = Danger.None;
            private readonly Dictionary<string, int> counts = new Dictionary<string, int>();

            public void Add(string hazard, Danger danger)
            {
                int n;
                counts.TryGetValue(hazard, out n);
                counts[hazard] = n + 1;
                if (danger > Worst) Worst = danger;
            }

            // ReachText is the distance the check measured, in cells, or empty
            // when no path was found.
            public string ReachText()
            {
                return Cells > 0 ? $"{Cells} cells of path" : "";
            }

            // HazardText names what the walk crosses with the cells it does,
            // e.g. "fire (3 cells), toxic waste (8 cells)".
            public string HazardText()
            {
                if (counts.Count == 0) return "";
                List<string> parts = new List<string>();
                foreach (KeyValuePair<string, int> pair in counts)
                {
                    parts.Add($"{pair.Key} ({pair.Value} {(pair.Value == 1 ? "cell" : "cells")})");
                }
                return string.Join(", ", parts);
            }
        }

        // InspectWalk finds the walk the game's own pathfinder would take and
        // scans its cells. The game's danger scale is the temperature of the
        // rooms the walk crosses (Region.DangerFor), so fire, toxic waste and
        // the gases read "none" there; they are named here at the level each
        // costs (GasHazard.All): fire is deadly, polluted ground, tox gas and
        // rot stink some, blind smoke and deadlife dust none.
        private static WalkReport InspectWalk(Pawn pawn, LocalTargetInfo dest, PathEndMode endMode = PathEndMode.OnCell)
        {
            WalkReport report = new WalkReport();
            PawnPath path = pawn.Map.pathFinder.FindPathNow(pawn.Position, dest, pawn, null, endMode);
            if (path == null) return report;
            try
            {
                if (!path.Found) return report;
                report.Cells = path.NodesLeftCount;
                List<IntVec3> nodes = path.NodesReversed;
                // Which gases matter is a property of the pawn, not of the
                // cell, so it is asked once for the whole walk.
                bool lungRot = CanCatchLungRot(pawn);
                bool anomaly = ModsConfig.AnomalyActive;
                for (int i = 0; i < nodes.Count; i++)
                {
                    IntVec3 cell = nodes[i];
                    // The pawn's own cell is where he already stands: a move
                    // out of a burning room must not be refused for the fire
                    // he is standing in.
                    if (cell == pawn.Position) continue;
                    if (cell.ContainsStaticFire(pawn.Map))
                    {
                        report.Add("fire", Danger.Deadly);
                    }
                    if (pawn.Map.pollutionGrid.IsPolluted(cell))
                    {
                        report.Add("toxic waste", Danger.Some);
                    }
                    // Gas is not part of the pollution grid: the game keeps a
                    // separate GasGrid with one density byte (0-255) per gas
                    // and cell, and PawnGasEffectsTickInterval reacts to any
                    // density above zero, so every gassed cell counts, not
                    // only a thick cloud. All four gases share that grid and
                    // each costs something else (ticket #3248, #3283).
                    for (int g = 0; g < GasHazard.All.Length; g++)
                    {
                        GasHazard gas = GasHazard.All[g];
                        if (!gas.AppliesTo(lungRot, anomaly)) continue;
                        if (GasUtility.GasDensity(cell, pawn.Map, gas.GasType) > 0)
                        {
                            report.Add(gas.Label, gas.Danger);
                        }
                    }
                    Danger heat = cell.GetDangerFor(pawn, pawn.Map);
                    if (heat == Danger.Deadly)
                    {
                        report.Add("deadly heat or cold", Danger.Deadly);
                    }
                    else if (heat == Danger.Some)
                    {
                        report.Add("heat or cold outside his comfort", Danger.Some);
                    }
                }
            }
            finally
            {
                path.ReleaseToPool();
            }
            return report;
        }

        // CanCatchLungRot answers whether rot stink can harm this pawn: the game
        // adds lung rot exposure only to animals and humanlikes that are not
        // immune and still have a lung without a rot-protecting hediff on it
        // (GasUtility.PawnGasEffectsTickInterval), so a mechanoid walking
        // through a corpse pile is not warned about it.
        private static bool CanCatchLungRot(Pawn pawn)
        {
            if (!pawn.RaceProps.Animal && !pawn.RaceProps.Humanlike) return false;
            if (pawn.RaceProps.isImmuneToInfections) return false;
            return GasUtility.GetLungRotAffectedBodyParts(pawn).Any();
        }

        // ReachRefusal answers why the pawn cannot walk there at the tolerated
        // danger, or null when it can. The pawn goes into the TraverseParms
        // (ticket #3209): the map-level overload with a bare Danger builds
        // TraverseParms without a pawn, and Region.Allows skips its danger
        // check when the pawn is null, so the Danger.Some the check passed
        // never applied to anything. With the pawn in the parms the game's own
        // temperature check runs, and a closed door is passable again: with a
        // null pawn Region.Allows reads every door as a wall (ticket #3236), so
        // the parms come from WalkParms.For. The second question tells a wall
        // apart from a danger the caller did not tolerate: with Danger.Deadly the
        // danger check is off, so a path that exists then was refused by the heat.
        private static string ReachRefusal(Pawn pawn, LocalTargetInfo dest, PathEndMode endMode, Danger maxDanger, string destText)
        {
            Map map = pawn.Map;
            if (map.reachability.CanReach(pawn.Position, dest, endMode,
                    WalkParms.For(pawn, maxDanger)))
            {
                return null;
            }
            string from = $"{pawn.Position.x},{pawn.Position.z}";
            if (maxDanger < Danger.Deadly &&
                map.reachability.CanReach(pawn.Position, dest, endMode,
                    WalkParms.For(pawn, Danger.Deadly)))
            {
                return $"{pawn.LabelShort} cannot reach {destText} from {from} at " +
                    $"max_danger={DangerText(maxDanger)}: a path exists, but it crosses a room that is " +
                    "deadly hot or cold for him; pass max_danger=deadly to order it anyway";
            }
            return $"{pawn.LabelShort} cannot reach {destText} from {from}: no walkable path there " +
                "(a sealed room, a wall, water or a closed door the pawn cannot open in the way), " +
                "so the game would drop the job";
        }

        // TryParseDanger reads max_danger; empty keeps the old level, some.
        private static bool TryParseDanger(string text, out Danger danger, out string error)
        {
            danger = Danger.Some;
            error = null;
            if (string.IsNullOrEmpty(text)) return true;
            switch (text.Trim().ToLowerInvariant())
            {
                case "none":
                    danger = Danger.None;
                    return true;
                case "some":
                    danger = Danger.Some;
                    return true;
                case "deadly":
                    danger = Danger.Deadly;
                    return true;
                default:
                    error = $"max_danger \"{text}\" is not one of none, some or deadly";
                    return false;
            }
        }

        private static string DangerText(Danger danger)
        {
            switch (danger)
            {
                case Danger.Deadly: return "deadly";
                case Danger.Some: return "some";
                default: return "none";
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
            AttackDamage.Hit damage = DamageOf(pawn);
            float meleeDamage = MeleeDamage(pawn);
            HitPointsOf(pawn, out int hitPoints, out int maxHitPoints);
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
                DamagePerHit = damage.Damage,
                DamageKind = damage.Kind,
                MeleeDamage = meleeDamage,
                HitPoints = hitPoints,
                MaxHitPoints = maxHitPoints,
                MoveSpeed = MoveSpeed(pawn),
                Drafted = pawn.Drafted,
                FireAtWill = pawn.drafter != null && pawn.drafter.FireAtWill,
                Activity = pawn.CurJobDef != null ? pawn.CurJobDef.defName : "",
                Moving = pawn.pather != null && pawn.pather.Moving,
                Fighting = IsFightJob(pawn.CurJobDef),
                LordJob = LordJobOf(pawn),
                AttackBlocked = AttackBlockedOf(pawn),
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

        // LordJobOf names the group the pawn belongs to, as the game's own
        // class name (LordJob_AssaultColony for a raid, LordJob_DefendAndExpandHive
        // for a hive's insects), and is empty for a pawn without a lord. The
        // tool reads it to tell a raid from a hostile that stays where it is:
        // a hive's insects walk about their cave all day, and reading that walk
        // as a fight cut every wait to one step (ticket #3294).
        private static string LordJobOf(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            return lord != null && lord.LordJob != null ? lord.LordJob.GetType().Name : "";
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

        // DamageOf is the pawn's primary attack (ticket #3451): the shot of a
        // ranged weapon, or a blow. The cut between the two lives in
        // AttackDamage so the test project can state it without a game.
        private static AttackDamage.Hit DamageOf(Pawn pawn)
        {
            Verb verb = AttackVerb(pawn);
            VerbProperties props = verb != null ? verb.verbProps : null;
            if (props == null) return new AttackDamage.Hit();
            float projectile = props.defaultProjectile != null && props.defaultProjectile.projectile != null
                ? props.defaultProjectile.projectile.GetDamageAmount(
                    pawn.equipment != null ? pawn.equipment.Primary : null, null)
                : 0f;
            return AttackDamage.Of(props.range, MeleeDamage(pawn), projectile);
        }

        // AttackVerb is the equipped weapon's verb, or the pawn's own first
        // verb: an animal's bite, an unarmed colonist's fists. Reading only
        // the equipment made every animal look like it attacked for nothing.
        private static Verb AttackVerb(Pawn pawn)
        {
            if (pawn.equipment != null && pawn.equipment.PrimaryEq != null)
            {
                return pawn.equipment.PrimaryEq.PrimaryVerb;
            }
            return pawn.VerbTracker != null ? pawn.VerbTracker.PrimaryVerb : null;
        }

        // MeleeDamage is what the pawn does in contact: the best attack of the
        // weapon it holds (a rifle's stock) or of its own body (a warg's
        // teeth). A hostile that closes in is a melee fight whatever it
        // carries, so the read needs this number next to the shot's.
        private static float MeleeDamage(Pawn pawn)
        {
            ThingDef def = pawn.equipment != null && pawn.equipment.Primary != null
                ? pawn.equipment.Primary.def
                : pawn.def;
            if (def == null || def.tools == null) return 0f;
            float best = 0f;
            foreach (Tool tool in def.tools)
            {
                if (tool != null && tool.power > best) best = tool.power;
            }
            return best;
        }

        // HitPointsOf is the body's hit points, the unit DamagePerHit is
        // measured in: the sum over the parts the pawn still has, so a missing
        // leg takes its points with it. Both sides are scaled by the pawn's own
        // health scale (BodyPartDef.GetMaxHealth), the same number the game
        // uses for a part: the raw def value made a full-health raccoon read
        // "196/489 hp", i.e. 40%, and a run read that as a wounded animal it
        // was finally hurting (fox #3737). A pawn without a health tracker
        // reads zero, and the tool then prints no hit-point figure.
        private static void HitPointsOf(Pawn pawn, out int current, out int max)
        {
            current = 0;
            max = 0;
            if (pawn.health == null || pawn.health.hediffSet == null) return;
            foreach (BodyPartRecord part in pawn.health.hediffSet.GetNotMissingParts())
            {
                if (part == null || part.def == null) continue;
                max += (int)part.def.GetMaxHealth(pawn);
                current += (int)pawn.health.hediffSet.GetPartHealth(part);
            }
            if (current < 0) current = 0;
        }

        // MoveSpeed is the pawn's movement in cells per second, the number that
        // decides whether walking away breaks contact with a melee attacker.
        private static float MoveSpeed(Pawn pawn)
        {
            return pawn.GetStatValue(StatDefOf.MoveSpeed);
        }

        // IsHostile is the side test the tool uses: an enemy faction, or an
        // animal the game has turned manhunter. The rule lives in Hostility so
        // the cheap headcount counts the same pawns (ticket #3451).
        private static bool IsHostile(Pawn pawn)
        {
            return Hostility.IsHostileToColony(pawn);
        }

        // KindOf names the side a pawn is on for a hostile line; the rule is
        // HostilityRules.KindOf, shared with the creature summary (ticket #3690).
        private static string KindOf(Pawn pawn)
        {
            return Hostility.KindOf(pawn);
        }

    }
}
