using System.Collections.Generic;
using System.Linq;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // A grow rectangle over an existing field must join that field instead of
    // making a stray zone beside it (fox ticket #3388). The decision is plain
    // integer work, so it is checked here without a game.
    public class GrowingZoneMergeTests
    {
        private static GrowingZoneMerge.Zone Zone(int id, int x1, int z1, int x2, int z2)
        {
            return new GrowingZoneMerge.Zone
            {
                Id = id,
                Cells = GrowingZoneMerge.Rectangle(
                    new GrowingZoneMerge.Cell(x1, z1),
                    new GrowingZoneMerge.Cell(x2, z2)),
            };
        }

        private static GrowingZoneMerge.Plan Decide(
            List<GrowingZoneMerge.Cell> rectangle,
            List<GrowingZoneMerge.Zone> zones)
        {
            return GrowingZoneMerge.Decide(rectangle, _ => true, zones);
        }

        [Fact]
        public void ARectangleInsideAZoneJoinsItWithoutAddingCells()
        {
            var plan = Decide(
                GrowingZoneMerge.Rectangle(
                    new GrowingZoneMerge.Cell(2, 2),
                    new GrowingZoneMerge.Cell(3, 3)),
                new List<GrowingZoneMerge.Zone> { Zone(1, 0, 0, 9, 9) });

            Assert.Equal(1, plan.TargetZoneId);
            Assert.Empty(plan.NewCells);
            Assert.Equal(4, plan.AlreadyInTarget);
        }

        [Fact]
        public void FreeCellsOfTheRectangleGoToTheOverlappingZone()
        {
            var plan = Decide(
                GrowingZoneMerge.Rectangle(
                    new GrowingZoneMerge.Cell(8, 0),
                    new GrowingZoneMerge.Cell(11, 1)),
                new List<GrowingZoneMerge.Zone> { Zone(1, 0, 0, 9, 1) });

            Assert.Equal(1, plan.TargetZoneId);
            Assert.Equal(4, plan.AlreadyInTarget);
            Assert.Equal(new[] { "10,0", "10,1", "11,0", "11,1" }, Keys(plan.NewCells));
        }

        [Fact]
        public void TheZoneWithTheMostCellsWins()
        {
            var plan = Decide(
                GrowingZoneMerge.Rectangle(
                    new GrowingZoneMerge.Cell(0, 0),
                    new GrowingZoneMerge.Cell(9, 0)),
                new List<GrowingZoneMerge.Zone>
                {
                    Zone(7, 0, 0, 2, 0),
                    Zone(3, 3, 0, 9, 0),
                });

            Assert.Equal(3, plan.TargetZoneId);
            Assert.Equal(7, plan.AlreadyInTarget);
            Assert.Equal(3, plan.InOtherZone);
            Assert.Empty(plan.NewCells);
        }

        [Fact]
        public void ATieGoesToTheLowerIdSoTheSameRectangleAlwaysResolvesTheSameWay()
        {
            var plan = Decide(
                GrowingZoneMerge.Rectangle(
                    new GrowingZoneMerge.Cell(0, 0),
                    new GrowingZoneMerge.Cell(1, 0)),
                new List<GrowingZoneMerge.Zone>
                {
                    Zone(9, 0, 0, 0, 0),
                    Zone(4, 1, 0, 1, 0),
                });

            Assert.Equal(4, plan.TargetZoneId);
            Assert.Equal(1, plan.AlreadyInTarget);
            Assert.Equal(1, plan.InOtherZone);
        }

        [Fact]
        public void ARectangleOverNoZoneCreatesOne()
        {
            var plan = Decide(
                GrowingZoneMerge.Rectangle(
                    new GrowingZoneMerge.Cell(0, 0),
                    new GrowingZoneMerge.Cell(1, 1)),
                new List<GrowingZoneMerge.Zone> { Zone(1, 5, 5, 9, 9) });

            Assert.Null(plan.TargetZoneId);
            Assert.Equal(4, plan.NewCells.Count);
        }

        [Fact]
        public void CellsOutsideTheMapAreCountedAndNeverAdded()
        {
            var plan = GrowingZoneMerge.Decide(
                GrowingZoneMerge.Rectangle(
                    new GrowingZoneMerge.Cell(-1, 0),
                    new GrowingZoneMerge.Cell(0, 0)),
                cell => cell.X >= 0,
                new List<GrowingZoneMerge.Zone>());

            Assert.Equal(1, plan.OutOfBounds);
            Assert.Equal(new[] { "0,0" }, Keys(plan.NewCells));
        }

        [Fact]
        public void RectangleNormalizesSwappedCorners()
        {
            var cells = GrowingZoneMerge.Rectangle(
                new GrowingZoneMerge.Cell(3, 4),
                new GrowingZoneMerge.Cell(1, 2));

            Assert.Equal(new[] { "1,2", "1,3", "1,4", "2,2", "2,3", "2,4", "3,2", "3,3", "3,4" }, Keys(cells));
        }

        private static string[] Keys(IEnumerable<GrowingZoneMerge.Cell> cells)
        {
            return cells.Select(cell => cell.X + "," + cell.Z).ToArray();
        }
    }
}
