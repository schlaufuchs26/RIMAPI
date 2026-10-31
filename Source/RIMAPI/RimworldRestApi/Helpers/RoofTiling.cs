using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Where the build-roof jobs of a rectangle should stand (fox ticket
    /// #3418). A roof is pawn work: the game's own work giver scans the Build
    /// roof area, and the driver roofs the 3x3 block around its target cell
    /// (JobDriver_BuildRoof.DoEffect), so a room needs one job per 3x3 block,
    /// not one per cell. Blocks are laid from the rectangle's low corner, three
    /// cells apart, which tiles it without a gap: centres x1, x1+3, x1+6 roof
    /// x1-1 .. x1+7. A width that leaves the last two cells open gets one more
    /// centre one cell before its high edge, so the last block still covers it.
    ///
    /// Plain integers, so the tiling is checked without a game; the caller
    /// turns the cells into IntVec3 and asks the game whether a pawn may build
    /// there.
    /// </summary>
    public static class RoofTiling
    {
        public const int Stride = 3;

        public struct Cell
        {
            public int X;
            public int Z;

            public override string ToString()
            {
                return X + "," + Z;
            }
        }

        /// <summary>
        /// The job cells of a rectangle, row by row, low corner first. The
        /// corners may come in any order.
        /// </summary>
        public static List<Cell> JobCells(int x1, int z1, int x2, int z2)
        {
            int minX = x1 < x2 ? x1 : x2;
            int maxX = x1 < x2 ? x2 : x1;
            int minZ = z1 < z2 ? z1 : z2;
            int maxZ = z1 < z2 ? z2 : z1;

            List<int> xs = Centres(minX, maxX);
            List<int> zs = Centres(minZ, maxZ);
            var cells = new List<Cell>(xs.Count * zs.Count);
            foreach (int z in zs)
            {
                foreach (int x in xs)
                {
                    cells.Add(new Cell { X = x, Z = z });
                }
            }
            return cells;
        }

        // Centres answers block centres whose 3x3 blocks cover min..max.
        private static List<int> Centres(int min, int max)
        {
            var centres = new List<int>();
            for (int c = min; c <= max; c += Stride)
            {
                centres.Add(c);
            }
            if (centres[centres.Count - 1] + 1 < max)
            {
                centres.Add(max - 1);
            }
            return centres;
        }
    }
}
