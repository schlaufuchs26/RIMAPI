using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3490: a paused game names the hold that stopped its clock, so a
    // caller does not have to guess "an alert, a modal or a save".
    public class PauseSourceTests
    {
        [Fact]
        public void NoHoldIsNoSource()
        {
            Assert.Equal("", PauseSource.Describe(false, false, false, false, false));
        }

        [Fact]
        public void ALongEventComesFirst()
        {
            Assert.Equal(PauseSource.LongEvent, PauseSource.Describe(true, true, true, true, true));
        }

        [Fact]
        public void AWindowHoldsTheClock()
        {
            Assert.Equal(PauseSource.Window, PauseSource.Describe(false, true, false, false, true));
        }

        [Fact]
        public void TheTilePickerHoldsTheClock()
        {
            Assert.Equal(PauseSource.TilePicker, PauseSource.Describe(false, false, true, false, true));
        }

        [Fact]
        public void DebugModeHoldsTheClock()
        {
            Assert.Equal(PauseSource.Debug, PauseSource.Describe(false, false, false, true, true));
        }

        // The game's own catch-all: ForcePaused holds, and the cause is one this
        // build does not name (a gravship cutscene, a landing confirmation).
        [Fact]
        public void AnUnnamedHoldStillSaysSomethingHolds()
        {
            Assert.Equal(PauseSource.Other, PauseSource.Describe(false, false, false, false, true));
        }
    }
}
