using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The rules behind `cooler` (fox tickets #3744, #3834): which targets the
    // game's own temperature scale holds, whether a setting took, and which
    // side of a wall-mounted cooler is the one it chills.
    public class TemperatureTargetRulesTests
    {
        [Theory]
        [InlineData(-5f)]
        [InlineData(0f)]
        [InlineData(21f)]
        [InlineData(-273f)]
        [InlineData(1000f)]
        public void ATargetOnTheGamesScaleIsAccepted(float target)
        {
            Assert.True(TemperatureTargetRules.IsOnScale(target));
            Assert.Null(TemperatureTargetRules.Problem(target));
        }

        [Theory]
        [InlineData(-274f)]
        [InlineData(1001f)]
        public void ATargetPastTheScaleIsRefusedWithTheRange(float target)
        {
            Assert.False(TemperatureTargetRules.IsOnScale(target));
            string problem = TemperatureTargetRules.Problem(target);
            Assert.Contains("-273", problem);
            Assert.Contains("1000", problem);
        }

        [Fact]
        public void ANonNumberIsNoTemperature()
        {
            // A NaN or an infinity reaching the game would compare false against
            // every room and leave the machine running at nothing.
            Assert.False(TemperatureTargetRules.IsOnScale(float.NaN));
            Assert.False(TemperatureTargetRules.IsOnScale(float.PositiveInfinity));
            Assert.False(TemperatureTargetRules.IsOnScale(float.NegativeInfinity));
            Assert.NotNull(TemperatureTargetRules.Problem(float.NaN));
        }

        [Fact]
        public void TheSettingTookWhenTheGameReadsItBack()
        {
            Assert.True(TemperatureTargetRules.Took(-5f, -5f));
            Assert.True(TemperatureTargetRules.Took(-5f, -5.01f));
            // A machine the game clamps or refuses reports its old value.
            Assert.False(TemperatureTargetRules.Took(-5f, 21f));
        }

        [Theory]
        // Rot4.AsInt: 0 north, 1 east, 2 south, 3 west. The cold cell is
        // IntVec3.South rotated by the rotation, the vent cell IntVec3.North
        // rotated by it, exactly as Building_Cooler.TickRare computes them.
        [InlineData(0, 100, 100, 100, 99, 100, 101)]
        [InlineData(1, 100, 100, 99, 100, 101, 100)]
        [InlineData(2, 100, 100, 100, 101, 100, 99)]
        [InlineData(3, 100, 100, 101, 100, 99, 100)]
        public void TheCoolerChillsOneSideAndVentsIntoTheOther(
            int rotation,
            int x,
            int z,
            int coldX,
            int coldZ,
            int ventX,
            int ventZ
        )
        {
            // The measured case of fox ticket #3834: a cooler built into the
            // east wall of a freezer, cold side west, reads rotation east. A
            // read that picked the colder neighbour named the outdoors, so the
            // sides have to come from the rotation.
            TemperatureTargetRules.CoolerCells cells = TemperatureTargetRules.CoolerSideCells(
                x,
                z,
                rotation
            );
            Assert.Equal(coldX, cells.ColdX);
            Assert.Equal(coldZ, cells.ColdZ);
            Assert.Equal(ventX, cells.VentX);
            Assert.Equal(ventZ, cells.VentZ);
        }

        [Fact]
        public void TheTwoSidesAreAlwaysOppositeEachOther()
        {
            for (int rotation = 0; rotation < 4; rotation++)
            {
                TemperatureTargetRules.CoolerCells cells = TemperatureTargetRules.CoolerSideCells(
                    50,
                    60,
                    rotation
                );
                Assert.Equal(50, (cells.ColdX + cells.VentX) / 2);
                Assert.Equal(60, (cells.ColdZ + cells.VentZ) / 2);
                Assert.True(
                    cells.ColdX != cells.VentX || cells.ColdZ != cells.VentZ,
                    "rotation " + rotation + " puts both sides on one cell"
                );
            }
        }
    }
}
