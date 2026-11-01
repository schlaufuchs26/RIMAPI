using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3929: a naming dialog now takes the names a call asks for. The
    // window service writes them into curName / curSecondName and runs the
    // game's own OK; what it sets, and what a refusal says, is decided here so
    // it can be tested without the game.
    public class NameDialogRulesTests
    {
        [Fact]
        public void TheWantedNameWinsAndBothAreTrimmed()
        {
            Assert.Equal("Fuchsbau", NameDialogRules.Resolve("  Fuchsbau ", "Generated"));
            Assert.Equal("Generated", NameDialogRules.Resolve("", "  Generated "));
            Assert.Equal("Generated", NameDialogRules.Resolve(null, "Generated"));
        }

        // A dialog that asks one name is the settlement box on its own and the
        // gravship box; a run that thinks in colony names passes `settlement`
        // and must not have it silently dropped.
        [Fact]
        public void ASingleFieldTakesTheOnlyNameTheCallHas()
        {
            string problem;
            List<string> fromName = NameDialogRules.Plan(false, "Fuchsbau", null, "Old", "", out problem);
            Assert.Null(problem);
            Assert.Equal(new[] { "Fuchsbau" }, fromName);

            List<string> fromSettlement = NameDialogRules.Plan(false, null, "Fuchsbau", "Old", "", out problem);
            Assert.Null(problem);
            Assert.Equal(new[] { "Fuchsbau" }, fromSettlement);

            List<string> kept = NameDialogRules.Plan(false, null, null, "Generated", "", out problem);
            Assert.Null(problem);
            Assert.Equal(new[] { "Generated" }, kept);
        }

        [Fact]
        public void ThePairDialogTakesBothNamesAndFallsBackPerField()
        {
            string problem;
            Assert.Equal(new[] { "Die Fuechse", "Fuchsbau" },
                NameDialogRules.Plan(true, "Die Fuechse", "Fuchsbau", "Old", "OldBase", out problem));
            Assert.Null(problem);

            // One field asked, the other keeps what the game generated.
            Assert.Equal(new[] { "Die Fuechse", "OldBase" },
                NameDialogRules.Plan(true, "Die Fuechse", "", "Old", "OldBase", out problem));
            Assert.Null(problem);
        }

        [Fact]
        public void TheFieldLabelSaysWhichNameTheGameMeans()
        {
            Assert.Equal("name", NameDialogRules.FieldLabel(false, false));
            Assert.Equal("name", NameDialogRules.FieldLabel(true, false));
            Assert.Equal("faction name", NameDialogRules.FieldLabel(false, true));
            Assert.Equal("settlement name", NameDialogRules.FieldLabel(true, true));
        }

        [Fact]
        public void ARefusalRepeatsTheGamesOwnReason()
        {
            string text = NameDialogRules.Refusal("faction name", "a/b", "That name isn't valid.");
            Assert.Contains("faction name", text);
            Assert.Contains("a/b", text);
            Assert.Contains("That name isn't valid.", text);
        }

        [Fact]
        public void AnEmptyFieldSaysSoInsteadOfQuotingNothing()
        {
            string text = NameDialogRules.Refusal("settlement name", "", "That name isn't valid.");
            Assert.Contains("field is empty", text);
            Assert.DoesNotContain("\"\"", text);
        }

        [Fact]
        public void AMissingGameReasonStillReads()
        {
            string text = NameDialogRules.Refusal("name", "X", null);
            Assert.Contains("the game gave no reason", text);
        }
    }
}
