using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // When the power grid last ticked (fox ticket #3781): the power read reports
    // the last tick's figures, so a map that never ticked has to answer -1 and
    // not a figure that looks measured.
    public class PowerTickTests
    {
        [Fact]
        public void AMapThatNeverTickedAnswersMinusOne()
        {
            PowerTick.Clear();

            Assert.Equal(-1, PowerTick.Last(7));
        }

        [Fact]
        public void RecordKeepsTheLatestTickPerMap()
        {
            PowerTick.Clear();

            PowerTick.Record(7, 1200);
            PowerTick.Record(7, 1230);
            PowerTick.Record(8, 90);

            Assert.Equal(1230, PowerTick.Last(7));
            Assert.Equal(90, PowerTick.Last(8));
        }

        // A loaded colony keeps its map ids from the save, so the ticks of the
        // game that ran before the load would claim a grid this load never
        // measured.
        [Fact]
        public void ClearForgetsEveryMap()
        {
            PowerTick.Clear();
            PowerTick.Record(7, 1200);

            PowerTick.Clear();

            Assert.Equal(-1, PowerTick.Last(7));
        }

        // A tick of 0 is a measurement, not the absence of one: a colony started
        // from scratch records it on its first power update.
        [Fact]
        public void TickZeroIsAMeasurement()
        {
            PowerTick.Clear();

            PowerTick.Record(3, 0);

            Assert.Equal(0, PowerTick.Last(3));
        }
    }
}
