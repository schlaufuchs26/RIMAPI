using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The terrain read narrows its grid to a rectangle (fox ticket #3381).
    // Clipping is plain integer work, so it is checked here without a game.
    public class TerrainRectClipTests
    {
        private static TerrainRect.Box Clip(int w, int h, int x1, int z1, int x2, int z2)
        {
            TerrainRect.Box box;
            Assert.True(TerrainRect.Clip(w, h, x1, z1, x2, z2, out box), "the rectangle should lie on the map");
            return box;
        }

        [Fact]
        public void ARectangleInsideTheMapIsKeptAsGiven()
        {
            var box = Clip(250, 250, 203, 141, 206, 201);

            Assert.Equal(203, box.X1);
            Assert.Equal(141, box.Z1);
            Assert.Equal(206, box.X2);
            Assert.Equal(201, box.Z2);
            Assert.Equal(4, box.Width);
            Assert.Equal(61, box.Height);
        }

        [Fact]
        public void ARectangleHangingOverTheEdgeIsCutToTheMap()
        {
            var box = Clip(250, 250, 240, 0, 300, 10);

            Assert.Equal(240, box.X1);
            Assert.Equal(249, box.X2);
            Assert.Equal(10, box.Width);
        }

        [Fact]
        public void CornersInAnyOrderDescribeTheSameRectangle()
        {
            var forward = Clip(50, 50, 3, 4, 7, 9);
            var reversed = Clip(50, 50, 7, 9, 3, 4);

            Assert.Equal(forward.ToString(), reversed.ToString());
        }

        [Fact]
        public void ARectangleBesideTheMapIsRefusedInsteadOfComingBackEmpty()
        {
            TerrainRect.Box box;
            Assert.False(TerrainRect.Clip(250, 250, 300, 10, 320, 20, out box));
            Assert.False(TerrainRect.Clip(250, 250, -20, -20, -1, -1, out box));
            // A map of no cells has no rectangle to give.
            Assert.False(TerrainRect.Clip(0, 0, 0, 0, 0, 0, out box));
        }
    }
}
