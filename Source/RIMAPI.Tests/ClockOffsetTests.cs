using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The map's clock offset (fox ticket #3412): the founding map reads 06:00
    /// at tick 0 whatever its longitude, and a map east of it reads that many
    /// hours later.
    /// </summary>
    public class ClockOffsetTests
    {
        // The game's own start tick for a colony founded in the first twelfth
        // at a tile of the given time zone (GenTicks.ConfiguredTicksAbsAtGameStart:
        // 300000 * twelfth + 2500 * (6 + (24 - tz) % 24)).
        private static int GameStartAbsTick(int foundingTimeZone)
        {
            return 2500 * (6 + (24 - foundingTimeZone) % 24);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(-7)]
        public void The_founding_map_reads_six_at_tick_zero(int timeZone)
        {
            Assert.Equal(6 * ClockOffset.TicksPerHour, ClockOffset.From(GameStartAbsTick(timeZone), timeZone));
        }

        [Fact]
        public void A_map_east_of_the_founding_one_reads_hours_later()
        {
            // Founded at longitude 0 (time zone 0), second settlement 5 hours east.
            Assert.Equal(11 * ClockOffset.TicksPerHour, ClockOffset.From(GameStartAbsTick(0), 5));
        }

        [Fact]
        public void A_map_west_of_the_founding_one_reads_hours_earlier()
        {
            // Founded at time zone +3, second settlement at -2: five hours back.
            Assert.Equal(1 * ClockOffset.TicksPerHour, ClockOffset.From(GameStartAbsTick(3), -2));
        }

        [Fact]
        public void The_offset_stays_within_one_day()
        {
            // A twelfth of the year is 300000 ticks, five whole days, so the
            // start tick of a later twelfth must not change the clock.
            int twelfthFive = 5 * 300000 + GameStartAbsTick(0);
            Assert.Equal(ClockOffset.From(GameStartAbsTick(0), 0), ClockOffset.From(twelfthFive, 0));
        }
    }
}
