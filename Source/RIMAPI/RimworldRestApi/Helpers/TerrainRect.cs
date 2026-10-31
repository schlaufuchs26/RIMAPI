using System;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The rectangle arithmetic of the terrain read, kept free of RimWorld
    /// types so the unit tests run without a game (fox ticket #3381). The
    /// whole-map grid is about 30 kB even run-length encoded, and a caller that
    /// needs the ground under a handful of cells (a fire read) should not pay
    /// for the rest of the map.
    /// </summary>
    public static class TerrainRect
    {
        public struct Box
        {
            public int X1;
            public int Z1;
            public int X2;
            public int Z2;

            public int Width
            {
                get { return X2 - X1 + 1; }
            }

            public int Height
            {
                get { return Z2 - Z1 + 1; }
            }

            public override string ToString()
            {
                return X1 + "," + Z1 + "-" + X2 + "," + Z2;
            }
        }

        /// <summary>
        /// Narrows a requested rectangle to a map of mapWidth by mapHeight
        /// cells. The corners may come in any order, the way the plant read
        /// takes them. False when the rectangle misses the map entirely: an
        /// empty grid would read as a map without terrain.
        /// </summary>
        public static bool Clip(
            int mapWidth,
            int mapHeight,
            int x1,
            int z1,
            int x2,
            int z2,
            out Box box
        )
        {
            box = default(Box);
            if (mapWidth <= 0 || mapHeight <= 0)
            {
                return false;
            }

            int minX = Math.Max(0, Math.Min(x1, x2));
            int minZ = Math.Max(0, Math.Min(z1, z2));
            int maxX = Math.Min(mapWidth - 1, Math.Max(x1, x2));
            int maxZ = Math.Min(mapHeight - 1, Math.Max(z1, z2));

            if (minX > maxX || minZ > maxZ)
            {
                return false;
            }

            box = new Box { X1 = minX, Z1 = minZ, X2 = maxX, Z2 = maxZ };
            return true;
        }
    }
}
