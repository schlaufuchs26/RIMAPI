using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    public partial class ColonistService : IColonistService
    {
        public ColonistService() { }

        public ApiResult<PawnDto> GetColonist(int pawnId)
        {
            try
            {
                var result = ColonistsHelper.GetSingleColonistDto(pawnId);
                if (result == null)
                {
                    return ApiResult<PawnDto>.Fail($"Failed to find pawn with id: {pawnId}");
                }
                return ApiResult<PawnDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<PawnDto>
                    .Fail($"Failed to find pawn with id: {pawnId} - {ex.Message}");
            }
        }

        public ApiResult<BodyPartsDto> GetColonistBodyParts(int pawnId)
        {
            BodyPartsDto bodyParts = new BodyPartsDto();

            try
            {
                Pawn colonist = PawnsFinder
                    .AllMaps_FreeColonists.Where(p => p.thingIDNumber == pawnId)
                    .FirstOrDefault();

                Material bodyMaterial = colonist.Drawer.renderer.BodyGraphic.MatAt(Rot4.South);
                Texture2D bodyTexture = (Texture2D)bodyMaterial.mainTexture;

                Material headMaterial = colonist.Drawer.renderer.HeadGraphic.MatAt(Rot4.South);
                Texture2D headTexture = (Texture2D)headMaterial.mainTexture;

                bodyParts.BodyImage = TextureHelper.TextureToBase64(bodyTexture);
                bodyParts.BodyColor = bodyMaterial.color.ToString();
                bodyParts.HeadImage = TextureHelper.TextureToBase64(headTexture);
                bodyParts.HeadColor = headMaterial.color.ToString();
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error getting body image - {ex}");
            }
            return ApiResult<BodyPartsDto>.Ok(bodyParts);
        }

        public ApiResult<PawnDetailedRequestDto> GetColonistDetailed(int pawnId)
        {
            try
            {
                var result = ColonistsHelper.GetSingleColonistDetailedDto(pawnId);
                if (result == null)
                {
                    return ApiResult<PawnDetailedRequestDto>.Fail($"Failed to find pawn with id: {pawnId}");
                }
                return ApiResult<PawnDetailedRequestDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<PawnDetailedRequestDto>
                    .Fail($"Failed to find pawn with id: {pawnId} - {ex.Message}");
            }
        }

        public ApiResult<PawnInventoryDto> GetColonistInventory(int pawnId)
        {
            try
            {

                Pawn colonist = ColonistsHelper.GetColonistsList()
                    .Where(p => p.thingIDNumber == pawnId)
                    .FirstOrDefault();

                if (colonist == null) return ApiResult<PawnInventoryDto>
                    .Fail($"Colonist {pawnId} not found.");

                var inventory = PawnHelper.GetPawnInventory(colonist);
                return ApiResult<PawnInventoryDto>.Ok(inventory);
            }
            catch (Exception ex)
            {
                return ApiResult<PawnInventoryDto>.Fail(ex.Message);
            }
        }

        public ApiResult<List<PawnDto>> GetColonists()
        {
            var result = ColonistsHelper.GetColonistsDtos();
            return ApiResult<List<PawnDto>>.Ok(result);
        }

        public ApiResult<List<PawnPositionDto>> GetColonistPositions()
        {
            var result = ColonistsHelper.GetColonistPositions();
            return ApiResult<List<PawnPositionDto>>.Ok(result);
        }

        public ApiResult<List<PawnDetailedRequestDto>> GetColonistsDetailed()
        {
            var result = ColonistsHelper.GetColonistsDetailedDtos();
            return ApiResult<List<PawnDetailedRequestDto>>.Ok(result);
        }

        public ApiResult<OpinionAboutPawnDto> GetOpinionAboutPawn(int pawnId, int otherPawnId)
        {
            Pawn pawn = PawnHelper.FindPawnById(pawnId);
            if (pawn == null)
                return ApiResult<OpinionAboutPawnDto>.Fail($"Failed to find pawn with {pawnId} id");

            Pawn other = PawnHelper.FindPawnById(otherPawnId);
            if (other == null)
                return ApiResult<OpinionAboutPawnDto>.Fail($"Failed to find other pawn with {otherPawnId} id");

            var result = new OpinionAboutPawnDto
            {
                Opinion = pawn.relations.OpinionOf(other),
                OpinionAboutMe = other.relations.OpinionOf(pawn),
            };
            return ApiResult<OpinionAboutPawnDto>.Ok(result);
        }

        public ApiResult<List<OutfitDto>> GetOutfits()
        {
            var result = PawnHelper.GetOutfits();
            return ApiResult<List<OutfitDto>>.Ok(result);
        }

        public ApiResult<ImageDto> GetPawnPortraitImage(
            int pawnId,
            int width,
            int height,
            string direction
        )
        {
            Pawn pawn = PawnHelper.FindPawnById(pawnId);
            if (pawn == null)
            {
                return ApiResult<ImageDto>.Fail($"Pawn with ID {pawnId} not found.");
            }
            var result = TextureHelper.GetPawnPortraitImage(pawn, width, height, direction);
            return ApiResult<ImageDto>.Ok(result);
        }

        public ApiResult<List<TimeAssignmentDto>> GetTimeAssignmentsList()
        {
            var result = DefDatabase<TimeAssignmentDef>
                .AllDefs.Select(s => new TimeAssignmentDto { Name = s.defName })
                .ToList();
            return ApiResult<List<TimeAssignmentDto>>.Ok(result);
        }

        public ApiResult<TraitDefDto> GetTraitDefDto(string traitName)
        {
            TraitDef trait = DefDatabase<TraitDef>.GetNamed(traitName, false);
            var result = DefDatabaseHelper.GetTraitDefDto(trait);
            return ApiResult<TraitDefDto>.Ok(result);
        }

        public ApiResult<WorkListDto> GetWorkList()
        {
            WorkListDto workList = new WorkListDto { Work = new List<string>() };

            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefs)
            {
                if (workType == null)
                    continue;
                workList.Work.Add(workType.defName);
            }

            return ApiResult<WorkListDto>.Ok(workList);
        }

        // GetColonistWorkPriorities answers "what will this pawn do, and what is
        // it set to" in one read (fox, #3085): every work type with its stored
        // level, the level the game acts on, and whether the pawn can do it at
        // all. Without it a caller has to guess from the work tab.
        public ApiResult<ColonistWorkPrioritiesDto> GetColonistWorkPriorities(int pawnId)
        {
            try
            {
                if (pawnId <= 0)
                {
                    return ApiResult<ColonistWorkPrioritiesDto>.Fail(
                        "pawn_id is required; it is the id the colonists read reports."
                    );
                }

                Pawn pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null)
                {
                    return ApiResult<ColonistWorkPrioritiesDto>.Fail(
                        $"Could not find pawn with ID {pawnId}"
                    );
                }

                var readiness = WorkSettingsRules.Of(
                    hasWorkSettings: pawn.workSettings != null,
                    everWork: pawn.workSettings != null && pawn.workSettings.EverWork
                );

                var result = new ColonistWorkPrioritiesDto
                {
                    PawnId = pawn.thingIDNumber,
                    PawnName = pawn.LabelShortCap,
                    ManualPriorities = Find.PlaySettings?.useWorkPriorities ?? false,
                    Initialized = readiness == WorkSettingsRules.Readiness.Ready,
                    Readiness = WorkSettingsRules.Name(readiness),
                };

                if (!result.Initialized)
                {
                    return ApiResult<ColonistWorkPrioritiesDto>.Ok(result);
                }

                DefMap<WorkTypeDef, int> stored = StoredWorkPriorities(pawn);
                foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefs)
                {
                    if (workType == null)
                        continue;
                    int priority = pawn.workSettings.GetPriority(workType);
                    result.Work.Add(
                        new WorkPriorityEntryDto
                        {
                            WorkType = workType.defName,
                            Priority = priority,
                            Stored = stored != null ? stored[workType] : priority,
                            Disabled = pawn.WorkTypeIsDisabled(workType),
                        }
                    );
                }

                return ApiResult<ColonistWorkPrioritiesDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<ColonistWorkPrioritiesDto>.Fail(ex.Message);
            }
        }

        // DownedNote repeats the downing reason on a refusal (ticket #3288): a
        // patient with nothing to tend can still be down for a reason a run
        // must know, and "nothing to tend" alone sent a play run hunting
        // through the in-game health panel for five game days.
        private static string DownedNote(Pawn pawn)
        {
            string reason = PawnHelper.DownedReason(pawn);
            return reason == "" ? "" : $" (downed: {reason})";
        }

        // StoredWorkPriorities reads the levels the pawn keeps. The game holds
        // them in a private DefMap and GetPriority collapses every active type
        // to 3 while manual priorities are off, so the numbers behind the work
        // tab are only visible through that field. Null means unreadable, and
        // the caller falls back to the effective level.
        private static DefMap<WorkTypeDef, int> StoredWorkPriorities(Pawn pawn)
        {
            try
            {
                return Traverse
                    .Create(pawn.workSettings)
                    .Field("priorities")
                    .GetValue<DefMap<WorkTypeDef, int>>();
            }
            catch (Exception ex)
            {
                Core.LogApi.Error(
                    $"Error reading stored work priorities for pawn {pawn.thingIDNumber} - {ex.Message}"
                );
                return null;
            }
        }

        // Tending is the game's own job: a doctor walks to the patient and treats
        // the wound, with a named medicine when one is given. The tool can queue
        // it but must not invent the outcome, so the checks below are the ones
        // the work giver makes before it hands the job out, and each refusal
        // names why (ticket #3117).
        public ApiResult MakeJobTend(int mapId, int pawnId, int patientId, int medicineId)
        {
            try
            {
                Map map = MapLookup.Resolve(mapId, MapHelper.GetMapByID, out string mapError);
                if (map == null)
                {
                    return ApiResult.Fail(mapError);
                }
                Pawn doctor = PawnHelper.FindPawnForOrder(map, pawnId);
                if (doctor == null)
                {
                    return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                }
                Pawn patient = PawnHelper.FindPawnForOrder(map, patientId);
                if (patient == null)
                {
                    return ApiResult.Fail($"Patient with ID={patientId} not found");
                }
                // A carried patient is out of everyone's reach, and the old
                // lookup answered "not found" for him while a raider walked him
                // off the map (fox #3665).
                string carriedPatient = CombatRefusal.CarriedRefusal(
                    Describe(patient), patient.CarriedBy?.LabelShort, "tend");
                if (carriedPatient != null) return ApiResult.Fail(carriedPatient);
                // A pawn ordered to tend itself is the game's own self-tend:
                // the Health tab's Self-tend option is what WorkGiver_TendSelf
                // reads before it hands the job out, and JobDriver_TendPatient
                // ends the job when the option is off. The tool refused this
                // outright, so a colony of one watched a bleeding pawn die next
                // to a shelf of medicine (ticket #3247).
                bool selfTend = patient == doctor;
                if (selfTend)
                {
                    if (doctor.playerSettings == null || !doctor.playerSettings.selfTend)
                    {
                        return ApiResult.Fail(
                            $"Pawn {pawnId} cannot tend itself: the Self-tend option on its Health tab is off"
                        );
                    }
                    if (!doctor.RaceProps.Humanlike)
                    {
                        return ApiResult.Fail(
                            $"Pawn {pawnId} cannot tend itself: only humanlikes have a Self-tend option"
                        );
                    }
                    if (!doctor.health.HasHediffsNeedingTend(false))
                    {
                        return ApiResult.Fail(
                            $"Pawn {pawnId} has nothing to tend: no hediff needs treatment right now"
                                + DownedNote(doctor)
                        );
                    }
                    if (!doctor.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        return ApiResult.Fail(
                            $"Pawn {pawnId} cannot tend itself: its Manipulation capacity is too low"
                        );
                    }
                    if (doctor.InAggroMentalState)
                    {
                        return ApiResult.Fail(
                            $"Pawn {pawnId} cannot tend itself while in an aggressive mental state"
                        );
                    }
                    if (doctor.IsColonist && doctor.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
                    {
                        return ApiResult.Fail(
                            $"Pawn {pawnId} cannot self-tend: the Doctor work type is switched off or the pawn is incapable"
                        );
                    }
                    if (!map.reservationManager.CanReserve(doctor, doctor))
                    {
                        return ApiResult.Fail(
                            $"Pawn {pawnId} cannot tend itself: its own reservation is held by something else"
                        );
                    }
                }
                else
                {
                    if (doctor.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
                    {
                        return ApiResult.Fail(
                            $"Pawn {pawnId} cannot doctor: the Doctor work type is switched off or the pawn is incapable"
                        );
                    }
                    if (!patient.health.HasHediffsNeedingTend(false))
                    {
                        return ApiResult.Fail(
                            $"Patient {patientId} has nothing to tend: no hediff needs treatment right now"
                                + DownedNote(patient)
                        );
                    }
                }

                Thing medicine = null;
                if (medicineId > 0)
                {
                    medicine = map.listerThings.AllThings.FirstOrDefault(t =>
                        t.thingIDNumber == medicineId
                    );
                    if (medicine == null)
                    {
                        return ApiResult.Fail($"Medicine with ID={medicineId} not found");
                    }
                    if (!medicine.def.IsMedicine)
                    {
                        return ApiResult.Fail(
                            $"Thing {medicineId} is {medicine.def.defName}, not medicine"
                        );
                    }
                }

                Job job;
                try
                {
                    job = JobMaker.MakeJob(JobDefOf.TendPatient, patient, medicine);
                    job.count = 1;
                }
                catch (Exception ex)
                {
                    return ApiResult.Fail(
                        $"Failed to build the tend job for patient {patientId}: "
                            + $"{ex.GetType().Name}: {ex.Message}"
                    );
                }

                if (!doctor.jobs.TryTakeOrderedJob(job))
                {
                    // "Failed to assign job" alone names nothing a caller can
                    // act on; a pawn in a tantrum or on the floor is the usual
                    // reason the game takes no ordered job (fox, #3548).
                    return ApiResult.Fail(
                        $"Failed to assign job to pawn: {doctor.thingIDNumber}"
                            + PawnHelper.NoJobReason(doctor)
                    );
                }
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }


        public ApiResult MakeJobEquip(int mapId, int pawnId, int equipmentId)
        {
            try
            {
                Map map = MapLookup.Resolve(mapId, MapHelper.GetMapByID, out string mapError);
                if (map == null)
                {
                    return ApiResult.Fail(mapError);
                }
                Pawn pawn = PawnHelper.FindPawnForOrder(map, pawnId);
                if (pawn == null)
                {
                    return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                }
                // A carried pawn has no hands free for a new weapon, and the old
                // lookup answered "not found" for him (fox #3719).
                string carried = CombatRefusal.ActorRefusal(
                    Describe(pawn), "equip", PawnHelper.RefusalStateOf(pawn));
                if (carried != null) return ApiResult.Fail(carried);

                Thing foundThing = map.listerThings.AllThings.FirstOrDefault(t =>
                    t.thingIDNumber == equipmentId
                );
                if (foundThing == null)
                {
                    // The stored read lists a container's contents, so an id from
                    // it can name a weapon or a piece of apparel that is not on the
                    // map: hauled into a frame, dropped into a grave, held by a
                    // building. Naming the container says why the id does nothing
                    // instead of a bare "not found" (ticket #3200).
                    Thing holder = ThingHolderHelper.FindContainerHolding(map, equipmentId);
                    if (holder != null)
                    {
                        return ApiResult.Fail(
                            ThingHolderHelper.DescribeHeld(equipmentId, holder)
                                + "; nothing inside a container can be equipped, so it has to be hauled out first"
                        );
                    }
                    return ApiResult.Fail($"Thing with ID={equipmentId} not found");
                }

                // The def decides the job, and only its own check may run: a
                // weapon has no ThingDef.apparel, so ApparelUtility.HasPartsToWear
                // dereferences null there and upstream's blanket catch turned that
                // NullReferenceException into "Failed to determine equip job" for
                // every weapon (#3102). Apparel is worn, everything else that can
                // be held is equipped (beer included, JobDriver_Equip handles it).
                Job job = null;
                try
                {
                    if (foundThing.def.IsApparel)
                    {
                        if (!ApparelUtility.HasPartsToWear(pawn, foundThing.def))
                        {
                            return ApiResult.Fail(
                                $"Pawn {pawnId} has no body part to wear {foundThing.def.defName}"
                            );
                        }
                        job = JobMaker.MakeJob(JobDefOf.Wear, foundThing);
                    }
                    else
                    {
                        if (pawn.equipment == null)
                        {
                            return ApiResult.Fail($"Pawn {pawnId} cannot hold equipment");
                        }
                        string cantReason = null;
                        if (!EquipmentUtility.CanEquip(foundThing, pawn, out cantReason))
                        {
                            return ApiResult.Fail(
                                $"Pawn {pawnId} cannot equip {foundThing.def.defName}: "
                                    + (cantReason ?? "unknown reason")
                            );
                        }
                        job = JobMaker.MakeJob(JobDefOf.Equip, foundThing);
                    }
                }
                catch (Exception ex)
                {
                    return ApiResult.Fail(
                        $"Failed to determine equip job for item {foundThing.def.defName}: "
                            + $"{ex.GetType().Name}: {ex.Message}"
                    );
                }

                bool result = pawn.jobs.TryTakeOrderedJob(job);
                if (!result)
                {
                    return ApiResult.Fail($"Failed to assign job to pawn: {pawn.thingIDNumber}");
                }
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult SetColonistsWorkPriority(ColonistsWorkPrioritiesRequestDto body)
        {
            try
            {
                if (body?.Priorities == null)
                {
                    return ApiResult.Fail("Invalid payload: 'priorities' array is missing or null.");
                }

                if (body.Priorities.Count == 0)
                {
                    return ApiResult.Ok();
                }

                var warnings = new List<string>();
                foreach (var colonistPriorityUpdate in body.Priorities)
                {
                    var result = SetColonistWorkPriority(colonistPriorityUpdate);
                    if (result.Success == false)
                    {
                        warnings.AddRange(result.Errors);
                    }
                }

                if (warnings.Count > 0)
                {
                    string combinedWarnings = string.Join("; ", warnings);

                    if (warnings.Count == body.Priorities.Count)
                    {
                        return ApiResult.Fail(combinedWarnings);
                    }
                    return ApiResult.Partial(warnings);
                }

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult SetColonistWorkPriority(WorkPriorityRequestDto body)
        {
            try
            {
                // Find the pawn by thingIDNumber
                Pawn pawn = PawnHelper.FindPawnById(body.Id);
                if (pawn == null)
                {
                    return ApiResult.Fail($"Could not find pawn with ID {body.Id}");
                }

                // Find the WorkTypeDef by defName
                WorkTypeDef workTypeDef = DefDatabase<WorkTypeDef>.GetNamedSilentFail(body.Work);
                if (workTypeDef == null)
                {
                    return ApiResult.Fail($"Could not find WorkTypeDef with defName {body.Work}");
                }

                // A pawn whose priorities were never created used to be refused
                // here, so the doctor line could name a candidate whose fix the
                // write then would not carry out (#3566). The game creates the
                // defaults lazily on the first write
                // (EnableAndInitializeIfNotAlreadyInitialized), so the mod does
                // the same and the level lands. Only a pawn without the
                // settings component at all is a refusal.
                var readiness = WorkSettingsRules.Of(
                    hasWorkSettings: pawn.workSettings != null,
                    everWork: pawn.workSettings != null && pawn.workSettings.EverWork
                );
                if (readiness == WorkSettingsRules.Readiness.NoSettings)
                {
                    return ApiResult.Fail(WorkSettingsRules.NoSettingsRefusal(pawn.LabelShort));
                }
                if (readiness == WorkSettingsRules.Readiness.NeedsInitialization)
                {
                    pawn.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
                }

                // Check if the work type is disabled for this pawn
                if (body.Priority != 0 && pawn.WorkTypeIsDisabled(workTypeDef))
                {
                    return ApiResult.Fail(
                        $"Cannot set priority for disabled work type {workTypeDef.defName} on pawn {pawn.LabelShort}"
                    );
                }

                // Validate priority range (0-9)
                if (body.Priority < 0 || body.Priority > 9)
                {
                    return ApiResult.Fail($"Invalid priority {body.Priority}. Must be between 0 and 4");
                }

                // Set the priority
                pawn.workSettings.SetPriority(workTypeDef, body.Priority);

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult SetTimeAssignment(PawnTimeAssignmentRequestDto body)
        {
            try
            {
                Pawn pawn = PawnHelper.FindPawnById(body.PawnId);
                TimeAssignmentDef assignmentDef = DefDatabase<TimeAssignmentDef>
                    .AllDefs.Where(p => p.defName.ToLower() == body.Assignment.ToLower())
                    .FirstOrDefault();
                if (assignmentDef == null)
                {
                    return ApiResult.Fail(
                        $"Failed to find assignment def with {body.Assignment} name"
                    );
                }
                pawn.timetable.SetAssignment(body.Hour, assignmentDef);

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult<List<ApiV1PawnDetailedDto>> GetColonistsDetailedV1()
        {
            List<ApiV1PawnDetailedDto> result = new List<ApiV1PawnDetailedDto>();
            foreach (PawnDetailedRequestDto data in ColonistsHelper.GetColonistsDetailedDtos())
            {
                result.Add(new ApiV1PawnDetailedDto
                {
                    Colonist = data.Pawn,
                    BodySize = data.Detailes.BodySize,
                    Sleep = data.Detailes.Sleep,
                    Comfort = data.Detailes.Comfort,
                    Beauty = data.Detailes.Beauty,
                    Joy = data.Detailes.Joy,
                    Energy = data.Detailes.Energy,
                    DrugsDesire = data.Detailes.DrugsDesire,
                    SurroundingBeauty = data.Detailes.SurroundingBeauty,
                    FreshAir = data.Detailes.FreshAir,
                    ColonistWorkInfo = data.Detailes.WorkInfo,
                    MoodInfo = data.Detailes.MoodInfo,
                    PoliciesInfo = data.Detailes.PoliciesInfo,
                    ColonistMedicalInfo = data.Detailes.MedicalInfo,
                    SocialInfo = data.Detailes.SocialInfo,
                });
            }
            return ApiResult<List<ApiV1PawnDetailedDto>>.Ok(result);
        }

        public ApiResult<ApiV1PawnDetailedDto> GetColonistDetailedV1(int pawnId)
        {
            try
            {
                PawnDetailedRequestDto data = ColonistsHelper.GetSingleColonistDetailedDto(pawnId);

                ApiV1PawnDetailedDto result = new ApiV1PawnDetailedDto
                {
                    Colonist = data.Pawn,
                    BodySize = data.Detailes.BodySize,
                    Sleep = data.Detailes.Sleep,
                    Comfort = data.Detailes.Comfort,
                    Beauty = data.Detailes.Beauty,
                    Joy = data.Detailes.Joy,
                    Energy = data.Detailes.Energy,
                    DrugsDesire = data.Detailes.DrugsDesire,
                    SurroundingBeauty = data.Detailes.SurroundingBeauty,
                    FreshAir = data.Detailes.FreshAir,
                    ColonistWorkInfo = data.Detailes.WorkInfo,
                    MoodInfo = data.Detailes.MoodInfo,
                    PoliciesInfo = data.Detailes.PoliciesInfo,
                    ColonistMedicalInfo = data.Detailes.MedicalInfo,
                    SocialInfo = data.Detailes.SocialInfo,
                };

                if (result == null)
                {
                    return ApiResult<ApiV1PawnDetailedDto>.Fail($"Failed to find pawn with id: {pawnId}");
                }
                return ApiResult<ApiV1PawnDetailedDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<ApiV1PawnDetailedDto>
                    .Fail($"Failed to find pawn with id: {pawnId} - {ex.Message}");
            }
        }
    }

}