using System;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The map cells a screenshot covers (fox ticket #4025). The game's own
    /// camera decides the picture, and a check that compares the map art with
    /// the screen needs to know which cells the pixels show: the camera's
    /// centre and its zoom are the whole answer, so a caller reads them next
    /// to the picture instead of guessing from a save file.
    ///
    /// RootSize is half the view height in cells, and the view is as wide as
    /// the picture's aspect makes it. The rectangle follows the game's own
    /// CameraDriver.CurrentViewRect, which keeps one cell of slack at the low
    /// edge, and is cut to the map, because a camera at the edge sees part of
    /// the void.
    /// </summary>
    public static class CameraViewRules
    {
        public struct View
        {
            public float RootX;
            public float RootZ;
            public float RootSize;
            public int X1;
            public int Z1;
            public int X2;
            public int Z2;
            // False when the camera looks past the map, so the answer names the
            // camera without inventing cells.
            public bool HasCells;
        }

        public static View Visible(
            float centerX,
            float centerZ,
            float rootSize,
            int width,
            int height,
            int mapWidth,
            int mapHeight
        )
        {
            var view = new View { RootX = centerX, RootZ = centerZ, RootSize = rootSize };

            if (width <= 0 || height <= 0 || rootSize <= 0)
                return view;

            float aspect = (float)width / height;
            int x1 = (int)Math.Floor(centerX - rootSize * aspect - 1f);
            int x2 = (int)Math.Ceiling(centerX + rootSize * aspect);
            int z1 = (int)Math.Floor(centerZ - rootSize - 1f);
            int z2 = (int)Math.Ceiling(centerZ + rootSize);

            if (mapWidth > 0)
            {
                x1 = Math.Max(0, x1);
                x2 = Math.Min(mapWidth - 1, x2);
            }
            if (mapHeight > 0)
            {
                z1 = Math.Max(0, z1);
                z2 = Math.Min(mapHeight - 1, z2);
            }

            view.X1 = x1;
            view.Z1 = z1;
            view.X2 = x2;
            view.Z2 = z2;
            view.HasCells = x2 >= x1 && z2 >= z1;
            return view;
        }
    }
}
