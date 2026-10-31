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
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult.Fail($"Map with ID={mapId} not found");
                Pawn target = FindPawnOnMap(map, pawnId);
                if (target == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");

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
                foreach (Pawn candidate in TameHandlers(map))
                {
                    Job candidateJob = giver.JobOnThing(candidate, target, true);
                    if (candidateJob != null)
                    {
                        handler = candidate;
                        job = candidateJob;
                        break;
                    }
                }

                if (job == null)
                {
                    result.Note =
                        "no handler can take it now; the order stands and the colony's handlers will take it "
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
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult.Fail($"Map with ID={mapId} not found");
                Pawn target = FindPawnOnMap(map, pawnId);
                if (target == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");

                Pawn arrestor = arrestorId > 0
                    ? FindPawnOnMap(map, arrestorId)
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
                if (arrestor.Faction != Faction.OfPlayer || !arrestor.IsColonistPlayerControlled)
                {
                    return ApiResult.Fail($"{Describe(arrestor)} is not one of the colony's colonists");
                }

                bool capture = target.Downed && target.CanBeCaptured();
                if (!capture && !target.CanBeArrestedBy(arrestor))
                {
                    return ApiResult.Fail($"{Describe(target)} cannot be arrested: {ArrestRefusal(target)}");
                }

                JobDef def = capture ? JobDefOf.Capture : JobDefOf.Arrest;
                Building_Bed bed =
                    RestUtility.FindBedFor(target, arrestor, false, false, GuestStatus.Prisoner)
                    ?? RestUtility.FindBedFor(target, arrestor, false, true, GuestStatus.Prisoner);
                if (bed == null)
                {
                    return ApiResult.Fail(
                        "no prisoner bed: the game's Arrest and Capture jobs carry the pawn to a bed set to "
                        + "prisoners, so build one and set it to prisoners first (`things` shows beds, "
                        + "`build name=Bed` places one)");
                }

                bool draftedNow = false;
                if (!arrestor.Drafted && !capture && !target.IsWildMan())
                {
                    arrestor.drafter.Drafted = true;
                    draftedNow = true;
                    RIMAPI_GameComponent.UndraftWhenFree(arrestor);
                }

                Job job = JobMaker.MakeJob(def, target, bed);
                job.count = 1;
                if (!arrestor.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    if (draftedNow) arrestor.drafter.Drafted = false;
                    return ApiResult.Fail($"Failed to assign the {def.defName} job to {Describe(arrestor)}");
                }

                return ApiResult.Ok(new ArrestResultDto
                {
                    PawnId = pawnId,
                    Name = target.LabelShort,
                    ArrestorId = arrestor.thingIDNumber,
                    Arrestor = arrestor.LabelShort,
                    Job = def.defName,
                    BedId = bed.thingIDNumber,
                    Bed = bed.Label,
                    Drafted = arrestor.Drafted,
                });
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

        // TameHandlers orders the colony the way the game does: the pawns whose
        // Animals work is on come first, and the highest skill among them tries
        // first. With nobody on Animals, every colonist gets a chance, because
        // an ordered job runs even when the work type is off.
        private static IEnumerable<Pawn> TameHandlers(Map map)
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

        private static Pawn FindPawnOnMap(Map map, int pawnId)
        {
            return map.listerThings.AllThings.OfType<Pawn>()
                .FirstOrDefault(p => p.thingIDNumber == pawnId);
        }

        private static string Describe(Pawn pawn)
        {
            return $"{pawn.LabelShort} (id {pawn.thingIDNumber})";
        }

        // TameRefusal says which of TameUtility.CanTame's conditions failed, in
        // the game's own terms.
        private static string TameRefusal(Pawn pawn)
        {
            if (!pawn.AnimalOrWildMan()) return "only animals and wild people are tamed";
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
