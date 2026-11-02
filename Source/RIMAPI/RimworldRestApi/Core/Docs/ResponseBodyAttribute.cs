using System;

namespace RIMAPI.Core
{
    /// <summary>
    /// Names the type a route answers in the envelope's data field.
    /// </summary>
    /// <remarks>
    /// A handler takes only the HttpListenerContext, so its payload type is
    /// invisible to reflection; it lives in the service call inside the body.
    /// The route declares it here (ticket #4304) and the OpenAPI renderer turns
    /// that type into the response schema, so the document cannot drift from
    /// the C# type the service answers with. The source-level test in
    /// RouteMetadataTests reads the service call and fails when the declared
    /// type and the answered one disagree.
    ///
    ///     [ResponseBody(typeof(PawnDto))]            // data is one PawnDto
    ///     [ResponseBody(typeof(List&lt;PawnDto&gt;))]     // data is a list of them
    ///     [ResponseBody]                             // the route reports nothing back
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ResponseBodyAttribute : Attribute
    {
        /// <summary>The payload type; null when the route answers no payload.</summary>
        public Type Type { get; }

        public ResponseBodyAttribute() { }

        public ResponseBodyAttribute(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Type = type;
        }
    }
}
