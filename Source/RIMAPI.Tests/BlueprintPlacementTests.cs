using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// A def with no construction work is placed finished, the way the game's
    /// own build designator places it (fox ticket #3940: a sleeping spot stayed
    /// a blueprint until a builder got to it).
    /// </summary>
    public class BlueprintPlacementTests
    {
        [Fact]
        public void A_def_with_no_work_is_placed_finished()
        {
            Assert.True(BlueprintPlacement.PlacesFinished(0f));
        }

        [Fact]
        public void A_def_that_needs_work_is_a_blueprint()
        {
            Assert.False(BlueprintPlacement.PlacesFinished(1f));
            Assert.False(BlueprintPlacement.PlacesFinished(85f));
        }
    }
}
