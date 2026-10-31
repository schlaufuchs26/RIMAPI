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
    public partial class ColonistService
    {
        public ApiResult MakeJobRescue(int mapId, int pawnId, int rescuerId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult.Fail($"Map with ID={mapId} not found");
                Pawn target = FindPawnOnMap(map, pawnId);
                if (target == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");

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

                Pawn rescuer = rescuerId > 0 ? FindPawnOnMap(map, rescuerId) : PickRescuer(map, target);
                if (rescuer == null)
                {
                    return ApiResult.Fail(
                        $"no colonist can carry {Describe(target)} right now: everyone is downed, in a mental "
                        + "state or cannot reach the pawn");
                }
                if (rescuer == target) return ApiResult.Fail($"Pawn {pawnId} cannot rescue itself");
                if (rescuer.Faction != Faction.OfPlayer || !rescuer.IsColonistPlayerControlled)
                {
                    return ApiResult.Fail($"{Describe(rescuer)} is not one of the colony's colonists");
                }

                WorkGiver_RescueDowned giver = new WorkGiver_RescueDowned();
                if (!giver.HasJobOnThing(rescuer, target, true))
                {
                    return ApiResult.Fail(
                        $"{Describe(target)} cannot be rescued right now: {RescueRefusal(map, target, rescuer)}");
                }
                Job job = giver.JobOnThing(rescuer, target, true);
                if (job == null)
                {
                    return ApiResult.Fail(
                        $"{Describe(target)} cannot be rescued right now: the game's rescue order came back "
                        + "empty, so no bed is free for him");
                }
                if (!rescuer.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    return ApiResult.Fail($"Failed to assign the rescue job to {Describe(rescuer)}");
                }

                Building_Bed bed = job.targetB.Thing as Building_Bed;
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
                    result.BedId = bed.thingIDNumber;
                    result.Bed = bed.Label;
                    result.BedForPrisoners = bed.ForPrisoners;
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
            Building_Bed bed = RestUtility.FindBedFor(target, rescuer, false, false, GuestStatus.Guest)
                ?? RestUtility.FindBedFor(target, rescuer, false, true, GuestStatus.Guest);
            if (bed == null)
            {
                return "no free bed takes him: a rescue carries him to a bed, so build one "
                    + "(`build name=Bed`) or free one up";
            }
            return "the game's own rescue check refuses him";
        }
    }
}
