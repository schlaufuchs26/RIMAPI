using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // A bed's own medical setting (fox ticket #4235): the flag the bed tab's
    // "Medical" gizmo writes, and the refusals that name what stops a write
    // instead of a bare "failed".
    public class BedMedicalRulesTests
    {
        [Fact]
        public void ABedWhoseDefAllowsItTakesTheFlag()
        {
            Assert.True(BedMedicalRules.CanTake(true, true));
        }

        [Fact]
        public void ADefWithoutTheToggleRefusesTheFlag()
        {
            Assert.False(BedMedicalRules.CanTake(false, true));
        }

        [Fact]
        public void AnyBedDropsTheFlag()
        {
            // The game's setter only guards the way on: a hospital bed whose def
            // allows the toggle can always be turned back into a normal bed.
            Assert.True(BedMedicalRules.CanTake(false, false));
        }

        [Fact]
        public void TheRefusalNamesTheDefAndTheMissingToggle()
        {
            string refusal = BedMedicalRules.NoMedicalSetting("animal bed", 77261, "Bed_Animal");

            Assert.Contains("animal bed", refusal);
            Assert.Contains("77261", refusal);
            Assert.Contains("Bed_Animal", refusal);
            Assert.Contains("no medical setting", refusal);
        }

        [Fact]
        public void ABuildingThatIsNoBedIsRefusedWithItsDef()
        {
            string refusal = BedMedicalRules.NotABed("wall", 51058, "Wall_BlocksGranite");

            Assert.Contains("is not a bed", refusal);
            Assert.Contains("Wall_BlocksGranite", refusal);
        }

        [Fact]
        public void TheFlagTookWhenTheBedReadsIt()
        {
            Assert.True(BedMedicalRules.Took(true, true));
            Assert.True(BedMedicalRules.Took(false, false));
        }

        [Fact]
        public void TheFlagDidNotTakeWhenTheBedReadsTheOtherValue()
        {
            Assert.False(BedMedicalRules.Took(true, false));
            Assert.False(BedMedicalRules.Took(false, true));
        }

        [Fact]
        public void TheWriteRefusalSaysWhatTheBedReads()
        {
            string refusal = BedMedicalRules.DidNotTake("bed", 51058, true, false);

            Assert.Contains("did not take the medical setting", refusal);
            Assert.Contains("it reads a normal bed", refusal);
            Assert.Contains("asked for a medical bed", refusal);
        }

        [Fact]
        public void TheSettingReadsInWords()
        {
            Assert.Equal("a medical bed", BedMedicalRules.Describe(true));
            Assert.Equal("a normal bed", BedMedicalRules.Describe(false));
        }
    }
}
