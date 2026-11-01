using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The cells a screenshot covers (fox ticket #4025): the camera's centre
    // and zoom decide the picture, so the numbers are plain arithmetic and are
    // checked here without a game.
    public class CameraViewTests
    {
        [Fact]
        public void AViewInsideTheMapCoversTheCellsItsAspectShows()
        {
            // 1280x720 at RootSize 20.38: the view is 40.76 cells high and
            // 72.46 wide, so the game's own view rect around (147.6, 109.5)
            // is x110-184, z88-130 (one cell of slack at the low edge).
            var view = CameraViewRules.Visible(147.6f, 109.5f, 20.38f, 1280, 720, 250, 250);

            Assert.True(view.HasCells);
            Assert.Equal(110, view.X1);
            Assert.Equal(88, view.Z1);
            Assert.Equal(184, view.X2);
            Assert.Equal(130, view.Z2);
            Assert.Equal(20.38f, view.RootSize);
        }

        [Fact]
        public void AViewAtTheMapEdgeIsCutToTheMap()
        {
            var view = CameraViewRules.Visible(2f, 3f, 10f, 1000, 1000, 250, 250);

            Assert.True(view.HasCells);
            Assert.Equal(0, view.X1);
            Assert.Equal(0, view.Z1);
            Assert.Equal(12, view.X2);
            Assert.Equal(13, view.Z2);
        }

        [Fact]
        public void AViewPastTheMapHasNoCells()
        {
            var view = CameraViewRules.Visible(-40f, -40f, 5f, 800, 800, 250, 250);

            Assert.False(view.HasCells);
            Assert.Equal(0, view.X1);
            Assert.Equal(-35, view.X2);
        }

        [Fact]
        public void AWidePictureShowsMoreCellsThanATallOne()
        {
            var wide = CameraViewRules.Visible(100f, 100f, 10f, 1920, 720, 500, 500);
            var tall = CameraViewRules.Visible(100f, 100f, 10f, 720, 1920, 500, 500);

            Assert.True(wide.X2 - wide.X1 > tall.X2 - tall.X1);
            Assert.Equal(wide.Z2 - wide.Z1, tall.Z2 - tall.Z1);
        }

        [Fact]
        public void AViewWithoutAZoomNamesNoCells()
        {
            var view = CameraViewRules.Visible(100f, 100f, 0f, 1280, 720, 250, 250);

            Assert.False(view.HasCells);
        }
    }
}
