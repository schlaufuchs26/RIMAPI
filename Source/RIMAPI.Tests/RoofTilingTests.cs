using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // Where the build-roof jobs of a rectangle stand (fox ticket #3418). A roof
    // job roofs the 3x3 block around its target cell, so the tiling has to cover
    // the rectangle without a gap; plain integers, so this runs without a game.
    public class RoofTilingTests
    {
        // Every cell of the rectangle lies in the 3x3 block of one job cell.
        private static void AssertCovered(int x1, int z1, int x2, int z2)
        {
            List<RoofTiling.Cell> jobs = RoofTiling.JobCells(x1, z1, x2, z2);
            int minX = x1 < x2 ? x1 : x2, maxX = x1 < x2 ? x2 : x1;
            int minZ = z1 < z2 ? z1 : z2, maxZ = z1 < z2 ? z2 : z1;
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    bool covered = false;
                    foreach (RoofTiling.Cell job in jobs)
                    {
                        if (x >= job.X - 1 && x <= job.X + 1 && z >= job.Z - 1 && z <= job.Z + 1)
                        {
                            covered = true;
                            break;
                        }
                    }
                    Assert.True(covered, $"({x},{z}) of {x1},{z1}-{x2},{z2} is in no job's block");
                }
            }
        }

        [Fact]
        public void AFiveByFiveRoomTakesFourJobs()
        {
            List<RoofTiling.Cell> jobs = RoofTiling.JobCells(108, 116, 112, 120);

            Assert.Equal(4, jobs.Count);
            Assert.Equal("108,116", jobs[0].ToString());
            Assert.Equal("111,116", jobs[1].ToString());
            Assert.Equal("108,119", jobs[2].ToString());
            Assert.Equal("111,119", jobs[3].ToString());
            AssertCovered(108, 116, 112, 120);
        }

        [Fact]
        public void ASevenBySevenRoomWithItsWallsTakesNineJobs()
        {
            Assert.Equal(9, RoofTiling.JobCells(108, 116, 114, 122).Count);
            AssertCovered(108, 116, 114, 122);
        }

        [Fact]
        public void OneCellTakesOneJobOnThatCell()
        {
            List<RoofTiling.Cell> jobs = RoofTiling.JobCells(111, 120, 111, 120);

            Assert.Single(jobs);
            Assert.Equal(111, jobs[0].X);
            Assert.Equal(120, jobs[0].Z);
        }

        [Fact]
        public void TheCornersMayComeInAnyOrder()
        {
            List<RoofTiling.Cell> forward = RoofTiling.JobCells(108, 116, 112, 120);
            List<RoofTiling.Cell> reversed = RoofTiling.JobCells(112, 120, 108, 116);

            Assert.Equal(forward.Count, reversed.Count);
            for (int i = 0; i < forward.Count; i++)
            {
                Assert.Equal(forward[i].ToString(), reversed[i].ToString());
            }
        }

        [Fact]
        public void EveryRectangleUpToTwelveCellsIsCovered()
        {
            for (int w = 1; w <= 12; w++)
            {
                for (int h = 1; h <= 12; h++)
                {
                    AssertCovered(200, 140, 200 + w - 1, 140 + h - 1);
                }
            }
        }
    }
}
