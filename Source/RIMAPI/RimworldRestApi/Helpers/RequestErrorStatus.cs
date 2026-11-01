using System;
using System.Net;
using RIMAPI.Http;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// What a route handler's exception answers with (fox ticket #3769).
    ///
    /// A call that forgets a required parameter used to leave the handler as
    /// <c>Internal server error: Required parameter 'map_id' was not found or is
    /// empty.</c>, and the response builder picked the status out of that text:
    /// the words "not found" made a 404 out of a request the caller built wrong.
    /// A client that treats every 404 as "this mod build has no such route" then
    /// reported a missing endpoint for a call that only lacked a parameter.
    /// </summary>
    public static class RequestErrorStatus
    {
        /// <summary>
        /// True for the two exceptions a malformed request raises: a parameter
        /// the caller left out, and a parameter it wrote in the wrong shape.
        /// </summary>
        public static bool IsClientError(Exception ex) =>
            ex is ParameterNotFoundException || ex is ParameterFormatException;

        /// <summary>The status the exception answers with.</summary>
        public static HttpStatusCode For(Exception ex) =>
            IsClientError(ex)
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.InternalServerError;

        /// <summary>
        /// The sentence the caller reads. A parameter error speaks for itself;
        /// a server fault keeps the prefix that says the mod, not the caller,
        /// is at fault.
        /// </summary>
        public static string MessageFor(Exception ex) =>
            IsClientError(ex) ? ex.Message : $"Internal server error: {ex.Message}";
    }
}
