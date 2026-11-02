using System;

namespace RIMAPI.Core
{
    /// <summary>
    /// Names the DTO a route reads from its request body.
    /// </summary>
    /// <remarks>
    /// Every handler takes only the HttpListenerContext and pulls its body with
    /// ReadBodyAsync&lt;T&gt;(), so reflection cannot see the body type. Swashbuckle
    /// and NSwag get it from ASP.NET's ApiExplorer, which a HttpListener router
    /// does not have (ticket #4234), so the route declares it here and the
    /// OpenAPI renderer turns the DTO into a schema.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RequestBodyAttribute : Attribute
    {
        public Type Type { get; }

        public RequestBodyAttribute(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Type = type;
        }
    }
}
