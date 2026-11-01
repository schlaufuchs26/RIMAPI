using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3603: a release writes the colony back into the save it was loaded
    // from, so a probe copy never lands in the live colony's file.
    public class SaveNameRulesTests
    {
        [Fact]
        public void ANameIsItself()
        {
            Assert.Equal("fox-exit", SaveNameRules.Normalize("fox-exit"));
        }

        [Fact]
        public void TheSuffixIsDroppedInEitherCase()
        {
            Assert.Equal("archive-probe", SaveNameRules.Normalize("archive-probe.rws"));
            Assert.Equal("archive-probe", SaveNameRules.Normalize("archive-probe.RWS"));
        }

        [Fact]
        public void AFolderIsDropped()
        {
            Assert.Equal(
                "Asbutora World's Confederacy (Permadeath)",
                SaveNameRules.Normalize("/Users/fox/Library/Application Support/RimWorld/Saves/Asbutora World's Confederacy (Permadeath).rws"));
        }

        [Fact]
        public void SurroundingSpaceIsTrimmed()
        {
            Assert.Equal("fox-exit", SaveNameRules.Normalize("  fox-exit.rws  "));
        }

        [Fact]
        public void NothingNamesNothing()
        {
            Assert.Equal("", SaveNameRules.Normalize(""));
            Assert.Equal("", SaveNameRules.Normalize(null));
            Assert.Equal("", SaveNameRules.Normalize("   "));
        }

        [Fact]
        public void AnOrdinaryColonyLetsTheCallerPickTheName()
        {
            Assert.Equal("", SaveNameRules.WrittenName(false, "fox-exit"));
            Assert.Equal("", SaveNameRules.WrittenName(false, ""));
        }

        [Fact]
        public void ACommitmentColonyWritesItsOwnName()
        {
            Assert.Equal("Asbutora World's Confederacy (Permadeath)",
                SaveNameRules.WrittenName(true, "Asbutora World's Confederacy (Permadeath)"));
        }

        [Fact]
        public void ACommitmentColonyWithoutAUniqueNameAnswersNothing()
        {
            Assert.Equal("", SaveNameRules.WrittenName(true, null));
        }

        [Fact]
        public void TheLoadedSaveIsBareAndCanBeCleared()
        {
            LoadedSave.Set("/Saves/archive-probe.rws");
            Assert.Equal("archive-probe", LoadedSave.Current);
            LoadedSave.Clear();
            Assert.Equal("", LoadedSave.Current);
        }
    }
}
