using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3929: a colony may name its own ideoligion. The checks that keep a
    // refused name out of the world live here, so a duplicate or an empty name
    // is caught before the ideo reaches the game.
    public class IdeoNameRulesTests
    {
        private static readonly List<string> Kept = new List<string> { "Der Pfad", "The Path" };

        [Fact]
        public void AFreshNamePasses()
        {
            Assert.Null(IdeoNameRules.Check("Fuchsglaube", Kept, "empty"));
        }

        // The game's own message for an empty name is what a refusal prints,
        // because Page_ConfigureIdeo refuses the same case with that key.
        [Fact]
        public void AnEmptyNameRepeatsTheGamesOwnMessage()
        {
            Assert.Equal("The ideoligion name can't be empty.",
                IdeoNameRules.Check("", Kept, "The ideoligion name can't be empty."));
            Assert.Equal("The ideoligion name can't be empty.",
                IdeoNameRules.Check("   ", Kept, "The ideoligion name can't be empty."));
        }

        [Fact]
        public void AnEmptyNameStillReadsWithoutTheGameText()
        {
            Assert.Contains("can't be empty", IdeoNameRules.Check(null, Kept, null));
        }

        // Two ideos with one name make `ideo=<name>` pick whichever the game
        // lists first, so the second name is refused and the existing one
        // named.
        [Fact]
        public void ADuplicateIsRefusedAndTheKeptNameIsNamed()
        {
            string problem = IdeoNameRules.Check("der pfad", Kept, "empty");
            Assert.NotNull(problem);
            Assert.Contains("Der Pfad", problem);
            Assert.Contains("ideo=<name>", problem);
        }

        [Fact]
        public void ANameIsTrimmedBeforeItIsCompared()
        {
            Assert.NotNull(IdeoNameRules.Check("  The Path  ", Kept, "empty"));
        }

        [Fact]
        public void ANameLongerThanTheDialogsLimitIsRefused()
        {
            string longName = new string('x', IdeoNameRules.CharacterLimit + 1);
            Assert.NotNull(IdeoNameRules.Check(longName, Kept, "empty"));
            Assert.Null(IdeoNameRules.Check(new string('x', IdeoNameRules.CharacterLimit), Kept, "empty"));
        }

        [Fact]
        public void AControlCharacterIsRefused()
        {
            Assert.NotNull(IdeoNameRules.Check("Der\nPfad", Kept, "empty"));
        }
    }
}
