using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The request shape behind `dev` (fox ticket #3757): which ops exist, which
    // fields each one needs, and which numbers the game can run. A check that
    // builds a test situation must fail on a typo instead of clamping it into a
    // state nobody asked for.
    public class DevActionRulesTests
    {
        [Theory]
        [InlineData("damage")]
        [InlineData("heal")]
        [InlineData("hediff")]
        [InlineData("infection")]
        [InlineData("spawn")]
        [InlineData("spawn_animal")]
        [InlineData("spawn_pawn")]
        [InlineData("mental")]
        [InlineData("skill")]
        [InlineData("trait")]
        [InlineData("incident")]
        [InlineData("quest")]
        [InlineData("wealth")]
        [InlineData("ticks")]
        [InlineData("debug_actions")]
        [InlineData("debug_action")]
        public void EveryDocumentedOpIsKnown(string op)
        {
            Assert.True(DevActionRules.IsOp(op));
        }

        [Fact]
        public void AnUnknownOpNamesTheOnesThereAre()
        {
            string problem = DevActionRules.Problem("nuke", null, null, null, null, null, null);
            Assert.Contains("nuke", problem);
            Assert.Contains("damage", problem);
            Assert.Contains("ticks", problem);
        }

        [Fact]
        public void AnOpWithoutItsNameIsRefused()
        {
            // A spawn without a defName would place nothing and report success.
            string problem = DevActionRules.Problem("spawn", null, "", null, null, null, null);
            Assert.Contains("name", problem);
            Assert.Contains("spawn", problem);
            Assert.Null(DevActionRules.Problem("spawn", null, "Steel", null, null, 3, null));
        }

        [Fact]
        public void APawnOpWithoutItsIdIsRefused()
        {
            string problem = DevActionRules.Problem("damage", null, null, 5f, null, null, null);
            Assert.Contains("id", problem);
            Assert.Null(DevActionRules.Problem("damage", 12, null, 5f, null, null, null));
            // An op that acts on the map needs no pawn.
            Assert.Null(DevActionRules.Problem("ticks", null, null, null, null, null, 100));
        }

        [Fact]
        public void DamageNeedsAnAmountWithinAWholeBody()
        {
            Assert.Contains("amount", DevActionRules.Problem("damage", 3, null, null, null, null, null));
            Assert.Contains("amount", DevActionRules.Problem("damage", 3, null, 0f, null, null, null));
            // 1000 is the cap; past it the number is a typo, not a hit.
            Assert.Null(DevActionRules.Problem("damage", 3, null, DevActionRules.MaxDamage, null, null, null));
            Assert.Contains("1000", DevActionRules.Problem("damage", 3, null, 1001f, null, null, null));
        }

        [Fact]
        public void ASkillLevelRunsFromZeroToTwenty()
        {
            Assert.Contains("level", DevActionRules.Problem("skill", 3, "Cooking", null, null, null, null));
            Assert.Null(DevActionRules.Problem("skill", 3, "Cooking", null, 0, null, null));
            Assert.Null(DevActionRules.Problem("skill", 3, "Cooking", null, 20, null, null));
            Assert.Contains("20", DevActionRules.Problem("skill", 3, "Cooking", null, 21, null, null));
        }

        [Fact]
        public void ASpawnCountIsOneToFiveHundred()
        {
            Assert.Contains("count", DevActionRules.Problem("spawn", null, "Steel", null, null, 0, null));
            Assert.Null(DevActionRules.Problem("spawn", null, "Steel", null, null, 500, null));
            Assert.Contains("500", DevActionRules.Problem("spawn", null, "Steel", null, null, 501, null));
            // No count given means one, not zero.
            Assert.Equal(1, DevActionRules.SpawnCount(null));
            Assert.Equal(7, DevActionRules.SpawnCount(7));
        }

        [Fact]
        public void ATickSpanIsPositiveAndStaysInsideOneCall()
        {
            Assert.Contains("ticks", DevActionRules.Problem("ticks", null, null, null, null, null, null));
            Assert.Null(DevActionRules.Problem("ticks", null, null, null, null, null, DevActionRules.MaxTicks));
            Assert.Contains("20000", DevActionRules.Problem("ticks", null, null, null, null, null, DevActionRules.MaxTicks + 1));
        }

        [Fact]
        public void WealthAddsSilverUpToItsCap()
        {
            Assert.Contains("amount", DevActionRules.Problem("wealth", null, null, null, null, null, null));
            Assert.Null(DevActionRules.Problem("wealth", null, null, 0f, null, null, null));
            Assert.Null(DevActionRules.Problem("wealth", null, null, 25000f, null, null, null));
            Assert.Contains("100000", DevActionRules.Problem("wealth", null, null, 100001f, null, null, null));
        }

        [Fact]
        public void AHediffSeverityStaysOnTheDiseaseScale()
        {
            Assert.Null(DevActionRules.Problem("hediff", 3, "Flu", null, null, null, null));
            Assert.Null(DevActionRules.Problem("hediff", 3, "Flu", 0.6f, null, null, null));
            Assert.Contains("100", DevActionRules.Problem("hediff", 3, "Flu", 101f, null, null, null));
            Assert.Equal(0f, DevActionRules.Severity(-1f));
            Assert.Null(DevActionRules.Severity(null));
        }

        [Fact]
        public void HealNeedsOnlyThePawn()
        {
            Assert.Null(DevActionRules.Problem("heal", 4, null, null, null, null, null));
            Assert.True(DevActionRules.NeedsPawn("heal"));
            Assert.False(DevActionRules.NeedsName("heal"));
            Assert.False(DevActionRules.NeedsPawn("incident"));
        }

        // A pawn spawn is the general form of the animal one (ticket #3799): it
        // needs the kind it generates, and the same count cap, because the game
        // generates each pawn in one call.
        [Fact]
        public void APawnSpawnNeedsItsKind()
        {
            Assert.Contains("name", DevActionRules.Problem("spawn_pawn", null, null, null, null, null, null));
            Assert.Null(DevActionRules.Problem("spawn_pawn", null, "WildMan", null, null, 2, null));
            Assert.Contains("500", DevActionRules.Problem("spawn_pawn", null, "WildMan", null, null, 501, null));
        }

        // The menu's paths come with the Actions root and either separator; a
        // caller who writes the tail of the path still names one entry.
        [Theory]
        [InlineData(@"Actions\Spawning\WildMan", "Actions/Spawning/WildMan")]
        [InlineData(@"Actions\Spawning\WildMan", "Spawning/WildMan")]
        [InlineData(@"Actions\Spawning\WildMan", @"actions\spawning\wildman")]
        public void AMenuPathMatchesWhatTheCallNames(string nodePath, string wanted)
        {
            Assert.True(DevActionRules.MatchesPath(nodePath, wanted));
        }

        [Fact]
        public void AMenuPathIsNotAMereSubstring()
        {
            // The last step of a path is enough (the leaf's own label), but a
            // part of a word is not: "Wild" would otherwise pick whichever entry
            // the walk reaches first.
            Assert.True(DevActionRules.MatchesPath(@"Actions\Spawning\WildMan", "WildMan"));
            Assert.False(DevActionRules.MatchesPath(@"Actions\Spawning\WildMan", "Wild"));
            Assert.False(DevActionRules.MatchesPath(@"Actions\Spawning\WildMan", ""));
        }

        // A listing's filter is a substring: it is how a caller reaches an entry
        // the walk would otherwise pass by (ticket #3855).
        [Fact]
        public void AListingFiltersByAnyPartOfThePath()
        {
            Assert.True(DevActionRules.MatchesFilter(@"Actions\Spawning\WildMan", "Wild"));
            Assert.True(DevActionRules.MatchesFilter(@"Actions\Spawning\WildMan", "spawning/wild"));
            Assert.False(DevActionRules.MatchesFilter(@"Actions\Spawning\WildMan", "Caribou"));
            // No filter lists everything the walk reaches.
            Assert.True(DevActionRules.MatchesFilter(@"Actions\Spawning\WildMan", ""));
            Assert.True(DevActionRules.MatchesFilter(@"Actions\Spawning\WildMan", "  "));
        }

        [Fact]
        public void AListingPrintsAHundredPathsUnlessTheCallAsksForMore()
        {
            Assert.Equal(100, DevActionRules.ListLimit(null));
            Assert.Equal(7, DevActionRules.ListLimit(7));
            Assert.Equal(DevActionRules.MaxListed, DevActionRules.ListLimit(DevActionRules.MaxListed + 1));
        }
    }
}
