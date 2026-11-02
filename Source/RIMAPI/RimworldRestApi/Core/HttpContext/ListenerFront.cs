using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RIMAPI.Core
{
    /// <summary>
    /// The door in front of the HTTP listener (fox ticket #4233): it accepts on
    /// the addresses the setting names, rewrites the request head so the
    /// listener takes it, and splices the connection to the listener's private
    /// loopback port.
    /// </summary>
    /// <remarks>
    /// A request head is all this touches. Once the head is through, both
    /// directions are copied byte for byte, so a streamed answer (the event
    /// feed, the camera) behaves as if the client had dialled the listener
    /// itself. One connection carries one request, because the head says
    /// "Connection: close": a second request on the same connection would arrive
    /// unpatched.
    /// </remarks>
    public sealed class ListenerFront : IDisposable
    {
        /// <summary>A head is a few hundred bytes; this cap stops a flood from holding a buffer.</summary>
        private const int MaxHeadBytes = 64 * 1024;

        /// <summary>How long a client may take to send its head before it is dropped.</summary>
        private const int HeadTimeoutMs = 15000;

        private readonly IReadOnlyList<IPEndPoint> _endpoints;
        private readonly List<TcpListener> _listeners = new List<TcpListener>();
        private readonly object _listenersLock = new object();
        private CancellationTokenSource _stop;
        private volatile bool _running;

        public ListenerFront(IReadOnlyList<IPEndPoint> endpoints, int upstreamPort)
        {
            if (endpoints == null || endpoints.Count == 0)
                throw new ArgumentException("at least one address is needed", nameof(endpoints));
            if (upstreamPort < 1 || upstreamPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(upstreamPort), upstreamPort, "a port is 1-65535");

            _endpoints = endpoints;
            UpstreamPort = upstreamPort;
        }

        /// <summary>The loopback port the listener is bound to.</summary>
        public int UpstreamPort { get; }

        /// <summary>The Host header every forwarded request carries.</summary>
        public string UpstreamHost
        {
            get { return "127.0.0.1:" + UpstreamPort; }
        }

        /// <summary>A port nothing listens on right now, for the listener to take.</summary>
        public static int FreeLoopbackPort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            try
            {
                return ((IPEndPoint)probe.LocalEndpoint).Port;
            }
            finally
            {
                probe.Stop();
            }
        }

        public void Start()
        {
            if (_running)
                return;

            _stop = new CancellationTokenSource();
            CancellationToken token = _stop.Token;

            try
            {
                foreach (IPEndPoint endpoint in _endpoints)
                {
                    var listener = new TcpListener(endpoint);
                    if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // The wildcard listens for both families; a single
                        // address answers for itself alone.
                        listener.Server.DualMode = endpoint.Address.Equals(IPAddress.IPv6Any);
                    }
                    listener.Start();
                    lock (_listenersLock)
                    {
                        _listeners.Add(listener);
                    }
                    TcpListener accepting = listener;
                    _ = Task.Run(() => AcceptLoopAsync(accepting, token));
                    LogApi.Info($"Front listening on {endpoint}");
                }
            }
            catch
            {
                Stop();
                throw;
            }

            _running = true;
        }

        public void Stop()
        {
            _running = false;
            _stop?.Cancel();

            lock (_listenersLock)
            {
                foreach (TcpListener listener in _listeners)
                {
                    try
                    {
                        listener.Stop();
                    }
                    catch (Exception ex)
                    {
                        LogApi.Warning($"Stopping the front's {listener.LocalEndpoint} failed: {ex.Message}");
                    }
                }
                _listeners.Clear();
            }
        }

        public void Dispose()
        {
            Stop();
            _stop?.Dispose();
            _stop = null;
        }

        private async Task AcceptLoopAsync(TcpListener listener, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                        return;
                    LogApi.Error($"Front failed to accept: {ex.Message}");
                    continue;
                }

                TcpClient accepted = client;
                _ = Task.Run(() => ForwardAsync(accepted, token));
            }
        }

        private async Task ForwardAsync(TcpClient client, CancellationToken token)
        {
            TcpClient upstream = null;
            try
            {
                client.NoDelay = true;
                NetworkStream clientStream = client.GetStream();

                RequestHead request = await ReadHeadAsync(clientStream, token);
                if (request == null)
                    return; // the client left, or never finished its head

                byte[] patched = RequestFraming.Normalize(request.Head, UpstreamHost);

                upstream = new TcpClient();
                await upstream.ConnectAsync(IPAddress.Loopback, UpstreamPort);
                upstream.NoDelay = true;
                NetworkStream upstreamStream = upstream.GetStream();

                await upstreamStream.WriteAsync(patched, 0, patched.Length);
                if (request.Rest.Length > 0)
                    await upstreamStream.WriteAsync(request.Rest, 0, request.Rest.Length);

                Task[] pumps =
                {
                    PumpAsync(clientStream, upstreamStream, token),
                    PumpAsync(upstreamStream, clientStream, token),
                };
                await Task.WhenAny(pumps);

                Close(client);
                Close(upstream);
                await Task.WhenAll(pumps);
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    LogApi.Warning($"Front dropped a connection: {ex.Message}");
            }
            finally
            {
                Close(upstream);
                Close(client);
            }
        }

        /// <summary>Copies one direction until the far end or this end closes.</summary>
        private static async Task PumpAsync(NetworkStream from, NetworkStream to, CancellationToken token)
        {
            var buffer = new byte[16 * 1024];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int read = await from.ReadAsync(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return;
                    await to.WriteAsync(buffer, 0, read);
                    await to.FlushAsync();
                }
            }
            catch (Exception)
            {
                // Either end closing is the normal way a request ends.
            }
        }

        /// <summary>
        /// A request head and the body bytes that arrived in the same read. The
        /// body travels with the head, because the splice starts after them.
        /// </summary>
        private sealed class RequestHead
        {
            public byte[] Head;
            public byte[] Rest;
        }

        /// <summary>
        /// The request head, terminator included, or null when the client closed
        /// first or took too long.
        /// </summary>
        private async Task<RequestHead> ReadHeadAsync(NetworkStream stream, CancellationToken token)
        {
            var buffer = new byte[MaxHeadBytes];
            int total = 0;
            Task timeout = Task.Delay(HeadTimeoutMs, token);

            while (total < buffer.Length)
            {
                int read;
                Task<int> pending = stream.ReadAsync(buffer, total, buffer.Length - total);
                Task finished = await Task.WhenAny(pending, timeout);
                if (finished == timeout)
                    return null;
                read = await pending;
                if (read <= 0)
                    return null;
                total += read;

                int end = RequestFraming.HeadEnd(buffer, total);
                if (end >= 0)
                {
                    var head = new byte[end];
                    Buffer.BlockCopy(buffer, 0, head, 0, end);
                    var rest = new byte[total - end];
                    Buffer.BlockCopy(buffer, end, rest, 0, rest.Length);
                    return new RequestHead { Head = head, Rest = rest };
                }
            }

            return null;
        }

        private static void Close(TcpClient client)
        {
            if (client == null)
                return;
            try
            {
                client.Close();
            }
            catch (Exception)
            {
                // A closed socket is the goal, not an error.
            }
        }
    }
}
