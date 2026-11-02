using System.Collections.Generic;
using System.Linq;
using RIMAPI.Helpers;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    // The designate route that takes cells instead of a rectangle (fox ticket
    // #4095). The sweep itself needs a live map, so what is testable here is
    // the part that decides which cells go out: a repeated cell would be
    // counted twice and read as "already marked" on its second visit.
    public class DesignationCellsTests
    {
        [Fact]
        public void ARepeatedCellIsMarkedOnce()
        {
            List<PositionDto> cells = DesignationCells.Unique(new List<PositionDto>
            {
                new PositionDto { X = 100, Z = 100 },
                new PositionDto { X = 101, Z = 100 },
                new PositionDto { X = 100, Z = 100 },
            });

            Assert.Equal(2, cells.Count);
            Assert.Equal(100, cells[0].X);
            Assert.Equal(101, cells[1].X);
        }

        [Fact]
        public void TheCallersOrderIsKept()
        {
            // The answer names the cells in the order they were marked, so a
            // sweep over a zone reads the way the save lists the zone.
            List<PositionDto> cells = DesignationCells.Unique(new List<PositionDto>
            {
                new PositionDto { X = 131, Z = 130 },
                new PositionDto { X = 130, Z = 130 },
            });

            Assert.Equal(new[] { 131, 130 }, cells.Select(c => c.X).ToArray());
        }

        [Fact]
        public void CellsThatDifferOnlyInYAreTheSameCell()
        {
            // The map is flat: y is the altitude layer, so two entries that
            // differ only in y are one cell.
            List<PositionDto> cells = DesignationCells.Unique(new List<PositionDto>
            {
                new PositionDto { X = 100, Y = 0, Z = 100 },
                new PositionDto { X = 100, Y = 3, Z = 100 },
            });

            Assert.Single(cells);
        }

        [Fact]
        public void AnEmptyOrMissingListStaysEmpty()
        {
            Assert.Empty(DesignationCells.Unique(null));
            Assert.Empty(DesignationCells.Unique(new List<PositionDto>()));
        }

        [Fact]
        public void TheRouteExistsAndTakesABody()
        {
            // A route the router does not see is a 404 at the caller, and the
            // client then falls back to the rectangle it was trying to avoid.
            string root = SourceTree.RepoRoot();
            string source = System.IO.File.ReadAllText(System.IO.Path.Combine(
                root, "Source", "RIMAPI", "RimworldRestApi", "Controllers", "Colony", "OrderController.cs"));

            Assert.Contains("[Post(\"/api/v1/order/designate/cells\")]", source);
            Assert.Contains("DesignateCellsRequestDto", source);
        }
    }
}
