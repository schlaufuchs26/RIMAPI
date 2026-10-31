using System.IO;
using System.Linq;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // Which targets a designation order covers (fox ticket #3425). The harvest
    // rule is the game's own designator check; the rectangle rule is what lets
    // an undesignate call find what a designate call marked.
    public class DesignationRulesTests
    {
        [Fact]
        public void ARipeStandardPlantIsAHarvestTarget()
        {
            Assert.True(DesignationRules.IsHarvestTarget(true, "Standard"));
        }

        [Fact]
        public void ATreeIsNeverAHarvestTarget()
        {
            // Trees carry harvestTag "Wood" and report HarvestableNow from 40 %
            // growth, which is how 770 of them were marked HarvestPlant.
            Assert.False(DesignationRules.IsHarvestTarget(true, "Wood"));
        }

        [Fact]
        public void AnUnripePlantIsNoHarvestTarget()
        {
            Assert.False(DesignationRules.IsHarvestTarget(false, "Standard"));
        }

        [Fact]
        public void AMissingHarvestTagIsNoHarvestTarget()
        {
            Assert.False(DesignationRules.IsHarvestTarget(true, null));
        }

        [Fact]
        public void TheSameRectangleCoversItself()
        {
            Assert.True(DesignationRules.Covers(60, 60, 60, 60, 60, 60, 200, 200));
        }

        [Fact]
        public void ATargetOnTheEdgeIsCovered()
        {
            Assert.True(DesignationRules.Covers(200, 200, 200, 200, 60, 60, 200, 200));
            Assert.False(DesignationRules.Covers(201, 200, 201, 200, 60, 60, 200, 200));
        }

        [Fact]
        public void ATargetOutsideTheRectangleIsNotCovered()
        {
            Assert.False(DesignationRules.Covers(59, 100, 59, 100, 60, 60, 200, 200));
        }

        [Fact]
        public void AMultiCellTargetIsCoveredByOneOfItsCells()
        {
            // A 2x2 table marked from one cell must clear from that cell too.
            Assert.True(DesignationRules.Covers(100, 100, 101, 101, 101, 101, 101, 101));
        }

        [Fact]
        public void CornersMayComeInAnyOrder()
        {
            Assert.True(DesignationRules.Covers(122, 82, 122, 82, 124, 84, 120, 80));
            Assert.True(DesignationRules.Covers(122, 82, 122, 82, 120, 84, 124, 80));
        }

        // The rule is only worth its test when the harvest order asks it: the
        // branch used to check HarvestableNow alone, which a tree passes.
        [Fact]
        public void TheHarvestOrderAsksTheRule()
        {
            string root = SourceTree.RepoRoot();
            string order = SourceTree
                .Sources(root, "RimworldRestApi", "Services")
                .First(path => path.EndsWith(Path.Combine("OrderService", "OrderService.cs")));
            string source = File.ReadAllText(order);

            Assert.Contains("DesignationRules.IsHarvestTarget(", source);
            Assert.DoesNotContain("if (!p.HarvestableNow) continue;", source);
        }
    }
}
