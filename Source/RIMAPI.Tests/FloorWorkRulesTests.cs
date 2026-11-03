using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The per-cell verdicts of the Architect tab's two floor designators (fox
    // ticket #4316, parity wave 7). The order of the checks is the game's:
    // Designator_Smooth.CanDesignateCell and
    // Designator_RemoveFloor.CanDesignateCell.
    public class FloorWorkRulesTests
    {
        private static FloorWorkRules.SmoothCell SmoothableFloor()
        {
            return new FloorWorkRules.SmoothCell
            {
                InBounds = true,
                Fogged = false,
                TooCloseToMapEdge = false,
                AlreadyDesignated = false,
                EdificeSmoothable = false,
                EdificeBlocksFloor = false,
                HasSmoothableStone = true,
            };
        }

        [Fact]
        public void ASmoothableStoneFloorTakesTheFloorOrder()
        {
            string reason;
            Assert.Equal(
                FloorWorkRules.Verdict.Designate,
                FloorWorkRules.Smooth(SmoothableFloor(), out reason)
            );
            Assert.Null(reason);
        }

        [Fact]
        public void ASmoothableWallTakesTheWallOrder()
        {
            var cell = SmoothableFloor();
            cell.EdificeSmoothable = true;
            string reason;
            Assert.Equal(
                FloorWorkRules.Verdict.DesignateWall,
                FloorWorkRules.Smooth(cell, out reason)
            );
        }

        [Fact]
        public void APlainFloorIsRefusedWithTheGamesSentence()
        {
            var cell = SmoothableFloor();
            cell.HasSmoothableStone = false;
            string reason;
            Assert.Equal(
                FloorWorkRules.Verdict.Refused,
                FloorWorkRules.Smooth(cell, out reason)
            );
            Assert.Equal("MessageMustDesignateSmoothableSurface", reason);
        }

        [Fact]
        public void AFoggedCellIsUnseenAndNotRefused()
        {
            var cell = SmoothableFloor();
            cell.Fogged = true;
            string reason;
            Assert.Equal(FloorWorkRules.Verdict.Unseen, FloorWorkRules.Smooth(cell, out reason));
        }

        [Fact]
        public void TheEdgeAreaComesBeforeTheAlreadyCheck()
        {
            // Designator_Smooth.CanDesignateCell asks the edge area before it
            // looks at the existing designation, so a cell at the map edge that
            // is already marked still reads as the edge refusal.
            var cell = SmoothableFloor();
            cell.TooCloseToMapEdge = true;
            cell.AlreadyDesignated = true;
            string reason;
            Assert.Equal(FloorWorkRules.Verdict.Refused, FloorWorkRules.Smooth(cell, out reason));
            Assert.Equal("TooCloseToMapEdge", reason);
        }

        [Fact]
        public void AnAlreadyMarkedCellIsNotMarkedTwice()
        {
            var cell = SmoothableFloor();
            cell.AlreadyDesignated = true;
            string reason;
            Assert.Equal(FloorWorkRules.Verdict.Already, FloorWorkRules.Smooth(cell, out reason));
        }

        private static FloorWorkRules.RemoveFloorCell RemovableFloor()
        {
            return new FloorWorkRules.RemoveFloorCell
            {
                InBounds = true,
                Fogged = false,
                AlreadyDesignated = false,
                EdificeBlocksFloor = false,
                TerrainRemovable = true,
                BuildingBlocksRemoval = false,
            };
        }

        [Fact]
        public void ARemovableFloorTakesTheOrder()
        {
            string reason;
            Assert.Equal(
                FloorWorkRules.Verdict.Designate,
                FloorWorkRules.RemoveFloor(RemovableFloor(), out reason)
            );
        }

        [Fact]
        public void TerrainWithNothingToRemoveCarriesTheGamesSentence()
        {
            var cell = RemovableFloor();
            cell.TerrainRemovable = false;
            string reason;
            Assert.Equal(
                FloorWorkRules.Verdict.Refused,
                FloorWorkRules.RemoveFloor(cell, out reason)
            );
            Assert.Equal("TerrainMustBeRemovable", reason);
        }

        [Fact]
        public void ABuildingOnTheCellBlocksTheRemoval()
        {
            var cell = RemovableFloor();
            cell.BuildingBlocksRemoval = true;
            string reason;
            Assert.Equal(
                FloorWorkRules.Verdict.Refused,
                FloorWorkRules.RemoveFloor(cell, out reason)
            );
            Assert.Contains("building", reason);
        }

        [Fact]
        public void AMarkedCellIsReportedAsAlready()
        {
            var cell = RemovableFloor();
            cell.AlreadyDesignated = true;
            string reason;
            Assert.Equal(
                FloorWorkRules.Verdict.Already,
                FloorWorkRules.RemoveFloor(cell, out reason)
            );
        }
    }
}
