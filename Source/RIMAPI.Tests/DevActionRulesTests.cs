using System.Collections.Generic;
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
        [InlineData("shuttle")]
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
        public void AShuttleNeedsNoPawnAndTakesAColonistCount()
        {
            // The shuttle op names a pawn only when it should wait for one, so
            // the id stays optional; count is how many colonists it wants, and
            // 0 (or none) is a shuttle that waits for nobody in particular.
            Assert.Null(DevActionRules.Problem("shuttle", null, null, null, null, null, null));
            Assert.Null(DevActionRules.Problem("shuttle", 12, null, null, null, null, null));
            Assert.Null(DevActionRules.Problem("shuttle", null, null, null, null, 0, null));
            Assert.Contains("colonists", DevActionRules.Problem("shuttle", null, null, null, null, -1, null));
            Assert.Contains(
                DevActionRules.MaxSpawn.ToString(),
                DevActionRules.Problem("shuttle", null, null, null, null, DevActionRules.MaxSpawn + 1, null));
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

        // A dev entry that needs something a call cannot give it runs and
        // changes nothing, and used to answer "ran X" all the same (ticket
        // #3922). The stamps around the call are what tells the two apart.
        [Fact]
        public void AnEntryThatChangedNothingReportsNoDifference()
        {
            DevActionRules.ColonyStamp before = Stamp(12, 3412, 0);
            DevActionRules.ColonyStamp after = Stamp(12, 3412, 0);

            List<string> moved = DevActionRules.Difference(before, before, after);

            Assert.Empty(moved);
            Assert.Contains("12 pawns", DevActionRules.UnchangedDetail(@"Actions\Add Prisoner", before));
            Assert.Contains("nothing selected", DevActionRules.UnchangedDetail(@"Actions\Add Prisoner", before));
        }

        [Fact]
        public void ASpawnedPawnAndItsOwnStateAreBothNamed()
        {
            DevActionRules.ColonyStamp before = Stamp(12, 3412, 0);
            DevActionRules.ColonyStamp after = Stamp(13, 3413, 0);
            after.PawnDigest = before.PawnDigest + 1;

            List<string> moved = DevActionRules.Difference(before, before, after);

            Assert.Contains("pawns 12 -> 13", moved);
            Assert.Contains("things 3412 -> 3413", moved);
            Assert.Contains("a pawn's own state", moved);
            Assert.Contains("pawns 12 -> 13", DevActionRules.ChangedDetail(@"Actions\Add Prisoner", moved));
        }

        // A counter that already moved between the two stamps taken before the
        // entry is the game ticking: reporting it would call every call a
        // change in a colony that keeps running.
        [Fact]
        public void ACounterThatMovesOnItsOwnIsNoEvidence()
        {
            DevActionRules.ColonyStamp quiet = Stamp(12, 3412, 0);
            DevActionRules.ColonyStamp before = Stamp(12, 3412, 0);
            DevActionRules.ColonyStamp after = Stamp(12, 3412, 0);
            // The letter stack grew between the two control reads.
            quiet.Letters = 3;
            before.Letters = 4;
            after.Letters = 5;
            after.PawnDigest = before.PawnDigest + 7;

            List<string> moved = DevActionRules.Difference(quiet, before, after);

            Assert.DoesNotContain(moved, m => m.StartsWith("letters"));
            Assert.Contains("a pawn's own state", moved);
        }

        [Fact]
        public void AnEntryThatOpenedNothingElseIsStillAChange()
        {
            DevActionRules.ColonyStamp before = Stamp(12, 3412, 0);
            DevActionRules.ColonyStamp after = Stamp(12, 3412, 0);
            after.Bills = before.Bills + 1;
            after.Designations = before.Designations + 4;

            List<string> moved = DevActionRules.Difference(before, before, after);

            Assert.Contains("bills 0 -> 1", moved);
            Assert.Contains("designations 0 -> 4", moved);
        }

        [Fact]
        public void AQuestOfferSendsItsLetterUnlessTheScriptOrTheQuestSaysNo()
        {
            // The game's own rule (IncidentWorker_GiveQuest): a quest that was
            // just made available gets an offer letter only when it is not
            // hidden and its script wants one. A check reads that letter, so a
            // dev offer that stays silent leaves nothing to read (ticket #3951).
            Assert.True(DevActionRules.SendsAvailableLetter(hidden: false, scriptWantsLetter: true));
            Assert.False(DevActionRules.SendsAvailableLetter(hidden: true, scriptWantsLetter: true));
            Assert.False(DevActionRules.SendsAvailableLetter(hidden: false, scriptWantsLetter: false));
            Assert.False(DevActionRules.SendsAvailableLetter(hidden: true, scriptWantsLetter: false));
        }

        /// <summary>A stamp with the counters a call moves, and stable digests.</summary>
        private static DevActionRules.ColonyStamp Stamp(int pawns, int things, int selected)
        {
            return new DevActionRules.ColonyStamp
            {
                Pawns = pawns,
                Things = things,
                Selected = selected,
                Maps = 1,
                PawnDigest = 1234,
                ThingDigest = 5678,
                Letters = 0,
            };
        }
    }
}
