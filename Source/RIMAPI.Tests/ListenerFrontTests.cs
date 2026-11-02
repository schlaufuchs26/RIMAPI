using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RIMAPI.Core;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The door in front of the listener (fox ticket #4233), measured against a
    /// real HttpListener on loopback: what the listener refuses on its own, the
    /// door makes acceptable, and the body and the answer stay intact.
    /// </summary>
    public class ListenerFrontTests : IDisposable
    {
        private const int TimeoutMs = 5000;

        private readonly HttpListener _listener;
        private readonly ListenerFront _front;
        private readonly int _listenerPort;
        private readonly int _publicPort;

        public ListenerFrontTests()
        {
            _listenerPort = ListenerFront.FreeLoopbackPort();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{_listenerPort}/");
            _listener.Start();
            _ = Task.Run(AnswerAsync);

            _publicPort = ListenerFront.FreeLoopbackPort();
            _front = new ListenerFront(
                new[] { new IPEndPoint(IPAddress.Loopback, _publicPort) },
                _listenerPort
            );
            _front.Start();
        }

        public void Dispose()
        {
            _front.Dispose();
            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch (Exception)
            {
                // A closed listener is the goal, not an error.
            }
        }

        /// <summary>Answers "METHOD|body", or a megabyte for /big.</summary>
        private async Task AnswerAsync()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                string body = string.Empty;
                if (context.Request.HasEntityBody)
                {
                    using (var reader = new StreamReader(context.Request.InputStream))
                    {
                        body = await reader.ReadToEndAsync();
                    }
                }

                byte[] payload =
                    context.Request.Url.AbsolutePath == "/big"
                        ? Encoding.ASCII.GetBytes("big|" + new string('x', 1024 * 1024))
                        : Encoding.ASCII.GetBytes(context.Request.HttpMethod + "|" + body);

                context.Response.StatusCode = 200;
                context.Response.ContentLength64 = payload.Length;
                await context.Response.OutputStream.WriteAsync(payload, 0, payload.Length);
                context.Response.Close();
            }
        }

        [Fact]
        public void TheBareListenerRefusesABodylessPost()
        {
            string answer = Send(_listenerPort, "POST /x HTTP/1.1\r\nHost: h\r\n\r\n");

            Assert.StartsWith("HTTP/1.1 411", answer);
        }

        [Fact]
        public void ABodylessPostReachesTheHandlerThroughTheDoor()
        {
            string answer = Send(_publicPort, "POST /x HTTP/1.1\r\nHost: 127.0.0.1:8765\r\n\r\n");

            Assert.StartsWith("HTTP/1.1 200", answer);
            Assert.Equal("POST|", Body(answer));
        }

        [Fact]
        public void APostBodyArrivesWhole()
        {
            const string json = "{\"id\":5}";
            string answer = Send(
                _publicPort,
                "POST /x HTTP/1.1\r\nHost: h\r\nContent-Type: application/json\r\n"
                    + $"Content-Length: {json.Length}\r\n\r\n"
                    + json
            );

            Assert.Equal("POST|" + json, Body(answer));
        }

        [Fact]
        public void AHeadThatArrivesInTwoPiecesIsStillRead()
        {
            string answer = Send(
                _publicPort,
                "POST /x HTTP/1.1\r\nHost: 127.0.0.1:8765\r\n\r\n",
                splitAfter: 12
            );

            Assert.StartsWith("HTTP/1.1 200", answer);
        }

        [Fact]
        public void AReadPassesThrough()
        {
            string answer = Send(_publicPort, "GET /x HTTP/1.1\r\nHost: h\r\n\r\n");

            Assert.StartsWith("HTTP/1.1 200", answer);
            Assert.Equal("GET|", Body(answer));
        }

        [Fact]
        public void ALargeAnswerArrivesWhole()
        {
            string answer = Send(_publicPort, "GET /big HTTP/1.1\r\nHost: h\r\n\r\n");

            string body = Body(answer);
            Assert.StartsWith("big|", body);
            Assert.Equal(4 + 1024 * 1024, body.Length);
        }

        [Fact]
        public void AWildcardSettingIsReachableOnLoopback()
        {
            int publicPort = ListenerFront.FreeLoopbackPort();
            var wildcard = ListenAddresses.For("0.0.0.0", publicPort);
            using (var front = new ListenerFront(wildcard, _listenerPort))
            {
                front.Start();

                string answer = Send(publicPort, "POST /x HTTP/1.1\r\nHost: h\r\n\r\n");

                Assert.StartsWith("HTTP/1.1 200", answer);
                Assert.Equal("POST|", Body(answer));
            }
        }

        [Fact]
        public void TheNextConnectionIsServedToo()
        {
            Send(_publicPort, "POST /x HTTP/1.1\r\nHost: h\r\n\r\n");
            string answer = Send(_publicPort, "POST /x HTTP/1.1\r\nHost: h\r\n\r\n");

            Assert.StartsWith("HTTP/1.1 200", answer);
        }

        private static string Body(string answer)
        {
            int blank = answer.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            return blank >= 0 ? answer.Substring(blank + 4) : string.Empty;
        }

        /// <summary>
        /// Sends raw bytes, because a normal client always sends a length, and
        /// reads the answer by its declared length so the socket's own close does
        /// not cut it.
        /// </summary>
        private static string Send(int port, string request, int splitAfter = -1)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(request);
            using (var client = new TcpClient())
            {
                client.Connect(IPAddress.Loopback, port);
                client.ReceiveTimeout = TimeoutMs;
                NetworkStream stream = client.GetStream();

                if (splitAfter > 0)
                {
                    stream.Write(bytes, 0, splitAfter);
                    stream.Flush();
                    Task.Delay(50).Wait();
                    stream.Write(bytes, splitAfter, bytes.Length - splitAfter);
                }
                else
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
                stream.Flush();

                var answer = new MemoryStream();
                var buffer = new byte[64 * 1024];
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;
                    answer.Write(buffer, 0, read);
                    if (IsComplete(answer.ToArray()))
                        break;
                }

                return Encoding.ASCII.GetString(answer.ToArray());
            }
        }

        private static bool IsComplete(byte[] answer)
        {
            string text = Encoding.ASCII.GetString(answer);
            int blank = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (blank < 0)
                return false;

            int lengthAt = text.IndexOf("Content-Length: ", StringComparison.OrdinalIgnoreCase);
            if (lengthAt < 0)
                return true; // no body to wait for, e.g. a 411

            int lineEnd = text.IndexOf("\r\n", lengthAt, StringComparison.Ordinal);
            int declared = int.Parse(text.Substring(lengthAt + 16, lineEnd - lengthAt - 16));
            return answer.Length >= blank + 4 + declared;
        }
    }
}
