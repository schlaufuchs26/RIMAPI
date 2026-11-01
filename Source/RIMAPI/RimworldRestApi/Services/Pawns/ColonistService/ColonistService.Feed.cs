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
                Map map = MapLookup.Resolve(mapId, MapHelper.GetMapByID, out string mapError);
                if (map == null) return ApiResult.Fail(mapError);
                Pawn patient = PawnHelper.FindPawnForOrder(map, pawnId);
                if (patient == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found");
                // A carried patient is despawned into the carrier's hands, so the
                // old lookup answered "not found" for him (fox #3719).
                string carried = CombatRefusal.CarriedRefusal(
                    Describe(patient), patient.CarriedBy?.LabelShort, "feed");
                if (carried != null) return ApiResult.Fail(carried);
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
                        + "in a bed, and " + PatientRules.FeedBedHint(patient.RaceProps.Humanlike));
                }
                if (!FeedPatientUtility.ShouldBeFed(patient))
                {
                    return ApiResult.Fail(
                        $"{Describe(patient)} does not need feeding right now: the game hands out a feed job only "
                        + "for a patient who lies in a bed for medical rest");
                }
                // The gate behind ticket #3680: the patient had just eaten, so
                // the game refused the job, and the old answer blamed a missing
                // meal while 16 meals sat in the colony's own stockpile.
                if (patient.DevelopmentalStage.Baby())
                {
                    return ApiResult.Fail(
                        $"{Describe(patient)} is a baby: the game feeds a baby at the breast, not with a feed job");
                }
                if (!FeedPatientUtility.IsHungry(patient))
                {
                    int stacks, meals;
                    CountStoredMeals(map, patient, out stacks, out meals);
                    // IsHungry allows the 2-point band above the hungry
                    // threshold, so the answer names the same boundary the game
                    // applies.
                    return ApiResult.Fail(FeedRules.NotHungry(Describe(patient),
                        patient.needs.food.CurLevelPercentage * 100f,
                        (patient.needs.food.PercentageThreshHungry + 0.02f) * 100f, meals, stacks));
                }

                Pawn feeder = feederId > 0 ? PawnHelper.FindPawnForOrder(map, feederId) : PickFeeder(map, patient);
                if (feeder == null)
                {
                    return ApiResult.Fail(
                        $"no colonist can feed {Describe(patient)} right now: everyone is downed, in a mental "
                        + "state or cannot reach the patient");
                }
                if (feeder == patient) return ApiResult.Fail($"Pawn {pawnId} cannot feed itself");
                // IsColonistPlayerControlled is false for a colonist in a mental
                // state or a downed one, so the membership test alone called him
                // a foreigner (ticket #3469); the state is named instead.
                string refusal = CombatRefusal.ActorRefusal(
                    Describe(feeder), "feed", PawnHelper.RefusalStateOf(feeder));
                if (refusal != null) return ApiResult.Fail(refusal);

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
                    // TargetA is the food and targetB the patient: that is the
                    // order JobDriver_FoodFeedPatient reads (the game's own
                    // givers build it this way), and the old call had them the
                    // other way round, so a named meal fed the meal to nobody.
                    job = FeedJob(meal, patient, 1);
                }
                else
                {
                    // The giver is the game's own for this patient's kind. The
                    // old code always built WorkGiver_Warden_Feed, which only
                    // ever feeds prisoners, so a colonist and an animal were
                    // refused by ShouldTakeCareOfPrisoner (fox ticket #3519).
                    string giverName = PatientRules.FeedGiver(
                        patient.IsPrisonerOfColony || patient.IsSlaveOfColony, patient.RaceProps.Humanlike);
                    WorkGiverDef giverDef = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverName);
                    if (giverDef == null)
                    {
                        return ApiResult.Fail(
                            $"the game has no feed work giver \"{giverName}\": a mod renamed or removed it, so no "
                            + "colonist can be sent to feed this patient");
                    }
                    // Every feed giver is a scanner (HasJobOnThing/JobOnThing);
                    // the def's Worker is what carries its def, so the def's own
                    // feedAnimalsOnly / feedHumanlikesOnly flags apply.
                    WorkGiver_Scanner giver = giverDef.Worker as WorkGiver_Scanner;
                    if (giver == null)
                    {
                        return ApiResult.Fail(
                            $"the game's feed work giver \"{giverName}\" is no scanner, so it cannot be asked for "
                            + "a job");
                    }
                    job = giver.HasJobOnThing(feeder, patient, true)
                        ? giver.JobOnThing(feeder, patient, true)
                        : null;
                    if (job == null)
                    {
                        // Every rule we can name passed, so the giver's own food
                        // search is what came up empty. Find the stack here and
                        // say which ones were looked at instead of sending the
                        // run off to cook food that already exists (#3680).
                        MealScan scan = ScanMeals(map, feeder, patient);
                        Thing meal = scan.Best(patient, feeder);
                        if (meal == null)
                        {
                            return ApiResult.Fail($"{Describe(patient)} cannot be fed right now: "
                                + FeedRules.NoMealRefusal(scan.Refused));
                        }
                        job = FeedJob(meal, patient,
                            FoodUtility.WillIngestStackCountOf(patient, meal.def,
                                FoodUtility.GetNutrition(patient, meal, meal.def)));
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
                // targetA is the meal and targetB the patient in the game's own
                // job, so the meal has to be read from targetA; the old block
                // reported the patient's name and id as the meal.
                if (job.targetA.HasThing)
                {
                    result.MealId = job.targetA.Thing.thingIDNumber;
                    result.Meal = job.targetA.Thing.LabelShort;
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

        // FeedJob is the game's own job shape: targetA the food, targetB the
        // patient, which is the order JobDriver_FoodFeedPatient reads. count is
        // how much of the stack the feeder carries; the driver caps it itself.
        private static Job FeedJob(Thing meal, Pawn patient, int count)
        {
            Job job = JobMaker.MakeJob(JobDefOf.FeedPatient, meal, patient);
            job.count = count;
            return job;
        }

        // PickFeeder takes the nearest colonist who could feed the patient and
        // reach him. A colonist whose own work type is on comes first; Warden
        // for a prisoner, Doctor for a colonist or an animal, which is where
        // the game's feed givers sit; but any able colonist can take an
        // ordered job.
        private static Pawn PickFeeder(Map map, Pawn patient)
        {
            WorkTypeDef workType = patient.IsPrisonerOfColony || patient.IsSlaveOfColony
                ? WorkTypeDefOf.Warden
                : WorkTypeDefOf.Doctor;
            return map.mapPawns.FreeColonistsSpawned
                .Where(p => !p.Downed && !p.InMentalState && p != patient)
                .Where(p => p.CanReach(patient, PathEndMode.ClosestTouch, Danger.Deadly))
                .OrderByDescending(p => p.workSettings != null && p.workSettings.WorkIsActive(workType))
                .ThenBy(p => (p.Position - patient.Position).LengthHorizontalSquared)
                .FirstOrDefault();
        }

        // CountStoredMeals counts the proper meals the patient may eat, for the
        // "not hungry" answer: it tells the run the food exists, so there is
        // nothing to cook (ticket #3680).
        private static void CountStoredMeals(Map map, Pawn patient, out int stacks, out int count)
        {
            stacks = 0;
            count = 0;
            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree))
            {
                if (!t.Spawned || t.def.ingestible == null) continue;
                if (t.def.ingestible.preferability < FoodPreferability.MealAwful) continue;
                if (t.IsForbidden(patient) || t.IsNotFresh() || t.IsDessicated()) continue;
                if (!patient.WillEat(t, patient, careIfNotAcceptableForTitle: true, allowVenerated: true)) continue;
                stacks++;
                count += t.stackCount;
            }
        }

        // MealScan is the map's stored food split into what the patient may eat
        // and reach and what not, with the reason for each refusal, so a
        // refusal can name the stacks it considered (ticket #3680).
        private sealed class MealScan
        {
            public readonly List<Thing> Usable = new List<Thing>();
            public readonly List<MealRefusal> Refused = new List<MealRefusal>();

            public Thing Best(Pawn patient, Pawn feeder)
            {
                return Usable
                    .OrderByDescending(t => FoodUtility.FoodOptimality(patient, t, t.def,
                        (t.PositionHeld - feeder.Position).LengthHorizontal))
                    .FirstOrDefault();
            }
        }

        private static MealScan ScanMeals(Map map, Pawn feeder, Pawn patient)
        {
            MealScan scan = new MealScan();
            FoodPreferability minPref = MinPreferability(patient);
            var refused = new List<KeyValuePair<Thing, string>>();
            // The same thing group the game's own feed search walks: meals, raw
            // food and corpses, never plants (a feed job does not harvest).
            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSourceNotPlantOrTree))
            {
                if (!t.Spawned) continue;
                string reason = MealRefusalReason(t, feeder, patient, minPref);
                if (reason == null) scan.Usable.Add(t);
                else refused.Add(new KeyValuePair<Thing, string>(t, reason));
            }
            // Meals first: a run reads the first lines, and a forbidden meal
            // says more than a stack of raw rice.
            refused.Sort((a, b) => Preferability(b.Key).CompareTo(Preferability(a.Key)));
            foreach (KeyValuePair<Thing, string> pair in refused)
            {
                scan.Refused.Add(new MealRefusal(pair.Key.LabelShort, pair.Key.thingIDNumber, pair.Key.stackCount,
                    pair.Value));
            }
            return scan;
        }

        // MealRefusalReason states the first of the game's own food conditions
        // a stack fails, so the list explains the refusal instead of repeating
        // "no meal in reach". Null means the patient could be fed from it.
        private static string MealRefusalReason(Thing t, Pawn feeder, Pawn patient, FoodPreferability minPref)
        {
            if (!t.def.IsNutritionGivingIngestible) return "is not food";
            if (!patient.WillEat(t, feeder, careIfNotAcceptableForTitle: true, allowVenerated: true))
            {
                return "is ruled out by the patient's diet or food restriction";
            }
            if ((int)t.def.ingestible.preferability < (int)minPref)
            {
                return "is too unappetising while the patient is only hungry (raw food is for starving patients)";
            }
            if (!t.IngestibleNow) return "is not edible right now";
            if (t.IsNotFresh()) return "is rotten";
            if (t.IsDessicated()) return "is dried out";
            if (t.IsForbidden(feeder)) return "is forbidden";
            if (!feeder.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return "is out of reach or reserved by another pawn";
            }
            return null;
        }

        // MinPreferability mirrors the floor the game's food search applies: an
        // animal or wild man eats anything, a starving patient raw food, a
        // merely hungry one at least a bad meal.
        private static FoodPreferability MinPreferability(Pawn patient)
        {
            if (patient.NonHumanlikeOrWildMan()) return FoodPreferability.NeverForNutrition;
            if (patient.needs.food.CurCategory == HungerCategory.Starving) return FoodPreferability.DesperateOnly;
            FoodPreferability min = (int)patient.needs.food.CurCategory >= 2
                ? FoodPreferability.RawBad
                : FoodPreferability.MealAwful;
            if (min == FoodPreferability.MealAwful && patient.genes != null && patient.genes.DontMindRawFood)
            {
                min = FoodPreferability.RawBad;
            }
            return min;
        }

        private static int Preferability(Thing t)
        {
            return t.def.ingestible == null ? 0 : (int)t.def.ingestible.preferability;
        }
    }
}
