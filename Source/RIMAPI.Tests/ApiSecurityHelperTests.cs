using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The filter on text that reaches a letter (ticket #3316): the caller's
    /// input is untrusted, so control characters and null bytes must not land in
    /// the game's text, while German text and ordinary punctuation must survive.
    /// </summary>
    public class ApiSecurityHelperTests
    {
        [Fact]
        public void OrdinaryTextIsKeptUnchanged()
        {
            const string text = "Die Kolonie hat 12 Mahlzeiten (übrig: 3).";

            Assert.Equal(text, ApiSecurityHelper.SanitizeLetterInput(text));
        }

        [Fact]
        public void AControlCharacterIsDropped()
        {
            Assert.Equal("abc", ApiSecurityHelper.SanitizeLetterInput("a\u0001b\u0007c"));
        }

        [Fact]
        public void ANullByteIsDropped()
        {
            Assert.Equal("ab", ApiSecurityHelper.SanitizeLetterInput("a\0b"));
        }

        [Fact]
        public void NewlinesAndTabsAreKept()
        {
            Assert.Equal("a\nb\tc", ApiSecurityHelper.SanitizeLetterInput("a\nb\tc"));
        }

        [Fact]
        public void ThreeNewlinesCollapseToTwo()
        {
            Assert.Equal("a\n\nb", ApiSecurityHelper.SanitizeLetterInput("a\n\n\n\n\nb"));
        }

        [Fact]
        public void TheEdgesAreTrimmed()
        {
            Assert.Equal("hello", ApiSecurityHelper.SanitizeLetterInput("  hello \n"));
        }

        [Fact]
        public void AnEmptyOrWhitespaceOnlyInputBecomesEmpty()
        {
            Assert.Equal(string.Empty, ApiSecurityHelper.SanitizeLetterInput(null));
            Assert.Equal(string.Empty, ApiSecurityHelper.SanitizeLetterInput("   \n\t "));
        }
    }
}
