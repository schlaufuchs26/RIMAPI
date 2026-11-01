using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // How many ticks a colony has run since it loaded (fox ticket #3781): the
    // power figures are written by the game's tick, and the tick the loader
    // itself runs still reads 0 W produced of 1700 W installed.
    public class LoadedTickTests
    {
        [Fact]
        public void WithoutALoadTheAnswerIsMinusOne()
        {
            LoadedTick.Clear();

            Assert.Equal(-1, LoadedTick.Since(1200));
        }

        [Fact]
        public void TheLoadTickItselfIsZeroTicksSince()
        {
            LoadedTick.Clear();
            LoadedTick.Record(1200);

            Assert.Equal(0, LoadedTick.Since(1200));
        }

        [Fact]
        public void TheAnswerCountsTheTicksSinceTheLoad()
        {
            LoadedTick.Clear();
            LoadedTick.Record(1200);

            Assert.Equal(2500, LoadedTick.Since(3700));
        }

        [Fact]
        public void AClockBeforeTheLoadIsNoMeasurement()
        {
            LoadedTick.Clear();
            LoadedTick.Record(1200);

            Assert.Equal(-1, LoadedTick.Since(1199));
        }

        [Fact]
        public void ClearForgetsTheLoad()
        {
            LoadedTick.Clear();
            LoadedTick.Record(1200);

            LoadedTick.Clear();

            Assert.Equal(-1, LoadedTick.Since(3700));
        }
    }
}
