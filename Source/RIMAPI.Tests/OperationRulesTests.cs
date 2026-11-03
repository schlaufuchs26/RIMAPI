using System.IO;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The wording of an operation's refusals (fox ticket #4212, gap 4). The
    /// health tab picks a recipe and a part from lists, so a caller who guessed a
    /// name must learn which names exist.
    /// </summary>
    public class OperationRulesTests
    {
        [Fact]
        public void TheMedicinePoliciesAreTheHealthTabsFive()
        {
            Assert.Equal(5, OperationRules.CareNames.Length);
            Assert.Equal("NoCare", OperationRules.CareNames[0]);
            Assert.Equal("Best", OperationRules.CareNames[4]);
        }

        [Fact]
        public void AMissingPartNamesThePartsThatExist()
        {
            string text = OperationRules.PartRequired("Install bionic eye", new[] { "left eye", "right eye" });
            Assert.Contains("Install bionic eye", text);
            Assert.Contains("left eye", text);
            Assert.Contains("right eye", text);
        }

        [Fact]
        public void AnUnknownPartNamesTheOnesTheRecipeCanWorkOn()
        {
            string text = OperationRules.UnknownPart("tail", new[] { "left leg", "right leg" });
            Assert.Contains("tail", text);
            Assert.Contains("left leg", text);
        }

        [Fact]
        public void AnAmbiguousPartNamesTheMatches()
        {
            string text = OperationRules.AmbiguousPart("eye", new[] { "left eye", "right eye" });
            Assert.Contains("eye", text);
            Assert.Contains("left eye", text);
            Assert.Contains("right eye", text);
        }

        [Fact]
        public void TheOtherRefusalsNameTheirFact()
        {
            Assert.Contains("no pawn", OperationRules.UnknownPawn(7));
            Assert.Contains("no recipe", OperationRules.UnknownRecipe("Nope"));
            Assert.Contains("not an operation", OperationRules.NotSurgery("Make_StoneBlocks"));
            Assert.Contains("no operation with id", OperationRules.UnknownBill("Bill_X_1"));
            Assert.Contains("needs the operation's id", OperationRules.NoOperationId());
            Assert.Contains("health tab", OperationRules.NoHealthTracker("Bess"));
            Assert.Contains("already takes", OperationRules.AlreadyCare("Vulpes", "Best"));
        }

        [Fact]
        public void AMissingDrugIsNamedWithTheRecipe()
        {
            // The health tab hides an "Administer drug" entry while the colony
            // holds none of that drug, so the refusal has to name both the
            // recipe and the drug the caller has to get (fox ticket #4323).
            string text = OperationRules.NoDrugInColony("Administer flake", "flake");
            Assert.Contains("Administer flake", text);
            Assert.Contains("flake", text);
        }

        [Fact]
        public void TheCareRefusalListsTheFivePolicies()
        {
            string text = OperationRules.UnknownCare("Bestest");
            Assert.Contains("Bestest", text);
            foreach (string care in OperationRules.CareNames)
                Assert.Contains(care, text);
        }

        [Fact]
        public void TheWritesGoThroughTheHealthTabsOwnPath()
        {
            // CreateSurgeryBill adds the bill, sets the part and posts the game's
            // own warnings; BillStack.Delete is the cross. A service that built a
            // Bill_Medical by hand would skip the part check and the messages.
            string root = SourceTree.RepoRoot();
            string service = Path.Combine(
                root,
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Services",
                "Pawns",
                "OperationService",
                "OperationService.cs"
            );

            string text = File.ReadAllText(service);
            Assert.Contains("CreateSurgeryBill(", text);
            Assert.Contains("BillStack.Delete(", text);
            Assert.DoesNotContain("new Bill_Medical(", text);
        }

        [Fact]
        public void TheDrugCheckIsTheHealthTabsOwnFilter()
        {
            // The health tab hides an "Administer drug" entry while the colony
            // holds none of that drug: it drops every recipe whose
            // PotentiallyMissingIngredients names a drug. The write has to ask
            // the same check, or the bill waits for stock that never comes
            // (fox ticket #4323).
            string root = SourceTree.RepoRoot();
            string service = Path.Combine(
                root,
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Services",
                "Pawns",
                "OperationService",
                "OperationService.cs"
            );

            string text = File.ReadAllText(service);
            Assert.Contains("Recipe_AdministerIngestible", text);
            Assert.Contains("PotentiallyMissingIngredients(", text);
        }
    }
}
