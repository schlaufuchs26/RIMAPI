using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The fog filter of the map reads (fox ticket #4220): a read answers what
    // a player can see, and the fog grid is what decides that.
    public class FogRevealTests
    {
        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void ARevealedCellShowsAndAFoggedOneDoesNot(bool fogged, bool shows)
        {
            Assert.Equal(shows, FogReveal.ShowsCell(fogged));
        }

        [Fact]
        public void AThingShowsWhenOneOfItsCellsIsRevealed()
        {
            // A two-cell wall half in the dark: the game draws the lit half, so
            // the read must not drop the building.
            Assert.True(FogReveal.ShowsAny(new[] { true, false }));
        }

        [Fact]
        public void AThingWholeInTheDarkIsHidden()
        {
            Assert.False(FogReveal.ShowsAny(new[] { true, true }));
        }

        [Fact]
        public void AThingWithNoCellIsHidden()
        {
            // A despawned thing occupies nothing the player could have seen.
            Assert.False(FogReveal.ShowsAny(new bool[0]));
            Assert.False(FogReveal.ShowsAny(null));
        }
    }
}
