using System.IO;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The wording of the training card's refusals (fox ticket #4212, gap 5). The
    /// card shows a trick only for an animal that can learn it, so a caller that
    /// guessed must learn which names exist.
    /// </summary>
    public class AnimalTrainingRulesTests
    {
        [Fact]
        public void AnUnknownTrainableNamesTheReadThatListsThem()
        {
            string text = AnimalTrainingRules.UnknownTrainable("Flying");
            Assert.Contains("Flying", text);
            Assert.Contains("training read", text);
        }

        [Fact]
        public void AHiddenTrickNamesTheAnimal()
        {
            string text = AnimalTrainingRules.NotShown("Bess", "Rescue");
            Assert.Contains("Bess", text);
            Assert.Contains("Rescue", text);
        }

        [Fact]
        public void AnUnknownAreaNamesTheAreasThatExist()
        {
            string text = AnimalTrainingRules.UnknownArea("Garden", new[] { "Home", "Barn" });
            Assert.Contains("Garden", text);
            Assert.Contains("Home, Barn", text);
        }

        [Fact]
        public void TheOtherRefusalsNameTheirFact()
        {
            Assert.Contains("no pawn", AnimalTrainingRules.UnknownPawn(7));
            Assert.Contains("not an animal", AnimalTrainingRules.NotAnAnimal("Vulpes"));
            Assert.Contains("not on a map", AnimalTrainingRules.NoMap("Bess"));
            Assert.Contains("obedience", AnimalTrainingRules.NoMasterNeeded("Bess"));
            Assert.Contains("follow_drafted", AnimalTrainingRules.NothingToChange());
            Assert.Contains("already wanted", AnimalTrainingRules.AlreadyWanted("Hauling", true));
            Assert.Contains("not wanted", AnimalTrainingRules.AlreadyWanted("Hauling", false));
            Assert.Contains("already restricted", AnimalTrainingRules.AlreadyArea("Bess", "Home"));
            Assert.Contains("already follows", AnimalTrainingRules.AlreadyMaster("Bess", "McClain"));
        }

        [Fact]
        public void TheWritesGoThroughTheCardsOwnCalls()
        {
            // SetWantedRecursive sets the prerequisites with the trick, as the
            // checkbox does; CanBeMaster is the master button's own check. A
            // service that wrote the wanted map directly would skip both.
            string root = SourceTree.RepoRoot();
            string service = Path.Combine(
                root,
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Services",
                "Pawns",
                "AnimalTrainingService",
                "AnimalTrainingService.cs"
            );

            string text = File.ReadAllText(service);
            Assert.Contains("SetWantedRecursive(", text);
            Assert.Contains("CanBeMaster(", text);
            Assert.Contains("CanAssignToTrain(", text);
        }
    }
}
