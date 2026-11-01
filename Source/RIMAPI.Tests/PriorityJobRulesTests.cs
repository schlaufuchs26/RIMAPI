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
            Assert.True(PriorityJobRules.Matches("build", "ConstructFinishFrames"));
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
