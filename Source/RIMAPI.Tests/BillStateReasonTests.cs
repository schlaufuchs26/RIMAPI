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
        public void AMissingIngredientTheFilterDropsIsNamedWithItsDefName()
        {
            // "nothing to work with: no twisted meat" reads the same whether the
            // colony is out of twisted meat or the bill's own filter keeps it
            // out, and the two fixes are opposite: haul meat, or let it in
            // (fox ticket #3931).
            var facts = Facts();
            facts.MissingIngredients = new List<string> { "twisted meat" };
            facts.FilterExcluded = new List<BillStateReason.ExcludedMaterial>
            {
                new BillStateReason.ExcludedMaterial { DefName = "Meat_Twisted", Count = 12 },
            };

            var state = BillStateReason.Describe(facts);

            Assert.Equal(BillStateReason.MissingIngredients, state.Name);
            Assert.Equal(
                "nothing to work with: no twisted meat; the filter keeps out Meat_Twisted (12 in the colony)",
                state.Reason);
        }

        [Fact]
        public void AFilterThatDropsNothingTheColonyHasAddsNoSentence()
        {
            var facts = Facts();
            facts.MissingIngredients = new List<string> { "leather" };
            facts.FilterExcluded = new List<BillStateReason.ExcludedMaterial>();

            Assert.Equal("nothing to work with: no leather",
                BillStateReason.Describe(facts).Reason);
        }

        [Fact]
        public void ABillThatRunsNeverCarriesTheFilterNote()
        {
            // The note is about a bill that cannot start. A running bill whose
            // filter drops something the colony has plenty of is not stuck, and
            // its reason stays empty.
            var facts = Facts();
            facts.FilterExcluded = new List<BillStateReason.ExcludedMaterial>
            {
                new BillStateReason.ExcludedMaterial { DefName = "Meat_Twisted", Count = 12 },
            };

            var state = BillStateReason.Describe(facts);

            Assert.Equal(BillStateReason.Running, state.Name);
            Assert.Null(state.Reason);
        }

        [Fact]
        public void OnlyTheFirstFewExcludedMaterialsAreNamed()
        {
            // A broad filter can drop dozens of things the colony has; the line
            // names three and counts the rest, like the filter read does.
            var facts = Facts();
            facts.MissingIngredients = new List<string> { "twisted meat" };
            facts.FilterExcluded = new List<BillStateReason.ExcludedMaterial>
            {
                new BillStateReason.ExcludedMaterial { DefName = "Meat_Twisted", Count = 12 },
                new BillStateReason.ExcludedMaterial { DefName = "Meat_Boar", Count = 3 },
                new BillStateReason.ExcludedMaterial { DefName = "Meat_Caribou", Count = 2 },
                new BillStateReason.ExcludedMaterial { DefName = "Meat_Hare", Count = 1 },
                new BillStateReason.ExcludedMaterial { DefName = "Meat_Muffalo", Count = 1 },
            };

            Assert.Equal(
                "nothing to work with: no twisted meat; the filter keeps out "
                + "Meat_Twisted (12 in the colony), Meat_Boar (3 in the colony), "
                + "Meat_Caribou (2 in the colony), +2 more",
                BillStateReason.Describe(facts).Reason);
        }

        [Fact]
        public void AForbiddenStackIsNamedWithTheAllowCall()
        {
            // Fox ticket #3984: the colony has the twisted meat, but the only
            // stack carries the forbidden flag, so hauling and waiting both do
            // nothing. The line has to name the stack and the call that lifts
            // the flag.
            var facts = Facts();
            facts.MissingIngredients = new List<string> { "twisted meat" };
            facts.ForbiddenStock = new List<BillStateReason.ForbiddenMaterial>
            {
                new BillStateReason.ForbiddenMaterial
                {
                    DefName = "Meat_Twisted",
                    Count = 10,
                    ThingIds = new List<int> { 12345 },
                },
            };

            var state = BillStateReason.Describe(facts);

            Assert.Equal(BillStateReason.MissingIngredients, state.Name);
            Assert.Equal(
                "nothing to work with: no twisted meat; forbidden: 10 Meat_Twisted "
                + "(thing 12345); action allow ids=12345 clears the flag",
                state.Reason);
        }

        [Fact]
        public void AForbiddenDefSplitOverSeveralStacksNamesAllTheirIds()
        {
            // One `allow` call can carry several ids, so the line names every
            // stack the flag sits on instead of one that leaves the rest.
            var facts = Facts();
            facts.MissingIngredients = new List<string> { "caribou meat" };
            facts.ForbiddenStock = new List<BillStateReason.ForbiddenMaterial>
            {
                new BillStateReason.ForbiddenMaterial
                {
                    DefName = "Meat_Caribou",
                    Count = 30,
                    ThingIds = new List<int> { 4711, 4712 },
                },
            };

            Assert.Equal(
                "nothing to work with: no caribou meat; forbidden: 30 Meat_Caribou "
                + "(things 4711, 4712); action allow ids=4711,4712 clears the flag",
                BillStateReason.Describe(facts).Reason);
        }

        [Fact]
        public void OnlyTheFirstFewForbiddenStacksAreNamed()
        {
            var facts = Facts();
            facts.MissingIngredients = new List<string> { "meat" };
            facts.ForbiddenStock = new List<BillStateReason.ForbiddenMaterial>
            {
                new BillStateReason.ForbiddenMaterial
                {
                    DefName = "Meat_Twisted", Count = 30, ThingIds = new List<int> { 1 },
                },
                new BillStateReason.ForbiddenMaterial
                {
                    DefName = "Meat_Boar", Count = 12, ThingIds = new List<int> { 2 },
                },
                new BillStateReason.ForbiddenMaterial
                {
                    DefName = "Meat_Caribou", Count = 5, ThingIds = new List<int> { 3 },
                },
                new BillStateReason.ForbiddenMaterial
                {
                    DefName = "Meat_Hare", Count = 2, ThingIds = new List<int> { 4 },
                },
            };

            Assert.Equal(
                "nothing to work with: no meat; forbidden: 30 Meat_Twisted (thing 1), "
                + "12 Meat_Boar (thing 2), 5 Meat_Caribou (thing 3), +1 more; "
                + "action allow ids=1,2,3 clears the flag",
                BillStateReason.Describe(facts).Reason);
        }

        [Fact]
        public void ABillThatRunsNeverCarriesTheForbiddenNote()
        {
            var facts = Facts();
            facts.ForbiddenStock = new List<BillStateReason.ForbiddenMaterial>
            {
                new BillStateReason.ForbiddenMaterial
                {
                    DefName = "Meat_Twisted", Count = 10, ThingIds = new List<int> { 1 },
                },
            };

            var state = BillStateReason.Describe(facts);

            Assert.Equal(BillStateReason.Running, state.Name);
            Assert.Null(state.Reason);
        }

        [Fact]
        public void AForbiddenStackWithNoThingIdNamesNoCall()
        {
            // A mod build that reports the stack but no id cannot name a call
            // that would work, so the sentence stops at the stack.
            var facts = Facts();
            facts.MissingIngredients = new List<string> { "twisted meat" };
            facts.ForbiddenStock = new List<BillStateReason.ForbiddenMaterial>
            {
                new BillStateReason.ForbiddenMaterial { DefName = "Meat_Twisted", Count = 10 },
            };

            Assert.Equal(
                "nothing to work with: no twisted meat; forbidden: 10 Meat_Twisted",
                BillStateReason.Describe(facts).Reason);
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
