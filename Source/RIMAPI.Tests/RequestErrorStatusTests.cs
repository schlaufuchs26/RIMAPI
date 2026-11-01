using System;
using System.Net;
using RIMAPI.Helpers;
using RIMAPI.Http;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// What a call the caller built wrong answers with (fox ticket #3769). A
    /// forgotten parameter used to arrive as an internal server error whose text
    /// said "not found", so the response builder made a 404 of it and the client
    /// read "endpoint not found" for a call that only lacked map_id.
    /// </summary>
    public class RequestErrorStatusTests
    {
        [Fact]
        public void AMissingParameterIsTheCallersMistake()
        {
            var ex = new ParameterNotFoundException("map_id");

            Assert.True(RequestErrorStatus.IsClientError(ex));
            Assert.Equal(HttpStatusCode.BadRequest, RequestErrorStatus.For(ex));
            Assert.Equal(
                "Required parameter 'map_id' was not found or is empty.",
                RequestErrorStatus.MessageFor(ex)
            );
        }

        [Fact]
        public void AMalformedParameterIsOneToo()
        {
            var ex = new ParameterFormatException("map_id", "abc", typeof(int));

            Assert.Equal(HttpStatusCode.BadRequest, RequestErrorStatus.For(ex));
            // The caller reads what it did, not a server fault it cannot act on.
            Assert.DoesNotContain("Internal server error", RequestErrorStatus.MessageFor(ex));
            Assert.Contains("map_id", RequestErrorStatus.MessageFor(ex));
        }

        [Fact]
        public void AnyOtherFaultStaysAServerError()
        {
            var ex = new InvalidOperationException("no map loaded");

            Assert.False(RequestErrorStatus.IsClientError(ex));
            Assert.Equal(HttpStatusCode.InternalServerError, RequestErrorStatus.For(ex));
            Assert.Equal(
                "Internal server error: no map loaded",
                RequestErrorStatus.MessageFor(ex)
            );
        }
    }
}
