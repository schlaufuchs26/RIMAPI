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
    // Rescue is the other way a downed pawn joins the colony (ticket #3183): a
    // transport pod crashed beside colony 8 and left a badly wounded neutral who
    // bled out while the tool could only watch. The game's own rescue job
    // carries a downed non-enemy to a free bed, and a rescued pod survivor
    // becomes a guest the colony can then recruit; a downed colonist is carried
    // to a bed the same way. The work giver itself decides, so its own checks
    // (reservation, reach, the carry cooldown after being downed) apply instead
    // of a second copy of them.
    //
    // A bedId names the bed instead (fox ticket #3519): a downed colony animal
    // is only carried into an animal bed or an animal sleeping spot, and the
    // game's own lookup picks the nearest free one, which may be nowhere near
    // the base. With a named bed the game's bed rules still decide (kind, size,
    // sleeping slot, reach), and a refusal says which one failed.
    public partial class ColonistService
    {
        public ApiResult MakeJobRescue(int mapId, int pawnId, int rescuerId, int bedId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult.Fail($"Map with ID={mapId} not found");
                Pawn target = PawnHelper.FindPawnForOrder(map, pawnId);
                if (target == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                // A carried pawn is despawned into his carrier's hands, so he
                // resolves here but no rescuer can reach him yet (fox #3665).
                string carried = CombatRefusal.CarriedRefusal(Describe(target), target.CarriedBy?.LabelShort, "rescue");
                if (carried != null) return ApiResult.Fail(carried);

                if (target.Dead) return ApiResult.Fail($"{Describe(target)} is dead");
                if (target.InBed()) return ApiResult.Fail($"{Describe(target)} already lies in a bed");
                if (!target.Downed)
                {
                    return ApiResult.Fail(
                        $"{Describe(target)} is not downed: rescue carries a pawn who cannot walk, so nothing "
                        + "needs doing for a standing one");
                }
                if (target.HostileTo(Faction.OfPlayer))
                {
                    return ApiResult.Fail(
                        $"{Describe(target)} is hostile to the colony: rescue does not carry an enemy, "
                        + "`arrest` captures him into a prisoner bed instead");
                }

                Pawn rescuer = rescuerId > 0 ? PawnHelper.FindPawnForOrder(map, rescuerId) : PickRescuer(map, target);
                if (rescuer == null)
                {
                    return ApiResult.Fail(
                        $"no colonist can carry {Describe(target)} right now: everyone is downed, in a mental "
                        + "state or cannot reach the pawn");
                }
                if (rescuer == target) return ApiResult.Fail($"Pawn {pawnId} cannot rescue itself");
                // The game's IsColonistPlayerControlled is false for a colonist
                // in a mental state or a downed one, so the membership test alone
                // refused a colonist the colony plainly has with "not one of the
                // colony's colonists" (ticket #3469); the state is named instead.
                string refusal = CombatRefusal.ActorRefusal(
                    Describe(rescuer), "rescue", PawnHelper.RefusalStateOf(rescuer));
                if (refusal != null) return ApiResult.Fail(refusal);

                Job job;
                Building_Bed bed;
                Building_Bed rejected = null;
                float rejectedTempC = 0f;
                bool rejectedFire = false;
                if (bedId > 0)
                {
                    // The caller named the bed: its choice stands, and only the
                    // game's own bed rules can refuse it.
                    Thing thing = MapHelper.GetThingOnMapById(mapId, bedId);
                    if (thing == null) return ApiResult.Fail($"Thing with ID={bedId} not found on this map");
                    bed = thing as Building_Bed;
                    if (bed == null)
                    {
                        return ApiResult.Fail(
                            $"{thing.LabelShort} (id {bedId}) is no bed: a rescue carries the pawn into a bed, "
                            + "so name a bed or an animal sleeping spot (the `things` read prints its id)");
                    }
                    string bedRefusal = BedRefusal(map, target, rescuer, bed);
                    if (bedRefusal != null) return ApiResult.Fail(bedRefusal);
                    job = JobMaker.MakeJob(JobDefOf.Rescue, target, bed);
                    job.count = 1;
                }
                else
                {
                    WorkGiver_RescueDowned giver = new WorkGiver_RescueDowned();
                    if (!giver.HasJobOnThing(rescuer, target, true))
                    {
                        return ApiResult.Fail(
                            $"{Describe(target)} cannot be rescued right now: {RescueRefusal(map, target, rescuer)}");
                    }
                    job = giver.JobOnThing(rescuer, target, true);
                    if (job == null)
                    {
                        return ApiResult.Fail(
                            $"{Describe(target)} cannot be rescued right now: the game's rescue order came back "
                            + "empty, so no bed is free for him");
                    }
                    // The giver's bed is the first free one it finds, not the
                    // safest one: in colony 10 that was the bed inside a room at
                    // 111 C, so the patient was carried back into the fire
                    // (ticket #3383). A bed in a burning or oven-hot room is
                    // replaced by a safe free one when the colony has any; the
                    // answer carries the room temperature either way, so a
                    // caller sees a bed to refuse.
                    bed = job.targetB.Thing as Building_Bed;
                    if (bed != null && BedSafety.IsUnsafe(bed.AmbientTemperature, BedPick.RoomHasFire(map, bed)))
                    {
                        Building_Bed safer = BedPick.SaferBed(map, target, rescuer, bed, forPrisoners: false);
                        if (safer != null)
                        {
                            rejected = bed;
                            rejectedTempC = bed.AmbientTemperature;
                            rejectedFire = BedPick.RoomHasFire(map, bed);
                            job = JobMaker.MakeJob(JobDefOf.Rescue, target, safer);
                            job.count = 1;
                            bed = safer;
                        }
                    }
                }

                if (!rescuer.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    return ApiResult.Fail($"Failed to assign the rescue job to {Describe(rescuer)}");
                }

                RescueResultDto result = new RescueResultDto
                {
                    PawnId = pawnId,
                    Name = target.LabelShort,
                    RescuerId = rescuer.thingIDNumber,
                    Rescuer = rescuer.LabelShort,
                    Job = job.def.defName,
                    Guest = BecomesGuest(job, target),
                };
                if (bed != null)
                {
                    Room room = BedPick.BedRoom(bed);
                    result.BedId = bed.thingIDNumber;
                    result.Bed = bed.Label;
                    result.BedForPrisoners = bed.ForPrisoners;
                    result.BedRoomTempC = bed.AmbientTemperature;
                    result.BedRoomHasFire = BedPick.RoomHasFire(map, bed);
                    result.BedOutdoors = BedPick.Outdoors(map, bed);
                    if (room != null)
                    {
                        result.BedRoomId = room.ID;
                        result.BedRoomLabel = BedPick.RoomLabel(room);
                    }
                }
                if (rejected != null)
                {
                    result.RejectedBedId = rejected.thingIDNumber;
                    result.RejectedBed = rejected.Label;
                    Room rejectedRoom = BedPick.BedRoom(rejected);
                    if (rejectedRoom != null) result.RejectedBedRoomLabel = rejectedRoom.GetRoomRoleLabel();
                    result.RejectedBedTempC = rejectedTempC;
                    result.RejectedBedRoomHasFire = rejectedFire;
                }
                return ApiResult.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.ToString());
            }
        }

        // BecomesGuest is the game's own answer, not a second rule: the rescue
        // job's finish (JobDriver_TakeToBed.CheckMakeTakeeGuest) makes the pawn
        // a guest of the colony unless it already belongs to the colony, is a
        // wild man or a baby, or has no guest tracker at all (an animal). The
        // old check asked whether the pawn was already a guest, and a downed
        // pod survivor has no HostFaction yet, so the answer for the very case
        // rescue exists for was "stays a colonist" (ticket #3244).
        private static bool BecomesGuest(Job job, Pawn target)
        {
            return !job.def.makeTargetPrisoner
                && target.Faction != Faction.OfPlayer
                && target.HostFaction != Faction.OfPlayer
                && target.guest != null
                && !target.IsWildMan()
                && target.DevelopmentalStage != DevelopmentalStage.Baby;
        }

        // PickRescuer takes the nearest colonist who could carry the pawn and
        // reach it. A colonist whose Doctor work is on comes first, the way the
        // game offers the job, but any able colonist can take an ordered job.
        private static Pawn PickRescuer(Map map, Pawn target)
        {
            return map.mapPawns.FreeColonistsSpawned
                .Where(p => !p.Downed && !p.InMentalState && p != target)
                .Where(p => p.CanReach(target, PathEndMode.OnCell, Danger.Deadly))
                .OrderByDescending(p => p.workSettings != null && p.workSettings.WorkIsActive(WorkTypeDefOf.Doctor))
                .ThenBy(p => (p.Position - target.Position).LengthHorizontalSquared)
                .FirstOrDefault();
        }

        // RescueRefusal says which of the game's rescue conditions failed, in
        // the game's own terms, because HasJobOnThing answers a bare false.
        private static string RescueRefusal(Map map, Pawn target, Pawn rescuer)
        {
            if (!rescuer.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true))
            {
                return $"{Describe(rescuer)} cannot reserve or reach him (another pawn has him reserved, or the "
                    + "path is blocked)";
            }
            // The same lookup the game's own rescue work giver makes
            // (WorkGiver_TakeToBed.FindBed), so the refusal names the bed rule
            // that actually applies: an animal's own guest status is none, and
            // forcing GuestStatus.Guest asked for a guest bed.
            Building_Bed bed = RestUtility.FindBedFor(target, rescuer, false, false, target.GuestStatus)
                ?? RestUtility.FindBedFor(target, rescuer, false, true, target.GuestStatus);
            if (bed == null)
            {
                return "no free bed takes him: a rescue carries him to a bed, so "
                    + PatientRules.NoBedHint(target.RaceProps.Humanlike);
            }
            return "the game's own rescue check refuses him";
        }

        // BedRefusal says why the bed a caller named cannot take the carried
        // pawn, in the game's own terms. RestUtility.IsValidBedFor answers a
        // bare false, so the checks a run can act on are named first: the
        // bed's kind (a humanlike bed never takes an animal), the animal's body
        // size against the bed's, a prisoner setting, and reach.
        private static string BedRefusal(Map map, Pawn target, Pawn rescuer, Building_Bed bed)
        {
            string where = $"{bed.Label} (id {bed.thingIDNumber})";
            if (!bed.Spawned || bed.Map != map) return $"{where} does not stand on this map";
            if (bed.IsBurning()) return $"{where} is on fire";
            string kind = PatientRules.BedKindRefusal(target.RaceProps.Humanlike, bed.def.building.bed_humanlike);
            if (kind != null) return $"{where} cannot take {Describe(target)}: {kind}";
            if (target.BodySize > bed.def.building.bed_maxBodySize)
            {
                return $"{where} is too small for {Describe(target)}: body size {target.BodySize:0.#} against "
                    + $"the bed's {bed.def.building.bed_maxBodySize:0.#}";
            }
            if (bed.ForPrisoners && !target.IsPrisonerOfColony)
            {
                return $"{where} is set for prisoners, and {Describe(target)} is none (`bed` with bed_for=colonists "
                    + "clears that)";
            }
            if (!rescuer.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                return $"{Describe(rescuer)} cannot reach {where}";
            }
            if (!RestUtility.IsValidBedFor(bed, target, rescuer, checkSocialProperness: false,
                    guestStatus: target.GuestStatus))
            {
                return $"{Describe(target)} cannot be carried to {where}: the game's own bed check refuses it "
                    + "(every sleeping slot is taken, the bed is forbidden, or another pawn has it reserved)";
            }
            return null;
        }
    }
}
