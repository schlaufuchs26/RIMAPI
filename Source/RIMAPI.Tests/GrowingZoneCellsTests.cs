using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // A growing zone counts its own crop (fox ticket #3805). Live, Growing
    // zone 1 read "88 cells, Plant_Rice, 83 sown, 5 unsown" while the field
    // held 83 rice plants plus a wild tall grass: the grass counted as sown
    // rice, so the field read fuller than its crop was.
    public class GrowingZoneCellsTests
    {
        [Fact]
        public void AWildPlantIsNotSownCrop()
        {
            var cells = new GrowingZoneCells("Plant_Rice");
            for (int i = 0; i < 83; i++)
                cells.AddPlant("Plant_Rice", false, false, false, false, 0.5f);
            cells.AddPlant("Plant_TallGrass", false, false, false, true, 1f);
            for (int i = 0; i < 4; i++)
                cells.AddEmpty();

            Assert.Equal(83, cells.SownCells);
            Assert.Equal(4, cells.UnsownCells);
            Assert.Equal(1, cells.ForeignCells);
            Assert.Equal(new[] { "Plant_TallGrass" }, cells.ForeignPlants);
        }

        [Fact]
        public void TheThreeCountsAddUpToTheZone()
        {
            var cells = new GrowingZoneCells("Plant_Rice");
            cells.AddPlant("Plant_Rice", false, false, false, false, 0.5f);
            cells.AddPlant("Plant_TallGrass", false, false, false, true, 1f);
            cells.AddPlant("Plant_TallGrass", false, false, false, true, 1f);
            cells.AddPlant("Plant_Bush", false, false, false, false, 0.2f);
            cells.AddEmpty();

            Assert.Equal(5, cells.SownCells + cells.UnsownCells + cells.ForeignCells);
        }

        [Fact]
        public void AForeignPlantNamesItsDefOnce()
        {
            var cells = new GrowingZoneCells("Plant_Rice");
            cells.AddPlant("Plant_TallGrass", false, false, false, true, 1f);
            cells.AddPlant("Plant_TallGrass", false, false, false, true, 1f);

            Assert.Equal(2, cells.ForeignCells);
            Assert.Equal(new[] { "Plant_TallGrass" }, cells.ForeignPlants);
        }

        [Fact]
        public void AWildPlantFeedsNoneOfTheCropCounts()
        {
            var cells = new GrowingZoneCells("Plant_Rice");
            // A blighted, ripe, mature wild bush must not make the rice field
            // read ripe or fully grown.
            cells.AddPlant("Plant_Bush", true, true, true, true, 1f);

            Assert.Equal(0, cells.SownCells);
            Assert.Equal(0, cells.DyingCells);
            Assert.Equal(0, cells.BlightedCells);
            Assert.Equal(0, cells.HarvestableCells);
            Assert.Equal(0, cells.MatureCells);
            Assert.Equal(0f, cells.MaxGrowth);
        }

        [Fact]
        public void AZoneWithoutACropCountsEveryPlantAsItsOwn()
        {
            // PlantDefName is "None" for a zone with no plant set: there is
            // nothing to compare against, so nothing reads as foreign.
            var cells = new GrowingZoneCells(null);
            cells.AddPlant("Plant_TallGrass", false, false, false, false, 0.4f);

            Assert.Equal(1, cells.SownCells);
            Assert.Equal(0, cells.ForeignCells);
        }

        [Fact]
        public void TheCropCountsItsRipeAndMaturePlants()
        {
            var cells = new GrowingZoneCells("Plant_Rice");
            cells.AddPlant("Plant_Rice", false, false, true, false, 0.73f);
            cells.AddPlant("Plant_Rice", false, false, true, true, 1f);

            Assert.Equal(2, cells.SownCells);
            Assert.Equal(2, cells.HarvestableCells);
            Assert.Equal(1, cells.MatureCells);
            Assert.Equal(1f, cells.MaxGrowth);
        }
    }
}
