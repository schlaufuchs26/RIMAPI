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
    // Feeding a patient is Warden work in the game and it has exactly one
    // shape: WorkGiver_Warden_Feed hands out JobDefOf.FeedPatient, and that job
    // needs the patient in a bed. Colony 12's Diver lay downed on the open
    // ground for nine game hours with 51 meals in the store, because no job can
    // feed a pawn who does not lie in a bed and nothing said so (ticket #3445).
    public partial class ColonistService
    {
        public ApiResult MakeJobFeed(int mapId, int pawnId, int feederId, int mealId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult.Fail($"Map with ID={mapId} not found");
                Pawn patient = FindPawnOnMap(map, pawnId);
                if (patient == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                if (patient.Dead) return ApiResult.Fail($"{Describe(patient)} is dead");
                if (!patient.Downed)
                {
                    return ApiResult.Fail(
                        $"{Describe(patient)} is not downed: feeding a patient is the game's FeedPatient job, "
                        + "and a pawn who can walk eats on his own");
                }
                if (!patient.InBed())
                {
                    return ApiResult.Fail(
                        $"{Describe(patient)} is not in a bed: the game's FeedPatient job only runs for a patient "
                        + "in a bed, so build one (`build name=Bed`) and rescue him into it first");
                }
                if (!FeedPatientUtility.ShouldBeFed(patient))
                {
                    return ApiResult.Fail(
                        $"{Describe(patient)} is not hungry: the game hands out a feed job only while "
                        + "malnutrition is on the patient");
                }

                Pawn feeder = feederId > 0 ? FindPawnOnMap(map, feederId) : PickFeeder(map, patient);
                if (feeder == null)
                {
                    return ApiResult.Fail(
                        $"no colonist can feed {Describe(patient)} right now: everyone is downed, in a mental "
                        + "state or cannot reach the patient");
                }
                if (feeder == patient) return ApiResult.Fail($"Pawn {pawnId} cannot feed itself");
                if (feeder.Faction != Faction.OfPlayer || !feeder.IsColonistPlayerControlled)
                {
                    return ApiResult.Fail($"{Describe(feeder)} is not one of the colony's colonists");
                }

                Job job;
                if (mealId > 0)
                {
                    // A named meal is the caller's choice, so it is checked here
                    // instead of letting the work giver pick a different stack.
                    Thing meal = MapHelper.GetThingOnMapById(mapId, mealId);
                    if (meal == null) return ApiResult.Fail($"Thing with ID={mealId} not found on this map");
                    if (!meal.def.IsNutritionGivingIngestible)
                    {
                        return ApiResult.Fail($"{meal.LabelShort} (id {mealId}) is not food");
                    }
                    if (!patient.RaceProps.CanEverEat(meal))
                    {
                        return ApiResult.Fail(
                            $"{Describe(patient)} cannot eat {meal.LabelShort} (id {mealId}): the meal does not "
                            + "fit his diet");
                    }
                    if (!feeder.CanReserveAndReach(meal, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        return ApiResult.Fail(
                            $"{Describe(feeder)} cannot reach {meal.LabelShort} (id {mealId})");
                    }
                    job = JobMaker.MakeJob(JobDefOf.FeedPatient, patient, meal);
                    job.count = 1;
                }
                else
                {
                    WorkGiver_Warden_Feed giver = new WorkGiver_Warden_Feed();
                    if (!giver.HasJobOnThing(feeder, patient, true))
                    {
                        return ApiResult.Fail(
                            $"{Describe(patient)} cannot be fed right now: {FeedRefusal(patient, feeder)}");
                    }
                    job = giver.JobOnThing(feeder, patient, true);
                    if (job == null)
                    {
                        return ApiResult.Fail(
                            $"{Describe(patient)} cannot be fed right now: no meal the patient may eat is in reach");
                    }
                }

                if (!feeder.jobs.TryTakeOrderedJob(job, JobTag.Misc))
                {
                    return ApiResult.Fail($"Failed to assign the feed job to {Describe(feeder)}");
                }

                FeedResultDto result = new FeedResultDto
                {
                    PawnId = pawnId,
                    Name = patient.LabelShort,
                    FeederId = feeder.thingIDNumber,
                    Feeder = feeder.LabelShort,
                    Job = job.def.defName,
                    Queued = true,
                };
                if (job.targetB.HasThing)
                {
                    result.MealId = job.targetB.Thing.thingIDNumber;
                    result.Meal = job.targetB.Thing.LabelShort;
                }
                Building_Bed bed = patient.CurrentBed();
                if (bed != null)
                {
                    result.BedId = bed.thingIDNumber;
                    result.Bed = bed.Label;
                }
                return ApiResult.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.ToString());
            }
        }

        // PickFeeder takes the nearest colonist who could feed the patient and
        // reach him. A colonist whose Warden work is on comes first, the way the
        // game offers the job, but any able colonist can take an ordered job.
        private static Pawn PickFeeder(Map map, Pawn patient)
        {
            return map.mapPawns.FreeColonistsSpawned
                .Where(p => !p.Downed && !p.InMentalState && p != patient)
                .Where(p => p.CanReach(patient, PathEndMode.ClosestTouch, Danger.Deadly))
                .OrderByDescending(p => p.workSettings != null && p.workSettings.WorkIsActive(WorkTypeDefOf.Warden))
                .ThenBy(p => (p.Position - patient.Position).LengthHorizontalSquared)
                .FirstOrDefault();
        }

        // FeedRefusal says which of the game's feed conditions failed, in the
        // game's own terms, because HasJobOnThing answers a bare false. The bed
        // is checked before this, so it never has to be the answer here.
        private static string FeedRefusal(Pawn patient, Pawn feeder)
        {
            if (!feeder.CanReserveAndReach(patient, PathEndMode.ClosestTouch, Danger.Deadly, 1, -1, null, true))
            {
                return $"{Describe(feeder)} cannot reserve or reach him (another pawn has him reserved, or the "
                    + "path is blocked)";
            }
            return "no meal the patient may eat is in reach: cook one (`bill`), allow a forbidden stack "
                + "(`allow`), or store meals inside the home area";
        }
    }
}
