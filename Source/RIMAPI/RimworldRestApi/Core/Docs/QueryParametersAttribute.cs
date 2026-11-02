using System;
using System.Collections.Generic;

namespace RIMAPI.Core
{
    /// <summary>
    /// Declares the query parameters a route reads.
    /// </summary>
    /// <remarks>
    /// The handlers read their query string through RequestParser, so no
    /// reflection can see which names a route takes (ticket #4234). A route
    /// declares them here in one compact line:
    ///
    ///     [QueryParameters("building_id:integer", "only_researched:boolean?")]
    ///
    /// A trailing '?' marks a parameter the route treats as optional. The
    /// source-level test in RouteMetadataTests fails when a declared name and
    /// the handler's RequestParser calls disagree, so the two cannot drift.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class QueryParametersAttribute : Attribute
    {
        public IReadOnlyList<QueryParameterSpec> Parameters { get; }

        public QueryParametersAttribute(params string[] parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var parsed = new List<QueryParameterSpec>(parameters.Length);
            foreach (string raw in parameters)
                parsed.Add(QueryParameterSpec.Parse(raw));

            Parameters = parsed;
        }
    }

    /// <summary>One declared query parameter: its name, its JSON type and whether the route needs it.</summary>
    public sealed class QueryParameterSpec
    {
        private static readonly string[] KnownTypes = { "integer", "number", "string", "boolean" };

        public string Name { get; }
        public string Type { get; }
        public bool Required { get; }

        public QueryParameterSpec(string name, string type, bool required)
        {
            Name = name;
            Type = type;
            Required = required;
        }

        /// <summary>Reads "name:type" or "name:type?"; anything else is a programming error.</summary>
        public static QueryParameterSpec Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentException("Invalid query parameter: a name and a type are required.", nameof(raw));

            int colon = raw.IndexOf(':');
            if (colon <= 0 || colon == raw.Length - 1)
                throw new ArgumentException(
                    "Invalid query parameter: expected \"name:type\" (or \"name:type?\"), got \"" + raw + "\".",
                    nameof(raw)
                );

            string name = raw.Substring(0, colon).Trim();
            string type = raw.Substring(colon + 1).Trim();
            bool required = true;
            if (type.EndsWith("?", StringComparison.Ordinal))
            {
                required = false;
                type = type.Substring(0, type.Length - 1);
            }

            if (Array.IndexOf(KnownTypes, type) < 0)
                throw new ArgumentException(
                    "Invalid query parameter: unknown type \"" + type + "\" in \"" + raw
                        + "\"; use one of " + string.Join(", ", KnownTypes) + ".",
                    nameof(raw)
                );

            if (name.Length == 0 || name.IndexOf(' ') >= 0)
                throw new ArgumentException("Invalid query parameter: bad name in \"" + raw + "\".", nameof(raw));

            return new QueryParameterSpec(name, type, required);
        }

        public override string ToString() => Name + ":" + Type + (Required ? string.Empty : "?");
    }
}
