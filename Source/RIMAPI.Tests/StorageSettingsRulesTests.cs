using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// A container's storage write (fox ticket #3888): the game's priority
    /// scale, and the two refusals a caller has to be able to act on. A shelf
    /// and a hopper keep the same storage tab a stockpile does, and until this
    /// route existed the tool could read a hopper's priority but not write it.
    /// </summary>
    public class StorageSettingsRulesTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public void TheGameScaleIsWritable(int priority)
        {
            Assert.True(StorageSettingsRules.PriorityInRange(priority));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        [InlineData(99)]
        public void APriorityOutsideTheScaleIsRefused(int priority)
        {
            Assert.False(StorageSettingsRules.PriorityInRange(priority));
        }

        [Fact]
        public void TheOutOfRangeSentenceNamesEveryStep()
        {
            string message = StorageSettingsRules.PriorityOutOfRange(9);

            Assert.Contains("9", message);
            Assert.Contains("1 low", message);
            Assert.Contains("5 critical", message);
        }

        [Fact]
        public void AThingWithoutAStorageTabNamesItsDefAndLabel()
        {
            string message = StorageSettingsRules.NotAStorage("Wall", "wall");

            Assert.Contains("Wall (wall)", message);
            Assert.Contains("no storage settings", message);
            // The next call needs a container id, and this is where it comes from.
            Assert.Contains("`things`", message);
        }

        [Fact]
        public void AMissingThingNamesTheIdItLookedFor()
        {
            string message = StorageSettingsRules.NoSuchThing(4711);

            Assert.Contains("4711", message);
            Assert.Contains("`things`", message);
        }
    }
}
