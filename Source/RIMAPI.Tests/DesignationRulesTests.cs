using System;
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
        public void ChopTakesATreeAndNothingElse()
        {
            // The chop order is the wood order: a blighted rice plant is not a
            // tree, which is why two chop sweeps over the field marked only the
            // trees while the blight doubled (fox ticket #3530).
            Assert.True(DesignationRules.IsChopTarget(true));
            Assert.False(DesignationRules.IsChopTarget(false));
        }

        [Fact]
        public void TheBlightOrderTakesEveryBlightedPlant()
        {
            // Tree or crop, blight is cured by cutting only.
            Assert.True(DesignationRules.IsBlightTarget(true));
            Assert.False(DesignationRules.IsBlightTarget(false));
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

        // The blight count rides along in the plant order's own walk, so a
        // map-wide sweep reads each cell's thing list once instead of twice
        // (fox ticket #3562).
        [Fact]
        public void TheBlightCountSharesThePlantOrdersWalk()
        {
            string root = SourceTree.RepoRoot();
            string order = SourceTree
                .Sources(root, "RimworldRestApi", "Services")
                .First(path => path.EndsWith(Path.Combine("OrderService", "OrderService.cs")));
            string source = File.ReadAllText(order);

            // The plant order counts the blight in the walk it takes anyway.
            Assert.Contains("if (plant.Blighted) blighted++;", source);
            // Counted before the order's own target rule: a chop sweep marks
            // trees only and still has to report the blighted crop beside them.
            Assert.True(source.IndexOf("if (plant.Blighted) blighted++;", StringComparison.Ordinal)
                < source.IndexOf("bool wanted", StringComparison.Ordinal));
            // The pass of its own survives behind the guard for the orders that
            // never walk plants, so no cell's thing list is read twice.
            Assert.Contains("if (!plantOrder)", source);
            Assert.True(source.IndexOf("if (!plantOrder)", StringComparison.Ordinal)
                < source.IndexOf("var blightPlant", StringComparison.Ordinal));
        }
    }
}

namespace RIMAPI.Tests
{
    using RIMAPI.Helpers;
    using Xunit;

    // fox #3625: the order word decides which designation a designate call
    // leaves behind, and that designation decides which work giver hands the
    // job out. Chop, cut and blight are the same CutPlant order.
    public class DesignationOrderTests
    {
        [Theory]
        [InlineData("chop", "CutPlant")]
        [InlineData("Chop", "CutPlant")]
        [InlineData("cut", "CutPlant")]
        [InlineData("blight", "CutPlant")]
        [InlineData("harvest", "HarvestPlant")]
        [InlineData("mine", "Mine")]
        [InlineData("deconstruct", "Deconstruct")]
        [InlineData("hunt", "Hunt")]
        public void TheOrderWordNamesItsDesignation(string type, string defName)
        {
            Assert.Equal(defName, DesignationRules.DefNameForOrder(type));
        }

        [Theory]
        [InlineData("remove-all")]
        [InlineData("plant")]
        [InlineData("")]
        [InlineData(null)]
        public void AnOrderWithoutADesignationNamesNone(string type)
        {
            Assert.Null(DesignationRules.DefNameForOrder(type));
        }
    }
}
