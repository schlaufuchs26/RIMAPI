using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The rules behind `cooler` (fox ticket #3744): which targets the game's
    // own temperature scale holds, whether a setting took, and which of the
    // rooms beside a wall-mounted cooler is the one it chills.
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

        [Fact]
        public void TheColdestRoomIsTheOneTheCoolerChills()
        {
            var rooms = new List<TemperatureTargetRules.RoomReading>
            {
                new TemperatureTargetRules.RoomReading { Id = 12, Label = "freezer", Temperature = 16.4f },
                new TemperatureTargetRules.RoomReading { Id = 13, Label = "corridor", Temperature = 22f },
            };
            Assert.Equal(0, TemperatureTargetRules.ColdestIndex(rooms));
        }

        [Fact]
        public void TheColdestRoomIsTheSecondWhenTheFirstIsWarmer()
        {
            // The exhaust side may come first in the list; the order the cells
            // are walked must not decide the answer.
            var rooms = new List<TemperatureTargetRules.RoomReading>
            {
                new TemperatureTargetRules.RoomReading { Id = 13, Label = "corridor", Temperature = 22f },
                new TemperatureTargetRules.RoomReading { Id = 12, Label = "freezer", Temperature = -4f },
            };
            Assert.Equal(1, TemperatureTargetRules.ColdestIndex(rooms));
        }

        [Fact]
        public void NoRoomsMeansNoColdestRoom()
        {
            // A cooler built into an outer wall has nothing on one side.
            Assert.Equal(-1, TemperatureTargetRules.ColdestIndex(null));
            Assert.Equal(-1, TemperatureTargetRules.ColdestIndex(new List<TemperatureTargetRules.RoomReading>()));
        }
    }
}
