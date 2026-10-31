using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3566: a pawn whose work priorities were never created reads as
    // every work type 0, which is the same as a colonist who switched a type
    // off. The read reports `initialized`, and the write must still be able to
    // give that pawn a level instead of refusing it: the game creates the
    // defaults lazily on the first write.
    public class WorkSettingsRulesTests
    {
        [Fact]
        public void AComponentWithoutPrioritiesNeedsInitialization()
        {
            Assert.Equal(
                WorkSettingsRules.Readiness.NeedsInitialization,
                WorkSettingsRules.Of(hasWorkSettings: true, everWork: false)
            );
        }

        [Fact]
        public void AnInitializedPawnIsReadyToRead()
        {
            Assert.Equal(
                WorkSettingsRules.Readiness.Ready,
                WorkSettingsRules.Of(hasWorkSettings: true, everWork: true)
            );
        }

        [Fact]
        public void APawnWithoutTheComponentCanTakeNoLevel()
        {
            Assert.Equal(
                WorkSettingsRules.Readiness.NoSettings,
                WorkSettingsRules.Of(hasWorkSettings: false, everWork: false)
            );
        }

        // The refusal has to name the pawn: a colony has more than one, and the
        // run picks the id it wants to write.
        [Fact]
        public void TheRefusalNamesThePawn()
        {
            Assert.Contains("Theo", WorkSettingsRules.NoSettingsRefusal("Theo"));
        }
    }
}
