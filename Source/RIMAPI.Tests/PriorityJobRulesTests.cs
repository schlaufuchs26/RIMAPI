using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3626: a `prioritize` call names the job the way the right-click
    // menu does, while the game builds a JobDef with its own defName. The
    // matching is what decides whether the job the game built is the one the
    // caller asked for, so it is tested here rather than in the game.
    public class PriorityJobRulesTests
    {
        [Fact]
        public void TheMenusWordReachesTheGamesDefName()
        {
            Assert.True(PriorityJobRules.Matches("tame", "Tame"));
            Assert.True(PriorityJobRules.Matches("harvest", "Harvest"));
            Assert.True(PriorityJobRules.Matches("chop", "CutPlant"));
            Assert.True(PriorityJobRules.Matches("deconstruct", "Deconstruct"));
        }

        // A defName counts as itself: a run that read the job off an earlier
        // answer can name it instead of the menu's word.
        [Fact]
        public void ADefNameCountsAsItself()
        {
            Assert.True(PriorityJobRules.Matches("TendPatient", "TendPatient"));
            Assert.True(PriorityJobRules.Matches("tendpatient", "TendPatient"));
        }

        // The same word can cover two jobs: a haul is HaulToCell on the ground
        // and HaulToContainer into a shelf, and the game decides which applies.
        [Fact]
        public void OneWordCanNameTwoJobs()
        {
            Assert.True(PriorityJobRules.Matches("haul", "HaulToCell"));
            Assert.True(PriorityJobRules.Matches("haul", "HaulToContainer"));
        }

        // "deconstruct" is the plain job only. DeconstructForBlueprint is the
        // game's own way-clearing job, and its giver answers for any building
        // once the call is forced, so a word that covered both queued it on a
        // working generator (fox #3706).
        [Fact]
        public void TheDeconstructWordIsThePlainJob()
        {
            Assert.True(PriorityJobRules.Matches("deconstruct", "Deconstruct"));
            Assert.False(PriorityJobRules.Matches("deconstruct", "DeconstructForBlueprint"));
            Assert.True(PriorityJobRules.Matches("DeconstructForBlueprint", "DeconstructForBlueprint"));
        }

        // "build" has to reach the job the game actually builds, FinishFrame.
        // The WorkGiverDef names (ConstructFinishFrames and the delivery
        // givers) are not JobDefs, so the old table matched nothing and a
        // caller who named the build got the refusal (fox #3706).
        [Fact]
        public void TheBuildWordReachesTheJobTheGameBuilds()
        {
            Assert.True(PriorityJobRules.Matches("build", "FinishFrame"));
            Assert.True(PriorityJobRules.Matches("build", "finishframe"));
            Assert.True(PriorityJobRules.Matches("Construct", "FinishFrame"));
            Assert.False(PriorityJobRules.Matches("build", "BuildRoof"));
            Assert.False(PriorityJobRules.Matches("build", "ConstructFinishFrames"));
        }

        [Fact]
        public void SpacesAndHyphensReadLikeTheMenu()
        {
            Assert.Equal("firefight", PriorityJobRules.Normalize(" Fire-Fight "));
            Assert.True(PriorityJobRules.Matches("fire-fight", "BeatFire"));
        }

        // A word the table does not know matches nothing, so the call falls
        // through to the refusal that names what the game does offer.
        [Fact]
        public void AnUnknownWordMatchesNoJob()
        {
            Assert.False(PriorityJobRules.Matches("teleport", "Tame"));
            Assert.False(PriorityJobRules.Matches("tame", "Rescue"));
            Assert.False(PriorityJobRules.Matches("", "Tame"));
            Assert.False(PriorityJobRules.Matches("tame", ""));
        }

        // The offer list is what the float menu would show, in the order the
        // givers were scanned, without repeats, and short enough to read.
        [Fact]
        public void TheOfferListIsDeduplicatedAndCapped()
        {
            List<string> offers = PriorityJobRules.OfferList(
                new[] { "HaulToCell", "Clean", "HaulToCell", null, "Mine", "Sow", "Harvest", "TendPatient", "Repair" });
            Assert.Equal(new[] { "HaulToCell", "Clean", "Mine", "Sow", "Harvest", "TendPatient" }, offers);
            Assert.Empty(PriorityJobRules.OfferList(null));
        }
    }
}
