using System.IO;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The wording and the arithmetic of an apparel policy's writes (fox ticket
    /// #4212, gap 2). The dialog's own sentence covers a policy in use; these
    /// cover the refusals that have no game text, because a caller who guessed a
    /// def name should learn which names exist.
    /// </summary>
    public class ApparelPolicyRulesTests
    {
        [Theory]
        [InlineData(0f, 1f, true)]
        [InlineData(0.5f, 0.5f, true)]
        [InlineData(0.2f, 0.9f, true)]
        [InlineData(0.9f, 0.2f, false)]
        [InlineData(-0.1f, 1f, false)]
        [InlineData(0f, 1.1f, false)]
        public void AHitPointsRangeIsValidOnlyInsideTheScale(float min, float max, bool valid)
        {
            Assert.Equal(valid, ApparelPolicyRules.HitPointsValid(min, max));
        }

        [Fact]
        public void TheHitPointsRefusalNamesTheScale()
        {
            string text = ApparelPolicyRules.HitPointsProblem(0.9f, 0.2f);
            Assert.Contains("0 to 1", text);
            Assert.Contains("0.9", text);
        }

        [Fact]
        public void TheQualityRefusalListsTheSevenNames()
        {
            string text = ApparelPolicyRules.QualityProblem("Superb");
            Assert.Contains("Superb", text);
            foreach (string name in ApparelPolicyRules.QualityNames)
                Assert.Contains(name, text);
        }

        [Fact]
        public void TheSevenQualityNamesAreTheGames()
        {
            Assert.Equal(7, ApparelPolicyRules.QualityNames.Length);
            Assert.Equal("Awful", ApparelPolicyRules.QualityNames[0]);
            Assert.Equal("Legendary", ApparelPolicyRules.QualityNames[6]);
        }

        [Fact]
        public void AnUnknownPolicyNamesTheIdsThatExist()
        {
            string text = ApparelPolicyRules.UnknownPolicy(99, new[] { 1, 2, 5 });
            Assert.Contains("99", text);
            Assert.Contains("1, 2, 5", text);
        }

        [Fact]
        public void ADefThatIsNotApparelIsRefusedByName()
        {
            string text = ApparelPolicyRules.NotApparel("Steel");
            Assert.Contains("Steel", text);
            Assert.Contains("not apparel", text);
        }

        [Fact]
        public void TheOtherRefusalsNameTheirFact()
        {
            Assert.Contains("label", ApparelPolicyRules.NoLabel());
            Assert.Contains("nothing to change", ApparelPolicyRules.NothingToChange());
            Assert.Contains("no pawn", ApparelPolicyRules.UnknownPawn(7));
            Assert.Contains("only humanlike", ApparelPolicyRules.NoOutfitTracker("Bess"));
            Assert.Contains("special filter", ApparelPolicyRules.UnknownSpecialFilter("Nope"));
            Assert.Contains("already wears", ApparelPolicyRules.AlreadyWorn("Vulpes", "Worker"));
        }

        [Fact]
        public void DeletingAPolicyGoesThroughTheGamesOwnRefusal()
        {
            // The dialog's Delete asks OutfitDatabase.TryDelete, whose "outfit in
            // use" report is the game's sentence. A service that removed the
            // policy itself would take a worn policy away from its pawns.
            string root = SourceTree.RepoRoot();
            string service = Path.Combine(
                root,
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Services",
                "Pawns",
                "OutfitService",
                "OutfitService.cs"
            );

            string text = File.ReadAllText(service);
            Assert.Contains("TryDelete(", text);
            Assert.Contains("MakeNewOutfit(", text);
        }
    }
}
