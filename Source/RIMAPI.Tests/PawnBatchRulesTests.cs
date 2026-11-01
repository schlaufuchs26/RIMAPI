using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The arithmetic behind a starting-colonist batch (fox ticket #3924): the
    // seed that makes a batch reproducible, the bounds of a batch, and the ids
    // a pick may name.
    public class PawnBatchRulesTests
    {
        [Fact]
        public void TheSameWorldSeedDrawsTheSameBatch()
        {
            Assert.Equal(PawnBatchRules.SeedOf("fox-site1"), PawnBatchRules.SeedOf("fox-site1"));
            Assert.Equal(PawnBatchRules.SeedOf("Fox-Site1"), PawnBatchRules.SeedOf("fox-site1"));
        }

        [Fact]
        public void AnotherWorldSeedIsAnotherBatch()
        {
            Assert.NotEqual(PawnBatchRules.SeedOf("fox-site1"), PawnBatchRules.SeedOf("fox-site2"));
        }

        [Fact]
        public void AnEmptySeedStillHasASeed()
        {
            // The start call lowercases and never sends an empty seed, but a
            // missing one must not read as the same stream as a real seed.
            Assert.NotEqual(PawnBatchRules.SeedOf(""), PawnBatchRules.SeedOf("fox"));
        }

        [Fact]
        public void ABatchNeedsAPositiveCountWithinTheCap()
        {
            Assert.Contains("positive", PawnBatchRules.BatchProblem(0, 3));
            Assert.Contains("at most", PawnBatchRules.BatchProblem(61, 3));
            Assert.Equal("", PawnBatchRules.BatchProblem(30, 3));
            Assert.Equal("", PawnBatchRules.BatchProblem(PawnBatchRules.MaxCount, 3));
        }

        [Fact]
        public void ABatchHasToFillTheScenario()
        {
            Assert.Contains("starts 5 colonists", PawnBatchRules.BatchProblem(4, 5));
            Assert.Equal("", PawnBatchRules.BatchProblem(5, 5));
            // A scenario count nobody knows yet is not a refusal.
            Assert.Equal("", PawnBatchRules.BatchProblem(1, 0));
        }

        [Fact]
        public void APickNamesIdsOfTheBatchOnce()
        {
            Assert.Equal("", PawnBatchRules.PicksProblem(null, 30));
            Assert.Equal("", PawnBatchRules.PicksProblem(new List<int>(), 30));
            Assert.Equal("", PawnBatchRules.PicksProblem(new[] { 3, 11, 30 }, 30));
            Assert.Contains("ids are 1 to 30", PawnBatchRules.PicksProblem(new[] { 3, 31 }, 30));
            Assert.Contains("not a batch id", PawnBatchRules.PicksProblem(new[] { 0 }, 0));
            Assert.Contains("picked twice", PawnBatchRules.PicksProblem(new[] { 3, 3 }, 30));
            Assert.Contains("batch holds 3", PawnBatchRules.PicksProblem(new[] { 1, 2, 3, 4 }, 3));
        }

        [Fact]
        public void OnlyASkillWithALevelOrAPassionIsWorthALine()
        {
            Assert.False(PawnBatchRules.WorthShowing(0, 0));
            Assert.True(PawnBatchRules.WorthShowing(1, 0));
            Assert.True(PawnBatchRules.WorthShowing(0, 1));
            Assert.True(PawnBatchRules.WorthShowing(8, 2));
        }
    }
}
