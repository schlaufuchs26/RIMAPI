using System.Text;
using RIMAPI.Core;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The head the door in front of the listener hands it (fox ticket #4233):
    /// the listener answers 411 to a POST without a length, and it matches the
    /// Host it was given, so both are corrected before it parses the request.
    /// </summary>
    public class RequestFramingTests
    {
        private static string Normalized(string head, string upstream = "127.0.0.1:8767")
        {
            byte[] patched = RequestFraming.Normalize(Encoding.ASCII.GetBytes(head), upstream);
            return Encoding.ASCII.GetString(patched);
        }

        [Fact]
        public void ABodylessPostGetsAnEmptyLength()
        {
            string head = Normalized("POST /api/v1/outfits/set-default?id=5 HTTP/1.1\r\nHost: 127.0.0.1:8765\r\n\r\n");

            Assert.Contains("Content-Length: 0\r\n", head);
        }

        [Fact]
        public void ABodylessPutGetsAnEmptyLengthToo()
        {
            string head = Normalized("PUT /api/v1/x HTTP/1.1\r\nHost: 127.0.0.1:8765\r\n\r\n");

            Assert.Contains("Content-Length: 0\r\n", head);
        }

        [Fact]
        public void AGetKeepsItsOwnShape()
        {
            string head = Normalized("GET /api/v1/game/state HTTP/1.1\r\nHost: 127.0.0.1:8765\r\n\r\n");

            Assert.DoesNotContain("Content-Length", head);
        }

        [Fact]
        public void APostWithABodyKeepsItsLength()
        {
            string head = Normalized(
                "POST /api/v1/game/speed HTTP/1.1\r\nHost: 127.0.0.1:8765\r\nContent-Length: 14\r\n\r\n"
            );

            Assert.Contains("Content-Length: 14\r\n", head);
            Assert.DoesNotContain("Content-Length: 0", head);
        }

        [Fact]
        public void AChunkedPostIsLeftWithoutALength()
        {
            string head = Normalized(
                "POST /api/v1/game/speed HTTP/1.1\r\nHost: 127.0.0.1:8765\r\nTransfer-Encoding: chunked\r\n\r\n"
            );

            Assert.Contains("Transfer-Encoding: chunked\r\n", head);
            Assert.DoesNotContain("Content-Length", head);
        }

        [Fact]
        public void TheHostNamesTheListenerAndNotTheClient()
        {
            string head = Normalized("POST /api/v1/x HTTP/1.1\r\nHost: 192.168.1.5:8765\r\n\r\n");

            Assert.Contains("Host: 127.0.0.1:8767\r\n", head);
            Assert.DoesNotContain("192.168.1.5", head);
        }

        [Fact]
        public void ARequestWithoutAHostStillGetsOne()
        {
            string head = Normalized("POST /api/v1/x HTTP/1.0\r\n\r\n");

            Assert.Contains("Host: 127.0.0.1:8767\r\n", head);
        }

        [Fact]
        public void TheHeadSaysCloseBecauseOneConnectionCarriesOneRequest()
        {
            string head = Normalized(
                "POST /api/v1/x HTTP/1.1\r\nHost: h\r\nConnection: keep-alive\r\n\r\n"
            );

            Assert.Contains("Connection: close\r\n", head);
            Assert.DoesNotContain("keep-alive", head);
            Assert.EndsWith("\r\n\r\n", head);
        }

        [Fact]
        public void EveryOtherHeaderSurvives()
        {
            string head = Normalized(
                "POST /api/v1/x HTTP/1.1\r\nHost: h\r\nX-Trace: 42\r\nContent-Type: application/json\r\n\r\n"
            );

            Assert.Contains("X-Trace: 42\r\n", head);
            Assert.Contains("Content-Type: application/json\r\n", head);
        }

        [Fact]
        public void TheHeaderNameIsMatchedWhateverItsCase()
        {
            string head = Normalized(
                "POST /api/v1/x HTTP/1.1\r\nHost: h\r\ncontent-length: 7\r\n\r\n"
            );

            Assert.Contains("content-length: 7\r\n", head);
            Assert.DoesNotContain("Content-Length: 0", head);
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("post")]
        [InlineData("PUT")]
        public void TheListenerWantsALengthForTheseMethods(string method)
        {
            Assert.True(RequestFraming.NeedsLength(method));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        [InlineData("")]
        public void TheseMethodsGoWithout(string method)
        {
            Assert.False(RequestFraming.NeedsLength(method));
        }

        [Fact]
        public void TheHeadEndIsFoundWithEitherLineEnding()
        {
            byte[] crlf = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\nbody");
            byte[] lf = Encoding.ASCII.GetBytes("GET / HTTP/1.1\n\nbody");

            Assert.Equal(18, RequestFraming.HeadEnd(crlf, crlf.Length));
            Assert.Equal(16, RequestFraming.HeadEnd(lf, lf.Length));
            Assert.Equal(-1, RequestFraming.HeadEnd(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n"), 16));
        }
    }
}
