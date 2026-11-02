using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RIMAPI.Core
{
    /// <summary>
    /// The documentation model the mod builds from its own route registry.
    /// </summary>
    /// <remarks>
    /// It lives in its own file, without a single game type, so the unit tests
    /// compile it and the OpenAPI renderer against it (ticket #4234): the
    /// document a client reads is shaped here, and a renderer that needs the
    /// game to run would be untestable.
    /// </remarks>
    public class ApiDocumentation
    {
        public DateTime GeneratedAt { get; set; }
        public string BaseUrl { get; set; }
        public string Version { get; set; }
        public List<DocumentationSection> Sections { get; set; } = new List<DocumentationSection>();
    }

    public class DocumentationSection
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<DocumentedEndpoint> Endpoints { get; set; } = new List<DocumentedEndpoint>();
    }

    public class DocumentedEndpoint
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public string GithubLinkTitle { get; set; }
        public string GithubLink { get; set; }
        public string Category { get; set; } = "General";
        public string Notes { get; set; }
        public string RequestExample { get; set; }
        public string ResponseExample { get; set; }
        public List<EndpointParameter> Parameters { get; set; } = new List<EndpointParameter>();
        public string Controller { get; set; }
        public string Action { get; set; }
        public bool RequiresAuthentication { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string DeprecationNotice { get; set; }
        public string SinceVersion { get; set; }

        /// <summary>
        /// The DTO a write route reads from its request body, from the route's
        /// [RequestBody(typeof(T))] attribute (ticket #4234). A Type cannot be
        /// serialized into the JSON documentation, so it stays out of it.
        /// </summary>
        [JsonIgnore]
        public Type RequestBodyType { get; set; }

        /// <summary>
        /// The type a route answers in the envelope's data field, from
        /// [ResponseBody(typeof(T))] (ticket #4304); null when it answers no
        /// payload, and <see cref="DeclaresResponseBody"/> says whether the
        /// route declared that at all. A Type cannot be serialized into the
        /// JSON documentation, so it stays out of it.
        /// </summary>
        [JsonIgnore]
        public Type ResponseBodyType { get; set; }

        /// <summary>Whether the route carries a [ResponseBody] declaration.</summary>
        [JsonIgnore]
        public bool DeclaresResponseBody { get; set; }
    }

    public class EndpointParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Source { get; set; } // "query", "path", "body", "header"
        public bool Required { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }
        public string DefaultValue { get; set; }
        public string[] AllowedValues { get; set; } = Array.Empty<string>();
        public ParameterValidation Validation { get; set; }
    }

    public class ParameterValidation
    {
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string Pattern { get; set; }
        public object Minimum { get; set; }
        public object Maximum { get; set; }
    }
}
