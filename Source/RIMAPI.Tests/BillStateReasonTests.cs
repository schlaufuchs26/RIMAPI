using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The states of a bill (ticket #3306): a bill that does not run has to say
    // so, in the game's own words, next to the recipe and the target.
    public class BillStateReasonTests
    {
        private static BillStateReason.Facts Facts()
        {
            return new BillStateReason.Facts
            {
                MissingIngredients = new List<string>(),
            };
        }

        [Fact]
        public void AnUnblockedBillIsRunning()
        {
            var state = BillStateReason.Describe(Facts());

            Assert.Equal(BillStateReason.Running, state.Name);
            Assert.Null(state.Reason);
        }

        [Fact]
        public void ASuspendedBillSaysSo()
        {
            var facts = Facts();
            facts.Suspended = true;

            var state = BillStateReason.Describe(facts);

            Assert.Equal(BillStateReason.Suspended, state.Name);
            Assert.Equal("suspended by you", state.Reason);
        }

        [Fact]
        public void ASuspendedBillAlsoNamesTheMissingMaterial()
        {
            var facts = Facts();
            facts.Suspended = true;
            facts.MissingIngredients = new List<string> { "leather" };

            var state = BillStateReason.Describe(facts);

            Assert.Equal("suspended by you; nothing to work with: no leather", state.Reason);
        }

        [Fact]
        public void APausedBillSaysSo()
        {
            var facts = Facts();
            facts.Paused = true;

            Assert.Equal(BillStateReason.Paused, BillStateReason.Describe(facts).Name);
        }

        [Fact]
        public void ATargetMetBillCarriesTheCount()
        {
            var facts = Facts();
            facts.TargetMet = true;
            facts.TargetText = "3 of 3 parkas in the colony";

            var state = BillStateReason.Describe(facts);

            Assert.Equal(BillStateReason.Satisfied, state.Name);
            Assert.Equal("3 of 3 parkas in the colony", state.Reason);
        }

        [Fact]
        public void AMissingIngredientIsNamedTheWayTheGameNamesIt()
        {
            var facts = Facts();
            facts.MissingIngredients = new List<string> { "leather", "cloth" };

            var state = BillStateReason.Describe(facts);

            Assert.Equal(BillStateReason.MissingIngredients, state.Name);
            Assert.Equal("nothing to work with: no leather, no cloth", state.Reason);
        }

        [Fact]
        public void ARecipeNobodyCanDoNamesTheSkillAndTheBestColonist()
        {
            var facts = Facts();
            facts.Skill = "Crafting";
            facts.MinSkill = 6;
            facts.BestSkill = 3;
            facts.SkilledColonists = 0;

            var state = BillStateReason.Describe(facts);

            Assert.Equal(BillStateReason.NoWorker, state.Name);
            Assert.Equal("needs Crafting 6; the best colonist has 3", state.Reason);
        }

        [Fact]
        public void ASkillRequirementSomebodyMeetsBlocksNothing()
        {
            var facts = Facts();
            facts.Skill = "Crafting";
            facts.MinSkill = 6;
            facts.BestSkill = 8;
            facts.SkilledColonists = 1;

            Assert.Equal(BillStateReason.Running, BillStateReason.Describe(facts).Name);
        }

        [Fact]
        public void TheSwitchesComeBeforeTheMaterial()
        {
            // A player who suspended a bill reads their own switch first, even
            // when the colony is also short of the material.
            var facts = Facts();
            facts.Paused = true;
            facts.MissingIngredients = new List<string> { "leather" };

            Assert.Equal(BillStateReason.Paused, BillStateReason.Describe(facts).Name);
        }

        [Fact]
        public void ABenchNobodyIsAssignedToNamesItsWorkType()
        {
            // The bench is fine and the material is there, but no colonist has
            // Crafting switched on, so the bill never gets a job (ticket
            // #3362). "running" here is the lie the ticket is about.
            var facts = Facts();
            facts.WorkTypes = new List<string> { "Crafting" };
            facts.AssignedColonists = 0;

            var state = BillStateReason.Describe(facts);

            Assert.Equal(BillStateReason.NoColonistAssigned, state.Name);
            Assert.Equal("nobody is assigned to Crafting", state.Reason);
        }

        [Fact]
        public void TwoWorkTypesOfOneBenchAreBothNamed()
        {
            // A bench can be served by more than one giver (a mod's smelter
            // that both smiths and crafts), and the player then has two
            // switches to flip.
            var facts = Facts();
            facts.WorkTypes = new List<string> { "Smithing", "Crafting" };
            facts.AssignedColonists = 0;

            Assert.Equal("nobody is assigned to Smithing or Crafting",
                BillStateReason.Describe(facts).Reason);
        }

        [Fact]
        public void AWorkTypeSomebodyHasBlocksNothing()
        {
            var facts = Facts();
            facts.WorkTypes = new List<string> { "Crafting" };
            facts.AssignedColonists = 2;

            Assert.Equal(BillStateReason.Running, BillStateReason.Describe(facts).Name);
        }

        [Fact]
        public void ABenchTheGameNamesNoWorkTypeForClaimsNothing()
        {
            // No giver lists this def (an unknown bench from a mod), so the
            // assignment cannot be judged and the bill is not accused.
            var facts = Facts();
            facts.WorkTypes = new List<string>();

            Assert.Equal(BillStateReason.Running, BillStateReason.Describe(facts).Name);
        }

        [Fact]
        public void TheAssignmentComesBeforeTheSkill()
        {
            // Nobody works Crafting, so the skill nobody has is not the first
            // thing to fix: switching the type on is.
            var facts = Facts();
            facts.WorkTypes = new List<string> { "Crafting" };
            facts.AssignedColonists = 0;
            facts.Skill = "Crafting";
            facts.MinSkill = 6;
            facts.BestSkill = 3;
            facts.SkilledColonists = 0;

            Assert.Equal(BillStateReason.NoColonistAssigned, BillStateReason.Describe(facts).Name);
        }
    }
}
