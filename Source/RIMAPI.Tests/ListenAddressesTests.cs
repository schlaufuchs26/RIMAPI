using System.Collections.Generic;
using System.Net;
using RIMAPI.Core;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// Which sockets the door in front of the listener opens (fox ticket
    /// #4233): the setting names what a client may dial, and the door binds
    /// exactly that.
    /// </summary>
    public class ListenAddressesTests
    {
        [Fact]
        public void TheLoopbackNameBindsBothFamilies()
        {
            IReadOnlyList<IPEndPoint> endpoints = ListenAddresses.For("localhost", 8766);

            Assert.Equal(2, endpoints.Count);
            Assert.Contains(endpoints, e => e.Address.Equals(IPAddress.Loopback) && e.Port == 8766);
            Assert.Contains(endpoints, e => e.Address.Equals(IPAddress.IPv6Loopback) && e.Port == 8766);
        }

        [Fact]
        public void TheIpv4AddressBindsTheSameTwoWithoutRepeating()
        {
            IReadOnlyList<IPEndPoint> endpoints = ListenAddresses.For("127.0.0.1", 8765);

            Assert.Equal(2, endpoints.Count);
        }

        [Theory]
        [InlineData("0.0.0.0")]
        [InlineData("*")]
        public void EveryInterfaceIsOneWildcardSocket(string setting)
        {
            IReadOnlyList<IPEndPoint> endpoints = ListenAddresses.For(setting, 8766);

            IPEndPoint only = Assert.Single(endpoints);
            Assert.Equal(IPAddress.IPv6Any, only.Address);
            Assert.Equal(8766, only.Port);
        }

        [Fact]
        public void AnotherAddressKeepsItsOwn()
        {
            IReadOnlyList<IPEndPoint> endpoints = ListenAddresses.For("192.168.1.5", 8766);

            IPEndPoint only = Assert.Single(endpoints);
            Assert.Equal(IPAddress.Parse("192.168.1.5"), only.Address);
        }

        [Fact]
        public void AnIpv6LiteralKeepsItsAddress()
        {
            IReadOnlyList<IPEndPoint> endpoints = ListenAddresses.For("[::1]", 8766);

            IPEndPoint only = Assert.Single(endpoints);
            Assert.Equal(IPAddress.IPv6Loopback, only.Address);
        }
    }
}
