using System;
using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The prefixes the bridge's HttpListener binds (fox ticket #3767).
    /// <para>A setting of "localhost" makes HttpListener bind that name, and on
    /// macOS it resolves to ::1 alone: the game answers on localhost while a
    /// client that dials 127.0.0.1 is refused, which reads as a dead bridge.
    /// So a loopback setting binds both loopback addresses, each as its own
    /// prefix, because HttpListener matches a request's Host header against the
    /// prefix it arrived on.</para>
    /// </summary>
    public static class ListenPrefixes
    {
        /// <summary>The wildcard HttpListener accepts for every interface.</summary>
        public const string AllInterfaces = "+";

        /// <summary>
        /// The prefixes for one setting: every interface when the setting asks
        /// for it, both loopback names for a loopback setting, otherwise the
        /// setting's own host.
        /// </summary>
        public static IReadOnlyList<string> For(string serverIp, int port)
        {
            if (string.IsNullOrEmpty(serverIp))
            {
                throw new ArgumentException("a listen address is needed", nameof(serverIp));
            }
            if (port < 1 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), port, "a port is 1-65535");
            }

            string host = serverIp.Trim();
            if (host == "0.0.0.0" || host == "*")
            {
                return new[] { Prefix(AllInterfaces, port) };
            }
            if (host == "localhost" || host == "127.0.0.1")
            {
                return new[] { Prefix("localhost", port), Prefix("127.0.0.1", port) };
            }
            // A literal IPv6 address keeps its brackets in the prefix.
            if (host.Contains(":"))
            {
                return new[] { Prefix("[" + host.Trim('[', ']') + "]", port) };
            }
            return new[] { Prefix(host, port) };
        }

        private static string Prefix(string host, int port)
        {
            return $"http://{host}:{port}/";
        }
    }
}
