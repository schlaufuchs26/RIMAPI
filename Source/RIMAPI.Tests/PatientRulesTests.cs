using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3519: a downed colony animal could neither be carried to a bed nor
    // fed. Both refusals came from humanlike assumptions, so these state which
    // of the game's own feed givers and beds a patient needs.
    public class PatientRulesTests
    {
        [Fact]
        public void APrisonerIsFedByTheWarden()
        {
            Assert.Equal(PatientRules.FeedPrisonerGiver, PatientRules.FeedGiver(prisoner: true, humanlike: true));
            Assert.Equal(PatientRules.FeedPrisonerGiver, PatientRules.FeedGiver(prisoner: true, humanlike: false));
        }

        [Fact]
        public void AHumanlikePatientIsFedByTheDoctorsHumanlikeFeeder()
        {
            Assert.Equal(PatientRules.FeedHumanlikeGiver, PatientRules.FeedGiver(prisoner: false, humanlike: true));
        }

        // The one the animal case needs: the doctor's animal feeder, which is
        // the only giver whose def carries feedAnimalsOnly.
        [Fact]
        public void AnAnimalIsFedByTheDoctorsAnimalFeeder()
        {
            Assert.Equal(PatientRules.FeedAnimalGiver, PatientRules.FeedGiver(prisoner: false, humanlike: false));
        }

        [Fact]
        public void ABedOfThePatientsOwnKindIsNoRefusal()
        {
            Assert.Null(PatientRules.BedKindRefusal(patientHumanlike: true, bedHumanlike: true));
            Assert.Null(PatientRules.BedKindRefusal(patientHumanlike: false, bedHumanlike: false));
        }

        // The refusal the sheep hit: the free beds the run had built were
        // humanlike beds, and the game's CanUseBedEver refuses those to an
        // animal.
        [Fact]
        public void AHumanlikeBedRefusesAnAnimalAndNamesTheAnimalOne()
        {
            string why = PatientRules.BedKindRefusal(patientHumanlike: false, bedHumanlike: true);
            Assert.Contains("humanlike bed", why);
            Assert.Contains("AnimalSleepingSpot", why);
        }

        [Fact]
        public void AnAnimalBedRefusesAHumanlikePatient()
        {
            string why = PatientRules.BedKindRefusal(patientHumanlike: true, bedHumanlike: false);
            Assert.Contains("animal bed", why);
        }

        [Fact]
        public void TheNoBedHintNamesTheBuildingForThePatientKind()
        {
            Assert.Contains("build name=Bed", PatientRules.NoBedHint(humanlike: true));
            Assert.Contains("AnimalSleepingSpot", PatientRules.NoBedHint(humanlike: false));
        }

        [Fact]
        public void TheFeedBedHintNamesTheBedForThePatientKind()
        {
            Assert.Contains("rescue", PatientRules.FeedBedHint(humanlike: true));
            Assert.Contains("AnimalSleepingSpot", PatientRules.FeedBedHint(humanlike: false));
        }
    }
}
