using System;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    public class PawnJobService : IPawnJobService
    {
        public PawnJobService() { }

        public ApiResult AssignJob(PawnJobRequestDto request)
        {
            try
            {
                Pawn pawn = PawnHelper.FindPawnById(request.PawnId);
                if (pawn == null)
                {
                    return ApiResult.Fail($"Pawn not found: {request.PawnId}");
                }

                JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail(request.JobDef);
                if (jobDef == null)
                {
                    return ApiResult.Fail($"JobDef not found: {request.JobDef}");
                }

                LocalTargetInfo target = LocalTargetInfo.Invalid;

                if (request.TargetThingId.HasValue)
                {
                    Thing thing = null;
                    foreach (Map map in Find.Maps)
                    {
                        thing = map.listerThings.AllThings
                            .FirstOrDefault(t => t.thingIDNumber == request.TargetThingId.Value);
                        if (thing != null) break;
                    }
                    if (thing == null)
                    {
                        return ApiResult.Fail($"Target thing not found: {request.TargetThingId}");
                    }
                    target = thing;
                }
                else if (request.TargetPosition != null)
                {
                    target = new IntVec3(
                        request.TargetPosition.X, 0, request.TargetPosition.Z);
                }

                bool success = PawnHelper.AssignJob(pawn, jobDef, target);
                if (!success)
                {
                    return ApiResult.Fail($"Pawn {request.PawnId} could not accept job {request.JobDef}");
                }
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        // The Strip order (fox ticket #4316, parity wave 7): the game's own
        // float menu entry, with its two gates. The job itself is the game's
        // JobDriver_Strip; what a player cannot strip is refused here with the
        // designator's reason instead of failing silently in the driver.
        public ApiResult Strip(StripRequestDto request)
        {
            try
            {
                Pawn pawn = PawnHelper.FindPawnById(request.PawnId);
                if (pawn == null)
                {
                    return ApiResult.Fail($"Pawn not found: {request.PawnId}");
                }

                Thing target = null;
                foreach (Map map in Find.Maps)
                {
                    target = map.listerThings.AllThings.FirstOrDefault(
                        t => t.thingIDNumber == request.TargetThingId
                    );
                    if (target != null)
                    {
                        break;
                    }
                }
                if (target == null)
                {
                    return ApiResult.Fail($"Target thing not found: {request.TargetThingId}");
                }

                if (!StrippableUtility.CanBeStrippedByColony(target))
                {
                    return ApiResult.Fail(
                        $"The game does not let the colony strip {target.LabelShort} "
                            + $"({target.def.defName}): it is not a corpse or a downed prisoner of the colony."
                    );
                }

                Pawn targetPawn = target as Pawn;
                if (targetPawn != null && targetPawn.HasExtraHomeFaction())
                {
                    return ApiResult.Fail(
                        $"{targetPawn.LabelShort} is quest-related, and the game's own Strip order refuses it."
                    );
                }

                if (!pawn.CanReach(target, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    return ApiResult.Fail(
                        $"CannotStrip: {pawn.LabelShort} cannot reach {target.LabelShort} (NoPath)."
                    );
                }

                target.SetForbidden(false, false);
                bool accepted = pawn.jobs.TryTakeOrderedJob(
                    JobMaker.MakeJob(JobDefOf.Strip, target),
                    JobTag.Misc
                );
                if (!accepted)
                {
                    return ApiResult.Fail(
                        $"{pawn.LabelShort} did not take the Strip job (a mental state or another order holds him)."
                    );
                }

                StrippableUtility.CheckSendStrippingImpactsGoodwillMessage(target);
                return ApiResult.Ok(
                    new
                    {
                        pawn_id = pawn.thingIDNumber,
                        target_thing_id = target.thingIDNumber,
                        target_label = target.LabelShort,
                    }
                );
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult AssignTendJob(MedicalTendRequestDto request)
        {
            try
            {
                Pawn patient = PawnHelper.FindPawnById(request.PatientPawnId);
                if (patient == null)
                {
                    return ApiResult.Fail($"Patient pawn not found: {request.PatientPawnId}");
                }

                Pawn doctor;
                if (request.DoctorPawnId.HasValue)
                {
                    doctor = PawnHelper.FindPawnById(request.DoctorPawnId.Value);
                    if (doctor == null)
                    {
                        return ApiResult.Fail($"Doctor pawn not found: {request.DoctorPawnId}");
                    }
                }
                else
                {
                    // Find the best available doctor on the same map
                    doctor = patient.Map?.mapPawns.FreeColonists
                        .Where(p => p != patient && !p.Downed && !p.Dead)
                        .OrderByDescending(p => p.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0)
                        .FirstOrDefault();

                    if (doctor == null)
                    {
                        return ApiResult.Fail("No available doctor found on the map");
                    }
                }

                bool success = PawnHelper.AssignTendJob(doctor, patient);
                if (!success)
                {
                    return ApiResult.Fail("Doctor could not accept tend job");
                }
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }

        public ApiResult AssignBedRest(MedicalBedRestRequestDto request)
        {
            try
            {
                Pawn patient = PawnHelper.FindPawnById(request.PatientPawnId);
                if (patient == null)
                {
                    return ApiResult.Fail($"Patient pawn not found: {request.PatientPawnId}");
                }

                Building_Bed bed = null;
                if (request.BedBuildingId.HasValue)
                {
                    int bedId = request.BedBuildingId.Value;
                    Building building = BuildingHelper.FindBuildingByID(bedId);
                    bed = building as Building_Bed;
                    if (bed == null)
                    {
                        // An id from the stored read can name a bed packed up in
                        // a stockpile or a thing inside a container, so name it
                        // instead of a bare "not found" (ticket #3354).
                        return ApiResult.Fail(
                            UnbuiltBuildingReason.OrNotFound(
                                ThingHolderHelper.DescribeUnbuilt(
                                    bedId,
                                    "only a built bed on the map can take a patient"),
                                $"Bed not found: {bedId}"));
                    }
                }

                bool success = PawnHelper.AssignBedRest(patient, bed);
                if (!success)
                {
                    return ApiResult.Fail("Patient could not be assigned to bed rest");
                }
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }
    }
}
