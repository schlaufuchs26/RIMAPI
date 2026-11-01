using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The port override a second game is started with (fox ticket #3580).
    /// </summary>
    public class ListenPortTests
    {
        [Theory]
        [InlineData("8766", 8766)]
        [InlineData("1", 1)]
        [InlineData("65535", 65535)]
        public void AcceptsAPort(string raw, int want)
        {
            Assert.True(ListenPort.TryParse(raw, out int port));
            Assert.Equal(want, port);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("65536")]
        [InlineData("-1")]
        [InlineData("eight")]
        [InlineData("87.5")]
        public void RefusesWhatIsNotAPort(string raw)
        {
            Assert.False(ListenPort.TryParse(raw, out _));
        }

        [Fact]
        public void WithoutAnArgumentTheSavedPortStands()
        {
            Assert.Equal(8765, ListenPort.Override(null, 8765, out bool refused));
            Assert.False(refused);
            Assert.Equal(8765, ListenPort.Override(string.Empty, 8765, out bool emptyRefused));
            Assert.False(emptyRefused);
        }

        [Fact]
        public void AUsableArgumentWins()
        {
            Assert.Equal(8766, ListenPort.Override("8766", 8765, out bool refused));
            Assert.False(refused);
        }

        [Fact]
        public void AnUnusableArgumentIsRefusedAndLeavesTheSavedPort()
        {
            Assert.Equal(8765, ListenPort.Override("nope", 8765, out bool refused));
            Assert.True(refused);
        }
    }
}
