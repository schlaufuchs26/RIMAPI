using System;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// Which addresses the bridge binds (fox ticket #3767): a client that dials
    /// 127.0.0.1 used to be refused while localhost answered, because the
    /// setting "localhost" bound the IPv6 loopback alone.
    /// </summary>
    public class ListenPrefixesTests
    {
        [Fact]
        public void TheLoopbackNameBindsBothFamilies()
        {
            Assert.Equal(
                new[] { "http://localhost:8766/", "http://127.0.0.1:8766/" },
                ListenPrefixes.For("localhost", 8766)
            );
        }

        [Fact]
        public void TheIpv4AddressGetsTheNameToo()
        {
            Assert.Equal(
                new[] { "http://localhost:8765/", "http://127.0.0.1:8765/" },
                ListenPrefixes.For("127.0.0.1", 8765)
            );
        }

        [Theory]
        [InlineData("0.0.0.0")]
        [InlineData("*")]
        public void EveryInterfaceIsTheWildcardPrefix(string setting)
        {
            Assert.Equal(new[] { "http://+:8766/" }, ListenPrefixes.For(setting, 8766));
        }

        [Fact]
        public void AnotherHostKeepsItsOwnPrefix()
        {
            Assert.Equal(new[] { "http://192.168.1.5:8766/" }, ListenPrefixes.For("192.168.1.5", 8766));
        }

        [Fact]
        public void AnIpv6LiteralKeepsItsBrackets()
        {
            Assert.Equal(new[] { "http://[::1]:8766/" }, ListenPrefixes.For("::1", 8766));
            Assert.Equal(new[] { "http://[::1]:8766/" }, ListenPrefixes.For("[::1]", 8766));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AMissingAddressIsRefused(string setting)
        {
            Assert.Throws<ArgumentException>(() => ListenPrefixes.For(setting, 8766));
        }

        [Fact]
        public void AUsablePortOnly()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ListenPrefixes.For("localhost", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ListenPrefixes.For("localhost", 65536));
        }
    }
}
