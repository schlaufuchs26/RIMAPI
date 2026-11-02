using System.IO;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The bounds and the wording of a drug policy write (fox ticket #4212, gap
    /// 3). Each number is the Manage drug policies dialog's own slider range, so
    /// the test pins the values a caller could otherwise exceed.
    /// </summary>
    public class DrugPolicyRulesTests
    {
        [Theory]
        [InlineData(0.05f, false)]
        [InlineData(0.1f, true)]
        [InlineData(1f, true)]
        [InlineData(25f, true)]
        [InlineData(26f, false)]
        public void TheScheduleRunsFromATenthOfADayToTwentyFive(float days, bool valid)
        {
            Assert.Equal(valid, DrugPolicyRules.DaysFrequencyValid(days));
        }

        [Theory]
        [InlineData(0f, false)]
        [InlineData(0.005f, false)]
        [InlineData(0.01f, true)]
        [InlineData(0.5f, true)]
        [InlineData(1f, true)]
        [InlineData(1.2f, false)]
        public void AMoodOrJoyLimitIsAFractionOfTheBar(float limit, bool valid)
        {
            Assert.Equal(valid, DrugPolicyRules.LimitValid(limit));
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        [InlineData(5, true)]
        [InlineData(6, false)]
        public void TakeToInventoryStopsAtWhatAPawnCanCarry(int count, bool valid)
        {
            Assert.Equal(valid, DrugPolicyRules.TakeToInventoryValid(count, 5));
        }

        [Fact]
        public void TheScheduleRefusalNamesBothEnds()
        {
            string text = DrugPolicyRules.DaysFrequencyProblem(40f);
            Assert.Contains("0.1", text);
            Assert.Contains("25", text);
            Assert.Contains("40", text);
        }

        [Fact]
        public void TheLimitRefusalNamesTheField()
        {
            Assert.Contains("only_if_joy_below", DrugPolicyRules.LimitProblem("only_if_joy_below", 1.5f));
            Assert.Contains("1.5", DrugPolicyRules.LimitProblem("only_if_joy_below", 1.5f));
        }

        [Fact]
        public void TheCarryRefusalNamesThePawnsLimit()
        {
            string text = DrugPolicyRules.TakeToInventoryProblem(9, 5);
            Assert.Contains("0 to 5", text);
            Assert.Contains("9", text);
        }

        [Fact]
        public void TheOtherRefusalsNameTheirFact()
        {
            Assert.Contains("label", DrugPolicyRules.NoLabel());
            Assert.Contains("no entry", DrugPolicyRules.NoEntries());
            Assert.Contains("no pawn", DrugPolicyRules.UnknownPawn(7));
            Assert.Contains("only humanlike", DrugPolicyRules.NoDrugTracker("Bess"));
            Assert.Contains("not a drug", DrugPolicyRules.NotADrug("Steel"));
            Assert.Contains("no line for", DrugPolicyRules.NoEntryFor("Worker", "Beer"));
            Assert.Contains("already follows", DrugPolicyRules.AlreadyFollowed("Vulpes", "Worker"));
            Assert.Contains("already the default", DrugPolicyRules.AlreadyDefault("Worker"));
        }

        [Fact]
        public void AWritesGoThroughTheDialogsOwnButtons()
        {
            // MakeNewDrugPolicy + CopyFrom are the New and Duplicate buttons,
            // TryDelete is the cross with the game's "in use" sentence. A service
            // that built its own DrugPolicy would skip the per-drug lines the
            // constructor creates.
            string root = SourceTree.RepoRoot();
            string service = Path.Combine(
                root,
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Services",
                "Pawns",
                "DrugPolicyService",
                "DrugPolicyService.cs"
            );

            string text = File.ReadAllText(service);
            Assert.Contains("MakeNewDrugPolicy(", text);
            Assert.Contains("CopyFrom(", text);
            Assert.Contains("TryDelete(", text);
            Assert.DoesNotContain("new DrugPolicyEntry {", text);
        }
    }
}
