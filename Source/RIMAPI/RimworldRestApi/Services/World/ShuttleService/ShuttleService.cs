using System;
using System.Collections.Generic;
using System.Linq;
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
    /// The shuttles on a map and the game's own "Enter shuttle" order (fox
    /// ticket #3862). A quest shuttle waits for the pawns its quest names, and
    /// the game's lever is the right-click action on the shuttle
    /// (CompShuttle.CompFloatMenuOptions): start the loading if it has not
    /// started, then queue JobDefOf.EnterTransporter on the pawn. Nothing in
    /// the toolset reached it, so a hospitality quest failed while its lodger
    /// stood healthy on the map; this service is that lever plus the read that
    /// names the shuttle's cell and the people it waits for.
    /// </summary>
    public class ShuttleService : IShuttleService
    {
        public ApiResult<List<ShuttleDto>> Shuttles(int mapId)
        {
            try
            {
                Map map = MapLookup.Resolve(mapId, MapHelper.GetMapByID, out string error);
                if (map == null)
                    return ApiResult<List<ShuttleDto>>.Fail(error);

                List<ShuttleDto> shuttles = ShuttlesOn(map).Select(s => ShuttleDtoOf(s, map)).ToList();
                return ApiResult<List<ShuttleDto>>.Ok(shuttles);
            }
            catch (Exception ex)
            {
                return ApiResult<List<ShuttleDto>>.Fail(ex.ToString());
            }
        }

        public ApiResult<ShuttleEnterResultDto> Enter(int pawnId, int shuttleId)
        {
            try
            {
                Pawn pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null)
                    return ApiResult<ShuttleEnterResultDto>.Fail(
                        $"no pawn with id {pawnId}: the `colonists` or `people` read prints the ids");

                Thing shuttle;
                string shuttleProblem;
                if (!FindShuttle(pawn, shuttleId, out shuttle, out shuttleProblem))
                    return ApiResult<ShuttleEnterResultDto>.Fail(shuttleProblem);

                Map map = shuttle.MapHeld;
                CompShuttle comp = shuttle.TryGetComp<CompShuttle>();
                CompTransporter transporter = shuttle.TryGetComp<CompTransporter>();
                if (comp == null || transporter == null)
                    return ApiResult<ShuttleEnterResultDto>.Fail(
                        $"{shuttle.Label} (id {shuttle.thingIDNumber}) is not a shuttle: it carries no shuttle component");

                bool contained = transporter.innerContainer.Contains(pawn);
                // A pawn who cannot walk is carried in, exactly as the game's
                // own "Carry to shuttle" option does it: a hospitality lodger is
                // often paralyzed, and that is the state the ticket lost the
                // reward pawn in.
                Pawn carrier = pawn.Downed ? CarrierFor(pawn, shuttle) : null;
                ShuttleRules.State state = StateOf(shuttle, comp, transporter, pawn, contained,
                    carrier != null ? carrier.LabelShortCap : "");
                if (!ShuttleRules.NothingToDo(state))
                {
                    string problem = ShuttleRules.Problem(state);
                    if (problem != null)
                        return ApiResult<ShuttleEnterResultDto>.Fail(
                            shuttle.Label.CapitalizeFirst() + " (id " + shuttle.thingIDNumber + "): " + problem);
                }

                bool tookJob = false;
                string orderedJob = "";
                if (!contained)
                {
                    // The float menu's own two steps, in its order.
                    if (!transporter.LoadingInProgressOrReadyToLaunch)
                        TransporterUtility.InitiateLoading(Gen.YieldSingle(transporter));
                    if (carrier != null)
                    {
                        Job carry = JobMaker.MakeJob(JobDefOf.HaulToTransporter, pawn, shuttle);
                        carry.ignoreForbidden = true;
                        carry.count = 1;
                        orderedJob = carry.def.defName;
                        tookJob = carrier.jobs.TryTakeOrderedJob(carry, JobTag.Misc);
                    }
                    else
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.EnterTransporter, shuttle);
                        orderedJob = job.def.defName;
                        tookJob = pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                }

                // Read the game back instead of acknowledging: a job the game
                // dropped, or a pawn that boarded at once, is the answer.
                bool nowContained = transporter.innerContainer.Contains(pawn);
                if (!contained && !tookJob && !nowContained)
                {
                    string job = pawn.jobs != null && pawn.jobs.curJob != null ? pawn.jobs.curJob.def.defName : "";
                    return ApiResult<ShuttleEnterResultDto>.Fail(
                        shuttle.Label.CapitalizeFirst() + " (id " + shuttle.thingIDNumber + "): the game did not take the job for "
                        + pawn.LabelShortCap + (job == "" ? "" : ", who holds " + job + " instead"));
                }

                ShuttleEnterResultDto result = new ShuttleEnterResultDto
                {
                    PawnId = pawn.thingIDNumber,
                    Pawn = pawn.LabelShortCap,
                    ShuttleId = shuttle.thingIDNumber,
                    Shuttle = shuttle.Label.CapitalizeFirst(),
                    Position = PositionOf(shuttle),
                    AlreadyAboard = contained,
                    TookJob = tookJob,
                    CarriedBy = carrier != null ? carrier.LabelShortCap : "",
                    Contained = nowContained,
                    Required = comp.requiredPawns.Contains(pawn),
                    // The job this call queued, not the one the pawn holds
                    // right now: the game interrupts the old job on its next
                    // tick, so reading it back would name "Pray" instead.
                    Job = orderedJob,
                    ShuttleState = ShuttleDtoOf(shuttle, map),
                };
                return ApiResult<ShuttleEnterResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<ShuttleEnterResultDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<ShuttleLaunchResultDto> Launch(int mapId, int shuttleId)
        {
            try
            {
                Thing shuttle;
                string shuttleProblem;
                if (!FindShuttleOnMap(mapId, shuttleId, out shuttle, out shuttleProblem))
                    return ApiResult<ShuttleLaunchResultDto>.Fail(shuttleProblem);

                CompShuttle comp = shuttle.TryGetComp<CompShuttle>();
                CompTransporter transporter = shuttle.TryGetComp<CompTransporter>();
                if (comp == null || transporter == null)
                    return ApiResult<ShuttleLaunchResultDto>.Fail(
                        $"{shuttle.Label} (id {shuttle.thingIDNumber}) is not a shuttle: it carries no shuttle component");

                if (ShipJobDefOf.FlyAway == null)
                    return ApiResult<ShuttleLaunchResultDto>.Fail(
                        shuttle.Label.CapitalizeFirst() + " (id " + shuttle.thingIDNumber
                        + ") cannot be sent away: this game has no shuttle send-away job, which comes with Royalty");

                ShuttleDto state = ShuttleDtoOf(shuttle, shuttle.MapHeld);
                ShuttleRules.SendState send = SendStateOf(comp, state);
                if (!ShuttleRules.NothingToSend(send))
                {
                    string refusal = ShuttleRules.SendProblem(send);
                    if (refusal != null)
                        return ApiResult<ShuttleLaunchResultDto>.Fail(
                            state.Label + " (id " + state.Id + "): " + refusal);
                }

                PositionDto position = state.Position;
                // The game's own lever, the one its "Send shuttle" gizmo pulls
                // (ShipJob_Wait.GetJobGizmos) and its quests pull
                // (SendShuttleAwayQuestPartUtility): the ship gets the FlyAway
                // job, which hands the contents to the leaving skyfaller, tells
                // the quest "FlewAway" and despawns the shuttle on the map.
                SendShuttleAwayQuestPartUtility.SendAway(shuttle, false);

                TransportShip ship = comp.shipParent;
                return ApiResult<ShuttleLaunchResultDto>.Ok(new ShuttleLaunchResultDto
                {
                    ShuttleId = state.Id,
                    Shuttle = state.Label,
                    Position = position,
                    Left = !shuttle.Spawned,
                    Job = ship != null && ship.curJob != null && ship.curJob.def != null
                        ? ship.curJob.def.defName : "",
                });
            }
            catch (Exception ex)
            {
                return ApiResult<ShuttleLaunchResultDto>.Fail(ex.ToString());
            }
        }

        // ShuttlesOn lists the things on a map that carry a CompShuttle: a quest
        // shuttle, an imperial shuttle, a player's passenger shuttle.
        private static IEnumerable<Thing> ShuttlesOn(Map map)
        {
            return map.listerThings.AllThings
                .Where(t => t != null && t.TryGetComp<CompShuttle>() != null)
                .OrderBy(t => t.thingIDNumber);
        }

        // FindShuttle resolves the shuttle a call means: the one it names, else
        // the single shuttle on the pawn's map. Two shuttles without an id is a
        // refusal that names both, because boarding the wrong one is what the
        // call must not guess at.
        private static bool FindShuttle(Pawn pawn, int shuttleId, out Thing shuttle, out string problem)
        {
            shuttle = null;
            problem = null;
            if (shuttleId > 0)
            {
                foreach (Map m in Find.Maps)
                {
                    shuttle = ShuttlesOn(m).FirstOrDefault(t => t.thingIDNumber == shuttleId);
                    if (shuttle != null) return true;
                }
                problem = $"no shuttle with id {shuttleId} is on a map: the `shuttles` read prints the id";
                return false;
            }

            Map map = pawn.MapHeld ?? Find.CurrentMap;
            if (map == null)
            {
                problem = "no map is open: a shuttle is read on the map it landed on";
                return false;
            }
            List<Thing> onMap = ShuttlesOn(map).ToList();
            if (onMap.Count == 0)
            {
                problem = "no shuttle is on the map: the `shuttles` read names one when it has landed";
                return false;
            }
            if (onMap.Count > 1)
            {
                problem = "the map has " + onMap.Count + " shuttles, so the call names one: "
                    + string.Join(", ", onMap.Select(t => t.Label + " (id " + t.thingIDNumber + ") at "
                        + t.Position.x + "," + t.Position.z).ToArray());
                return false;
            }
            shuttle = onMap[0];
            return true;
        }

        // FindShuttleOnMap resolves the shuttle a send-away call means: the one
        // it names, else the single shuttle on the map it names (the open one
        // when the call names no map). Two shuttles without an id is a refusal
        // that names both, because sending the wrong one away is what the call
        // must not guess at.
        private static bool FindShuttleOnMap(int mapId, int shuttleId, out Thing shuttle, out string problem)
        {
            shuttle = null;
            problem = null;
            if (shuttleId > 0)
            {
                foreach (Map m in Find.Maps)
                {
                    shuttle = ShuttlesOn(m).FirstOrDefault(t => t.thingIDNumber == shuttleId);
                    if (shuttle != null) return true;
                }
                problem = $"no shuttle with id {shuttleId} is on a map: the `shuttles` read prints the id";
                return false;
            }

            Map map = mapId > 0 ? MapHelper.GetMapByID(mapId) : Find.CurrentMap;
            if (map == null)
            {
                problem = mapId > 0
                    ? MapLookup.MapNotFound(mapId)
                    : "no map is open: a send-away call names the map the shuttle stands on in `map_id`";
                return false;
            }
            List<Thing> onMap = ShuttlesOn(map).ToList();
            if (onMap.Count == 0)
            {
                problem = "no shuttle is on the map: the `shuttles` read names one when it has landed";
                return false;
            }
            if (onMap.Count > 1)
            {
                problem = "the map has " + onMap.Count + " shuttles, so the call names one: "
                    + string.Join(", ", onMap.Select(t => t.Label + " (id " + t.thingIDNumber + ") at "
                        + t.Position.x + "," + t.Position.z).ToArray());
                return false;
            }
            shuttle = onMap[0];
            return true;
        }

        // SendStateOf collects what the read and the game say about a shuttle a
        // call wants to send away: the game's own readiness flag, the ship job
        // it holds, and the required pawns and items that are still missing,
        // worded the way the refusal needs them.
        //
        // AlreadyLeaving is the ship's *current* job, not
        // TransportShip.LeavingSoonAutomatically: a quest shuttle carries an
        // Unload and a FlyAway job in its queue while it still waits, so that
        // property is true on a shuttle nobody has sent yet (measured on the
        // shuttle-landed probe, 2026-11-02), and reading it here let a launch
        // through on a shuttle that was missing its pawn.
        private static ShuttleRules.SendState SendStateOf(CompShuttle comp, ShuttleDto state)
        {
            ShuttleRules.SendState send = new ShuttleRules.SendState
            {
                Shuttle = state.Label,
                ShuttleId = state.Id,
                Cell = "(" + state.Position.X + ", " + state.Position.Z + ")",
                Spawned = state.Spawned,
                AllRequiredLoaded = state.AllRequiredLoaded,
                AlreadyLeaving = comp.shipParent != null
                    && comp.shipParent.curJob != null
                    && ShipJobDefOf.FlyAway != null
                    && comp.shipParent.curJob.def == ShipJobDefOf.FlyAway,
            };
            int colonists = 0;
            foreach (ShuttlePawnDto p in state.ContainedPawns)
            {
                if (p.Kind == "colonist") colonists++;
            }
            foreach (ShuttlePawnDto p in state.RequiredPawns)
            {
                if (p.Contained || p.Dead) continue;
                send.Missing.Add(p.Name + " (id " + p.Id + ")" + MissingWhere(p));
            }
            foreach (ShuttleItemDto item in state.RequiredItems)
            {
                if (item.Loaded >= item.Required) continue;
                send.Missing.Add(item.Loaded + "/" + item.Required + " " + item.DefName);
            }
            if (state.RequiredColonistCount > 0 && colonists < state.RequiredColonistCount)
                send.Missing.Add(state.RequiredColonistCount + " colonist(s), " + colonists + " aboard");
            if (state.RequireAllColonistsOnMap)
                send.Missing.Add("every colonist on the map");
            return send;
        }

        // MissingWhere says why a required pawn is not aboard, in the words the
        // boarding rule uses for the same states.
        private static string MissingWhere(ShuttlePawnDto p)
        {
            if (!p.OnMap) return " is off the map";
            if (p.Downed) return " is down and cannot board on his own";
            return " is not aboard yet";
        }

        // StateOf collects what the game and the shuttle say about this pawn,
        // for the rule that turns it into a refusal or a go-ahead.
        private static ShuttleRules.State StateOf(Thing shuttle, CompShuttle comp, CompTransporter transporter,
            Pawn pawn, bool contained, string carrier)
        {
            return new ShuttleRules.State
            {
                Shuttle = shuttle.Label.CapitalizeFirst(),
                ShuttleId = shuttle.thingIDNumber,
                Cell = "(" + shuttle.Position.x + ", " + shuttle.Position.z + ")",
                ShuttleSpawned = shuttle.Spawned,
                Loading = transporter.LoadingInProgressOrReadyToLaunch,
                Pawn = pawn.LabelShortCap,
                PawnId = pawn.thingIDNumber,
                PawnSpawned = pawn.Spawned && pawn.MapHeld == shuttle.MapHeld,
                PawnWhere = WhereIs(pawn),
                PawnDead = pawn.Dead,
                PawnDowned = pawn.Downed,
                Carrier = carrier,
                AlreadyAboard = contained,
                Required = comp.requiredPawns.Contains(pawn),
                Allowed = comp.IsAllowedNow(pawn),
                CanReach = pawn.Spawned && shuttle.Spawned
                    && pawn.CanReach(shuttle, PathEndMode.Touch, Danger.Deadly),
                AcceptsColonists = comp.acceptColonists,
                OnlyAcceptColonists = comp.onlyAcceptColonists,
                OnlyAcceptHealthy = comp.onlyAcceptHealthy,
                PawnIsQuestLodger = pawn.IsQuestLodger(),
                PawnUnhealthy = pawn.Downed || pawn.InMentalState
                    || !pawn.health.capacities.CanBeAwake
                    || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving)
                    || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation),
                BelowMinAge = pawn.ageTracker != null && pawn.ageTracker.AgeBiologicalYearsFloat < comp.minAge,
                MaxColonistCount = comp.maxColonistCount,
                AtColonistLimit = comp.maxColonistCount >= 0
                    && CountBoarding(transporter, shuttle, pawn) >= comp.maxColonistCount,
            };
        }

        // CarrierFor finds a colonist who can carry a pawn that cannot walk into
        // the shuttle: the game's own "Carry to shuttle" option asks for a pawn
        // who can haul, can reach the downed one, and whose own reservation
        // allows it.
        private static Pawn CarrierFor(Pawn pawn, Thing shuttle)
        {
            Map map = shuttle.MapHeld;
            if (map == null) return null;
            foreach (Pawn candidate in map.mapPawns.FreeColonistsSpawned)
            {
                if (candidate == pawn || candidate.Dead || candidate.Downed) continue;
                if (candidate.WorkTypeIsDisabled(WorkTypeDefOf.Hauling)) continue;
                if (!candidate.CanReserveAndReach(pawn, PathEndMode.OnCell, Danger.Deadly)) continue;
                return candidate;
            }
            return null;
        }

        // CountBoarding is the game's own count for its colonist cap: the
        // colonists inside the shuttle plus the ones walking to it.
        private static int CountBoarding(CompTransporter transporter, Thing shuttle, Pawn except)
        {
            int count = 0;
            foreach (Thing thing in (IEnumerable<Thing>)transporter.innerContainer)
            {
                if (thing != except && thing is Pawn p && p.IsColonist) count++;
            }
            Map map = shuttle.MapHeld;
            if (map == null) return count;
            foreach (Pawn other in map.mapPawns.AllPawns)
            {
                if (other == except || other.jobs == null) continue;
                if (HeadsFor(other.jobs.curJob, shuttle)) count++;
                foreach (QueuedJob queued in other.jobs.jobQueue)
                {
                    if (HeadsFor(queued.job, shuttle)) count++;
                }
            }
            return count;
        }

        private static bool HeadsFor(Job job, Thing shuttle)
        {
            return job != null && job.def != null && job.def.driverClass != null
                && typeof(JobDriver_EnterTransporter).IsAssignableFrom(job.def.driverClass)
                && job.GetTarget(TargetIndex.A).Thing == shuttle;
        }

        // WhereIs names the place a pawn that is not on the shuttle's map is,
        // so the refusal says why he cannot board.
        private static string WhereIs(Pawn pawn)
        {
            if (pawn.Dead) return "";
            if (pawn.CarriedBy != null) return "carried by " + pawn.CarriedBy.LabelShort;
            if (pawn.GetCaravan() != null) return "in a caravan";
            if (pawn.MapHeld == null) return "off the map";
            if (pawn.MapHeld.Parent != null && pawn.MapHeld.Parent.def != null)
                return "on " + pawn.MapHeld.Parent.def.label;
            return "on another map";
        }

        // ShuttleDtoOf reads one shuttle as the tool prints it: the cell, the
        // people it waits for with the state each one is in, the items it
        // wants, and the quest's own countdown to its departure.
        private static ShuttleDto ShuttleDtoOf(Thing shuttle, Map map)
        {
            CompShuttle comp = shuttle.TryGetComp<CompShuttle>();
            CompTransporter transporter = shuttle.TryGetComp<CompTransporter>();
            ShuttleDto dto = new ShuttleDto
            {
                Id = shuttle.thingIDNumber,
                DefName = shuttle.def != null ? shuttle.def.defName : "",
                Label = shuttle.Label.CapitalizeFirst(),
                Faction = shuttle.Faction != null ? shuttle.Faction.Name : "",
                Position = PositionOf(shuttle),
                Spawned = shuttle.Spawned,
                Loading = transporter != null && transporter.LoadingInProgressOrReadyToLaunch,
                Autoload = comp.Autoload,
                AllRequiredLoaded = comp.AllRequiredThingsLoaded,
                RequiredColonistCount = comp.requiredColonistCount,
                RequireAllColonistsOnMap = comp.requireAllColonistsOnMap,
            };

            if (transporter != null)
            {
                foreach (Thing thing in (IEnumerable<Thing>)transporter.innerContainer)
                {
                    if (thing is Pawn p) dto.ContainedPawns.Add(PawnDtoOf(p, shuttle, true));
                }
                foreach (ThingDefCount required in comp.requiredItems)
                {
                    if (required == null || required.ThingDef == null) continue;
                    int loaded = 0;
                    foreach (Thing thing in (IEnumerable<Thing>)transporter.innerContainer)
                    {
                        if (thing.def == required.ThingDef) loaded += thing.stackCount;
                    }
                    dto.RequiredItems.Add(new ShuttleItemDto
                    {
                        DefName = required.ThingDef.defName,
                        Label = required.ThingDef.label,
                        Required = required.Count,
                        Loaded = loaded,
                    });
                }
            }
            foreach (Pawn p in comp.requiredPawns)
            {
                if (p == null) continue;
                dto.RequiredPawns.Add(PawnDtoOf(p, shuttle, transporter != null && transporter.innerContainer.Contains(p)));
            }

            int ticks;
            bool soon;
            DepartureOf(shuttle, out ticks, out soon);
            dto.LeavesInTicks = ticks;
            dto.LeavesSoon = soon;
            return dto;
        }

        // DepartureOf finds the quest part that sends this shuttle away and
        // reports its countdown: that part is what fails the quest, so the
        // number it holds is the one a run has to beat.
        private static void DepartureOf(Thing shuttle, out int ticks, out bool soon)
        {
            ticks = -1;
            soon = false;
            if (Find.QuestManager == null) return;
            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
            {
                if (quest == null || quest.Historical) continue;
                foreach (QuestPart part in quest.PartsListForReading)
                {
                    QuestPart_ShuttleLeaveDelay leave = part as QuestPart_ShuttleLeaveDelay;
                    if (leave == null || leave.shuttle != shuttle) continue;
                    if (leave.State != QuestPartState.Enabled) continue;
                    ticks = Math.Max(0, leave.TicksLeft);
                    soon = leave.AlertCritical;
                    return;
                }
            }
        }

        private static ShuttlePawnDto PawnDtoOf(Pawn pawn, Thing shuttle, bool contained)
        {
            return new ShuttlePawnDto
            {
                Id = pawn.thingIDNumber,
                Name = pawn.LabelShortCap,
                Contained = contained,
                Downed = pawn.Downed,
                OnMap = pawn.Spawned && pawn.MapHeld == shuttle.MapHeld,
                Dead = pawn.Dead,
                Kind = KindOf(pawn),
            };
        }

        // KindOf says what the pawn is to the colony, which decides whether a
        // shuttle takes him at all.
        private static string KindOf(Pawn pawn)
        {
            if (pawn.IsQuestLodger()) return "quest lodger";
            if (pawn.IsPrisonerOfColony) return "prisoner";
            if (pawn.IsColonist) return "colonist";
            if (pawn.IsSlave) return "slave";
            if (pawn.RaceProps != null && pawn.RaceProps.Animal) return "animal";
            if (pawn.Faction != null && pawn.Faction.IsPlayer) return "colonist";
            return "guest";
        }

        private static PositionDto PositionOf(Thing thing)
        {
            return new PositionDto
            {
                X = thing.Position.x,
                Y = thing.Position.y,
                Z = thing.Position.z,
            };
        }
    }
}
