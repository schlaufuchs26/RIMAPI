using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3625: why a colonist will not take a piece of work. The answer used
    // to be "colonists with PlantCutting cut them" whether or not anyone
    // would, so a run could not tell "wait" from "nobody ever will".
    public class WorkAcceptanceRulesTests
    {
        private static WorkAcceptanceRules.State Capable()
        {
            return new WorkAcceptanceRules.State
            {
                HasWorkSettings = true,
                WorkTypeActive = true,
                WorkTagsAllowed = true,
                WorkTypeName = "PlantCutting",
            };
        }

        [Fact]
        public void ACapableColonistIsNotRefused()
        {
            Assert.Null(WorkAcceptanceRules.Refusal(Capable()));
        }

        [Fact]
        public void WorkThatIsOffNamesTheWorkType()
        {
            var state = Capable();
            state.WorkTypeActive = false;
            string refusal = WorkAcceptanceRules.Refusal(state);
            Assert.Contains("PlantCutting", refusal);
            Assert.Contains("off", refusal);
        }

        [Fact]
        public void AnIncapableColonistIsNamedAsSuch()
        {
            var state = Capable();
            state.WorkTagsAllowed = false;
            Assert.Contains("incapable", WorkAcceptanceRules.Refusal(state));
        }

        [Fact]
        public void AMissingCapacityIsNamed()
        {
            var state = Capable();
            state.MissingCapacity = "Manipulation";
            string refusal = WorkAcceptanceRules.Refusal(state);
            Assert.Contains("Manipulation", refusal);
        }

        [Fact]
        public void ADraftedColonistIsNotAvailable()
        {
            var state = Capable();
            state.Drafted = true;
            Assert.Contains("drafted", WorkAcceptanceRules.Refusal(state));
        }

        [Fact]
        public void ADownedColonistIsNotAvailable()
        {
            var state = Capable();
            state.Downed = true;
            Assert.Contains("downed", WorkAcceptanceRules.Refusal(state));
        }

        [Fact]
        public void AMentalStateIsNamed()
        {
            var state = Capable();
            state.MentalState = true;
            Assert.Contains("mental state", WorkAcceptanceRules.Refusal(state));
        }

        // The work tab is the lever a player pulls first, so a pawn without it
        // must not read as "waiting for a free colonist".
        [Fact]
        public void NoWorkSettingsReadsAsWorkOff()
        {
            var state = Capable();
            state.HasWorkSettings = false;
            state.WorkTypeActive = false;
            Assert.Contains("PlantCutting work is off", WorkAcceptanceRules.Refusal(state));
        }
    }
}
