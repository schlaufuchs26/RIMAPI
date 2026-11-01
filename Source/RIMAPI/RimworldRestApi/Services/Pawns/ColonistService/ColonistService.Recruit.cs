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
    // Growing the colony is the one thing the tool could not do (ticket #3137):
    // a wild person ate from the stockpile and a downed raider lay on the map,
    // and neither could be turned into a colonist without the screen. Every
    // order here is a job or a setting the game itself offers; nothing spawns a
    // pawn and nothing recruits one directly.
    public partial class ColonistService
    {
        // Taming is the growth path a wild person needs. The game's Tame job
        // only runs while the Tame designation stands (JobDriver_Tame fails when
        // the designation is gone), so the order and the job go out together.
        // The handler is chosen the way the game chooses one: the Animals work
        // type first, then the highest skill, and the work giver itself builds
        // the job so its own checks (reservation, skill, sleep, food) decide.
        // When no handler can take it now, the designation stays and the
        // colony's handlers pick it up when one is free.
        public ApiResult MakeJobTame(int mapId, int pawnId)
        {
            try
            {
                Map map = MapLookup.Resolve(mapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult.Fail(mapError);
                Pawn target = PawnHelper.FindPawnForOrder(map, pawnId);
                if (target == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                // A carried pawn is despawned into his carrier's hands, so the
                // spawned-things lookup answered "not found" for him (fox
                // #3719).
                string carried = CombatRefusal.CarriedRefusal(
                    Describe(target), target.CarriedBy?.LabelShort, "tame");
                if (carried != null) return ApiResult.Fail(carried);

                if (!TameUtility.CanTame(target))
                {
                    return ApiResult.Fail($"{Describe(target)} cannot be tamed: {TameRefusal(target)}");
                }
                if (map.designationManager.DesignationOn(target, DesignationDefOf.Tame) == null)
                {
                    map.designationManager.AddDesignation(new Designation(target, DesignationDefOf.Tame));
                }

                TameResultDto result = new TameResultDto { PawnId = pawnId, Name = target.LabelShort };
                WorkGiver_Tame giver = new WorkGiver_Tame();
                Job job = null;
                Pawn handler = null;
                foreach (Pawn candidate in HandlingPawns(map))
                {
                    Job candidateJob = giver.JobOnThing(candidate, target, true);
                    if (candidateJob != null)
                    {
                        handler = candidate;
                        job = candidateJob;
                        break;
                    }
                    // The check that turned this handler down, best first, so
                    // "no handler can take it now" reads as either a wait or a
                    // wall (fox #3625).
                    result.Refusals.Add(TameRefusal(candidate, target));
                }

                if (job == null)
                {
                    // The note is what a caller without the refusal list reads,
                    // so it must not promise a free handler when the gate is a
                    // skill or a capacity (fox #3625).
                    result.Note = result.Refusals.Count > 0
                        ? "no handler can take it now; the order stands and the refusals name the check that stops each one"
                        : "no handler can take it now; the order stands and the colony's handlers will take it "
                          + "when one is free and has food for it";
                    return ApiResult.Ok(result);
                }
                if (!handler.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    return ApiResult.Fail($"Failed to assign the tame job to {Describe(handler)}");
                }
                result.HandlerId = handler.thingIDNumber;
                result.Handler = handler.LabelShort;
                result.Job = job.def.defName;
                result.Queued = true;
                return ApiResult.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        // TameRefusal names the check that kept a handler off the tame job, in
        // the order WorkGiver_Tame.JobOnThing applies them (fox #3625). The
        // skill gate is the one that reads like a wait in the old answer and
        // never clears by waiting: taming a wild man needs Animals 7, and no
        // free handler gets there on its own.
        private static HandlerRefusalDto TameRefusal(Pawn pawn, Pawn target)
        {
            int need = TrainableUtility.MinimumHandlingSkill(target);
            int level = pawn.skills?.GetSkill(SkillDefOf.Animals)?.Level ?? 0;
            string reason;
            if (need > level)
            {
                reason = "needs Animals " + need;
            }
            else if (!WorkGiver_InteractAnimal.CanInteractWithAnimal(pawn, target, out string why, true, false))
            {
                reason = InteractionRefusal(why);
            }
            else if (TameUtility.TriedToTameTooRecently(target))
            {
                reason = "someone tried to tame him less than half a day ago";
            }
            else
            {
                // JobOnThing checks nothing else after this: the handler has no
                // food for the animal in his inventory and none on the map he
                // would take.
                reason = "no food for it in his inventory and none on the map he would take";
            }
            return new HandlerRefusalDto
            {
                Id = pawn.thingIDNumber,
                Name = pawn.LabelShort,
                Skill = level,
                SkillLabel = SkillDefOf.Animals.LabelCap,
                Reason = reason,
            };
        }

        // InteractionRefusal turns WorkGiver_InteractAnimal's own reason into a
        // phrase that reads after a colonist's name. A null reason is the
        // reservation or reach check, which sets no text of its own.
        private static string InteractionRefusal(string why)
        {
            if (string.IsNullOrEmpty(why)) return "cannot reserve or reach him";
            if (why == "animal downed") return "the animal is downed";
            if (why == "animal sleeping") return "the animal is asleep";
            if (why == "animal busy") return "the animal is busy";
            return why;
        }

        // Arrest is the growth path a captured raider needs, and the game's own
        // rules decide which of its two jobs applies: a downed enemy is captured
        // (JobDefOf.Capture), everyone else is arrested (JobDefOf.Arrest). Both
        // carry the target to a prisoner bed and make him a prisoner on arrival,
        // so a bed set to prisoners is required and the game's own bed lookup
        // picks it. Arrest is only offered while the colonist is drafted, so the
        // mod drafts him for the order and drops the draft again once the job is
        // over (RIMAPI_GameComponent.UndraftWhenFree); a wild person is arrested
        // without a draft, which is the one case the game allows undrafted.
        public ApiResult MakeJobArrest(int mapId, int pawnId, int arrestorId)
        {
            try
            {
                Map map = MapLookup.Resolve(mapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult.Fail(mapError);
                Pawn target = PawnHelper.FindPawnForOrder(map, pawnId);
                if (target == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                // A carried pawn cannot be reached by an arrestor yet (fox
                // #3719); the old lookup answered "not found" instead.
                string carried = CombatRefusal.CarriedRefusal(
                    Describe(target), target.CarriedBy?.LabelShort, "arrest");
                if (carried != null) return ApiResult.Fail(carried);

                Pawn arrestor = arrestorId > 0
                    ? PawnHelper.FindPawnForOrder(map, arrestorId)
                    : PickArrestor(map, target);
                if (arrestor == null)
                {
                    return ApiResult.Fail(
                        $"no colonist can take {Describe(target)} right now: everyone is downed, drafted elsewhere "
                        + "or cannot reach the pawn");
                }
                if (arrestor == target)
                {
                    return ApiResult.Fail($"Pawn {pawnId} cannot arrest itself");
                }
                // IsColonistPlayerControlled is false for a colonist in a mental
                // state or a downed one, so the membership test alone called him
                // a foreigner (ticket #3469); the state is named instead.
                string refusal = CombatRefusal.ActorRefusal(
                    Describe(arrestor), "arrest", PawnHelper.RefusalStateOf(arrestor));
                if (refusal != null) return ApiResult.Fail(refusal);

                bool capture = target.Downed && target.CanBeCaptured();
                if (!capture && !target.CanBeArrestedBy(arrestor))
                {
                    return ApiResult.Fail($"{Describe(target)} cannot be arrested: {ArrestRefusal(target)}");
                }

                JobDef def = capture ? JobDefOf.Capture : JobDefOf.Arrest;
                // A bed set to prisoners before its room changed shape leaves
                // the room's prison-cell flag stale, and the game's lookup then
                // passes that bed over (#3742).
                BedPick.RefreshStalePrisonCells(map);
                Building_Bed bed =
                    RestUtility.FindBedFor(target, arrestor, false, false, GuestStatus.Prisoner)
                    ?? RestUtility.FindBedFor(target, arrestor, false, true, GuestStatus.Prisoner);
                if (bed == null)
                {
                    return ApiResult.Fail(
                        "no prisoner bed: the game's Arrest and Capture jobs carry the pawn to a bed set to "
                        + "prisoners in a room the game counts as a prison cell; "
                        + BedPick.PrisonerBedRefusal(map, arrestor));
                }

                bool draftedNow = false;
                if (!arrestor.Drafted && !capture && !target.IsWildMan())
                {
                    arrestor.drafter.Drafted = true;
                    draftedNow = true;
                    RIMAPI_GameComponent.UndraftWhenFree(arrestor);
                }

                // The game's lookup is the first free prisoner bed, not the
                // safest one: in a burning colony that is the bed inside the
                // fire, and a prisoner carried into it cannot walk out (ticket
                // #3421, the same trap the rescue route had in #3383). A safe
                // prisoner bed replaces it when the colony has one; the answer
                // carries the room temperature either way.
                //
                // Since #3621 the search also runs when the bed is no cell: a
                // cool prisoner bed on open ground reads safe and still lets
                // the prisoner escape in the same minute, so a bed inside a
                // walled room replaces it when the colony has one.
                string noCell = BedPick.NoCellReason(map, bed);
                Building_Bed rejected = null;
                float rejectedTempC = 0f;
                bool rejectedFire = false;
                string rejectedNoCell = "";
                if (noCell != "" || BedSafety.IsUnsafe(bed.AmbientTemperature, BedPick.RoomHasFire(map, bed)))
                {
                    Building_Bed safer = BedPick.SaferBed(map, target, arrestor, bed, forPrisoners: true);
                    if (safer != null)
                    {
                        rejected = bed;
                        rejectedTempC = bed.AmbientTemperature;
                        rejectedFire = BedPick.RoomHasFire(map, bed);
                        rejectedNoCell = noCell;
                        bed = safer;
                    }
                }

                Job job = JobMaker.MakeJob(def, target, bed);
                job.count = 1;
                if (!arrestor.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    if (draftedNow) arrestor.drafter.Drafted = false;
                    return ApiResult.Fail($"Failed to assign the {def.defName} job to {Describe(arrestor)}");
                }

                ArrestResultDto result = new ArrestResultDto
                {
                    PawnId = pawnId,
                    Name = target.LabelShort,
                    ArrestorId = arrestor.thingIDNumber,
                    Arrestor = arrestor.LabelShort,
                    Job = def.defName,
                    BedId = bed.thingIDNumber,
                    Bed = bed.Label,
                    Drafted = arrestor.Drafted,
                    BedRoomTempC = bed.AmbientTemperature,
                    BedRoomHasFire = BedPick.RoomHasFire(map, bed),
                    BedOutdoors = BedPick.Outdoors(map, bed),
                    BedNoCellReason = BedPick.NoCellReason(map, bed),
                };
                Room room = BedPick.BedRoom(bed);
                if (room != null)
                {
                    result.BedRoomId = room.ID;
                    result.BedRoomLabel = BedPick.RoomLabel(room);
                }
                if (rejected != null)
                {
                    result.RejectedBedId = rejected.thingIDNumber;
                    result.RejectedBed = rejected.Label;
                    Room rejectedRoom = BedPick.BedRoom(rejected);
                    if (rejectedRoom != null) result.RejectedBedRoomLabel = rejectedRoom.GetRoomRoleLabel();
                    result.RejectedBedTempC = rejectedTempC;
                    result.RejectedBedRoomHasFire = rejectedFire;
                    result.RejectedBedNoCellReason = rejectedNoCell;
                }
                return ApiResult.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        // A prisoner's interaction mode is the recruitment policy the prisoner
        // tab sets: AttemptRecruit makes the warden reduce resistance and then
        // recruit, ReduceResistance only talks, Release lets him go. The game
        // hides the recruit modes for a wild person (allowOnWildMan is false on
        // both), because a wild person is tamed, not recruited; the refusal says
        // so instead of setting a mode the game would reset on load.
        public ApiResult SetPrisonerInteraction(int pawnId, string mode)
        {
            try
            {
                Pawn pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                // A pawn on his way to the prisoner bed is not a prisoner yet,
                // and the game takes no policy for him while he is carried (fox
                // #3719).
                string carried = CombatRefusal.CarriedRefusal(
                    Describe(pawn), pawn.CarriedBy?.LabelShort, "set a recruitment policy for");
                if (carried != null) return ApiResult.Fail(carried);
                if (!pawn.IsPrisonerOfColony)
                {
                    return ApiResult.Fail($"{Describe(pawn)} is not a prisoner of the colony");
                }
                PrisonerInteractionModeDef def = ResolveInteractionMode(mode);
                if (def == null)
                {
                    string names = string.Join(", ", DefDatabase<PrisonerInteractionModeDef>.AllDefs
                        .Select(d => d.defName));
                    return ApiResult.Fail($"unknown prisoner interaction mode \"{mode}\"; the game has: {names}");
                }
                if (!def.allowOnWildMan && pawn.IsWildMan())
                {
                    return ApiResult.Fail(
                        $"{Describe(pawn)} is a wild person: the game does not recruit wild people as prisoners, "
                        + "it tames them (`tame`), and a tamed wild person joins as a colonist");
                }
                if (def.hideIfNotRecruitable && !pawn.guest.Recruitable)
                {
                    return ApiResult.Fail($"{Describe(pawn)} is not recruitable (the game marks him unrecruitable)");
                }

                if (def.isNonExclusiveInteraction)
                {
                    pawn.guest.ToggleNonExclusiveInteraction(def, true);
                }
                else
                {
                    pawn.guest.SetExclusiveInteraction(def);
                }

                return ApiResult.Ok(new PrisonerModeResultDto
                {
                    PawnId = pawnId,
                    Name = pawn.LabelShort,
                    Mode = def.defName,
                    ModeLabel = def.label,
                    Resistance = pawn.guest.Resistance,
                    Will = pawn.guest.will,
                    Recruitable = pawn.guest.Recruitable,
                });
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        // ResolveInteractionMode takes the defName the tool sends and also the
        // label the game shows ("recruit"), so a caller need not know which.
        private static PrisonerInteractionModeDef ResolveInteractionMode(string mode)
        {
            if (string.IsNullOrEmpty(mode)) return null;
            string wanted = mode.Trim();
            foreach (PrisonerInteractionModeDef def in DefDatabase<PrisonerInteractionModeDef>.AllDefs)
            {
                if (string.Equals(def.defName, wanted, StringComparison.OrdinalIgnoreCase)) return def;
            }
            foreach (PrisonerInteractionModeDef def in DefDatabase<PrisonerInteractionModeDef>.AllDefs)
            {
                if (string.Equals(def.label, wanted, StringComparison.OrdinalIgnoreCase)) return def;
            }
            return null;
        }

        // HandlingPawns orders the colony the way the game hands out the
        // Handling work: the pawns whose Handling work is on come first, and
        // the highest Animals skill among them tries first. With nobody on
        // Handling, every colonist gets a chance, because an ordered job runs
        // even when the work type is off. Both tame and slaughter ask through it.
        private static IEnumerable<Pawn> HandlingPawns(Map map)
        {
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned
                .OrderByDescending(p => p.skills?.GetSkill(SkillDefOf.Animals)?.Level ?? 0)
                .ToList();
            List<Pawn> handlers = colonists
                .Where(p => p.workSettings != null && p.workSettings.WorkIsActive(WorkTypeDefOf.Handling))
                .ToList();
            return handlers.Count > 0 ? handlers : colonists;
        }

        // PickArrestor takes the nearest colonist who could take the pawn and
        // reach it, the same two checks the game's own arrest option makes.
        private static Pawn PickArrestor(Map map, Pawn target)
        {
            bool capture = target.Downed && target.CanBeCaptured();
            return map.mapPawns.FreeColonistsSpawned
                .Where(p => !p.Downed && !p.InMentalState)
                .Where(p => capture ? target.CanBeCaptured() : target.CanBeArrestedBy(p))
                .Where(p => p.CanReach(target, PathEndMode.OnCell, Danger.Deadly))
                .OrderBy(p => (p.Position - target.Position).LengthHorizontalSquared)
                .FirstOrDefault();
        }

        // Every order route resolves its pawn through PawnHelper.FindPawnForOrder,
        // which also reaches a pawn who is despawned into a carrier's hands
        // (fox #3719); this service keeps no second lookup of its own.
        private static string Describe(Pawn pawn)
        {
            return $"{pawn.LabelShort} (id {pawn.thingIDNumber})";
        }

        // TameRefusal says which of TameUtility.CanTame's conditions failed, in
        // the game's own terms.
        private static string TameRefusal(Pawn pawn)
        {
            if (!pawn.AnimalOrWildMan()) return "only animals and wild people are tamed";
            // A quest guest counts as the colony's animal, so CanTame refuses it
            // without saying why (fox #3611).
            if (pawn.IsQuestLodger()) return "he is a quest guest, so the quest keeps him until it ends";
            if (pawn.Faction != null && pawn.Faction.def.humanlikeFaction)
                return $"he belongs to {pawn.Faction.Name}, so he is captured or arrested, not tamed";
            if (pawn.GetStatValue(StatDefOf.Wildness) >= 1f)
                return "his wildness is 100 %, so taming is not offered";
            if (pawn.RaceProps.animalType == AnimalType.Dryad) return "dryads are not tamed";
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.Scaria)) return "he has scaria";
            return "the game does not offer taming for him";
        }

        // ArrestRefusal says why CanBeArrestedBy said no.
        private static string ArrestRefusal(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike) return "only humanlike pawns are arrested";
            if (pawn.IsSubhuman) return "subhumans are not arrested";
            if (pawn.InAggroMentalState) return "he is in a mental state";
            if (pawn.HostileTo(Faction.OfPlayer))
                return "an enemy who is still standing fights; down him first and `arrest` captures him";
            if (pawn.IsPrisonerOfColony) return "he is already a prisoner of the colony";
            if (pawn.DevelopmentalStage.Baby()) return "babies are not arrested";
            return "the game's own arrest check refuses him";
        }
    }
}
