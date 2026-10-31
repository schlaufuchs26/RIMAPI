namespace RIMAPI.Helpers
{
    // The rules behind carrying and feeding a patient (fox ticket #3519): a
    // downed colony animal could neither be carried to a bed nor fed, because
    // both levers were written for humanlikes. They stay free of game types so
    // Source/RIMAPI.Tests can state them; the game calls live in
    // ColonistService.Rescue and ColonistService.Feed.
    public static class PatientRules
    {
        // The game's own feed work givers, from Core/Defs/WorkGiverDefs/
        // WorkGivers.xml.
        public const string FeedPrisonerGiver = "FeedPrisoner";
        public const string FeedHumanlikeGiver = "DoctorFeedHumanlikes";
        public const string FeedAnimalGiver = "DoctorFeedAnimals";

        // FeedGiver names the work giver the game hands a feed job out through:
        // a prisoner is fed by the warden, a humanlike patient by the doctor's
        // humanlike feeder, an animal by the doctor's animal feeder. The mod
        // used WorkGiver_Warden_Feed for every patient, and that one only ever
        // feeds prisoners (ShouldTakeCareOfPrisoner), so a colonist and an
        // animal were refused by a check that was never about them.
        public static string FeedGiver(bool prisoner, bool humanlike)
        {
            if (prisoner) return FeedPrisonerGiver;
            return humanlike ? FeedHumanlikeGiver : FeedAnimalGiver;
        }

        // BedKindRefusal says why a bed's own kind cannot take this patient:
        // RestUtility.CanUseBedEver refuses a humanlike pawn a bed marked for
        // animals and an animal a humanlike bed, so an animal needs an animal
        // bed or an animal sleeping spot. Null when the kinds match.
        public static string BedKindRefusal(bool patientHumanlike, bool bedHumanlike)
        {
            if (patientHumanlike == bedHumanlike) return null;
            if (patientHumanlike)
            {
                return "it is an animal bed, and a humanlike patient needs a human bed";
            }
            return "it is a humanlike bed, and an animal is only carried into an animal bed or an "
                + "animal sleeping spot (`build name=AnimalSleepingSpot`)";
        }

        // NoBedHint is the clause a "no free bed" refusal carries, so a run
        // knows which building to place: an animal has no bed at all until the
        // colony builds one, because a humanlike bed never takes it.
        public static string NoBedHint(bool humanlike)
        {
            return humanlike
                ? "build one (`build name=Bed`) or free one up"
                : "build one for animals (`build name=AnimalSleepingSpot`, free and no material) or "
                    + "free an animal bed up";
        }

        // FeedBedHint is the clause a "not in a bed" feed refusal carries: the
        // game's FeedPatient job needs a bed the patient may lie in, which is a
        // different building for an animal than for a humanlike patient.
        public static string FeedBedHint(bool humanlike)
        {
            return humanlike
                ? "build one (`build name=Bed`) and rescue him into it first"
                : "an animal is only fed in an animal bed or an animal sleeping spot "
                    + "(`build name=AnimalSleepingSpot`), so `rescue` it into one first";
        }
    }
}
