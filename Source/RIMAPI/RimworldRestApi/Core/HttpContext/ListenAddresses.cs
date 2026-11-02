using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using RIMAPI.Helpers;

namespace RIMAPI.Core
{
    /// <summary>
    /// The addresses the door in front of the listener binds (fox ticket #4233).
    /// </summary>
    /// <remarks>
    /// The setting names what a client may dial, and ListenPrefixes turns it
    /// into listener prefixes; the same setting decides which sockets the front
    /// opens, so the two cannot drift apart. The listener itself binds nothing
    /// but its own loopback port.
    /// </remarks>
    public static class ListenAddresses
    {
        public static IReadOnlyList<IPEndPoint> For(string serverIp, int port)
        {
            var endpoints = new List<IPEndPoint>();
            foreach (string prefix in ListenPrefixes.For(serverIp, port))
            {
                Add(prefix, port, endpoints);
            }
            return endpoints;
        }

        private static void Add(string prefix, int port, List<IPEndPoint> endpoints)
        {
            string host = HostOf(prefix);

            if (host == ListenPrefixes.AllInterfaces)
            {
                // IPv6Any listens for both families unless the socket says
                // otherwise, and the setting asked for every interface.
                AddOnce(endpoints, new IPEndPoint(IPAddress.IPv6Any, port));
                return;
            }

            if (host == "localhost")
            {
                AddOnce(endpoints, new IPEndPoint(IPAddress.Loopback, port));
                AddOnce(endpoints, new IPEndPoint(IPAddress.IPv6Loopback, port));
                return;
            }

            IPAddress literal;
            if (IPAddress.TryParse(host, out literal))
            {
                AddOnce(endpoints, new IPEndPoint(literal, port));
                return;
            }

            // A host name: bind every address it resolves to, so the name a
            // client dials is a name that answers.
            foreach (IPAddress address in Dns.GetHostAddresses(host))
            {
                AddOnce(endpoints, new IPEndPoint(address, port));
            }
        }

        private static void AddOnce(List<IPEndPoint> endpoints, IPEndPoint endpoint)
        {
            foreach (IPEndPoint known in endpoints)
            {
                if (known.Address.Equals(endpoint.Address) && known.Port == endpoint.Port)
                    return;
            }
            endpoints.Add(endpoint);
        }

        /// <summary>The host part of a listener prefix, brackets removed.</summary>
        private static string HostOf(string prefix)
        {
            int scheme = prefix.IndexOf("://", StringComparison.Ordinal);
            string rest = scheme >= 0 ? prefix.Substring(scheme + 3) : prefix;
            int colon = rest.LastIndexOf(':');
            string host = colon >= 0 ? rest.Substring(0, colon) : rest;
            if (host.StartsWith("[", StringComparison.Ordinal) && host.EndsWith("]", StringComparison.Ordinal))
                host = host.Substring(1, host.Length - 2);
            return host;
        }
    }
}
