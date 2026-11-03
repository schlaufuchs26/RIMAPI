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
    ///
    /// Since ticket #4313 the entry also carries the description a reader of
    /// the document sees, after an '=':
    ///
    ///     [QueryParameters("id:integer=The colonist's numeric id, as the pawn list reports it.")]
    ///
    /// A name whose meaning is the same on every route (`map_id`, `pawn_id`,
    /// `x`) takes its text from <see cref="ParameterDescriptions"/> instead,
    /// so the sentence is written once. A name that is neither declared here
    /// nor shared throws: an undocumented parameter cannot reach the document.
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

    /// <summary>One declared query parameter: its name, its JSON type, whether the route needs it, and what it means.</summary>
    public sealed class QueryParameterSpec
    {
        private static readonly string[] KnownTypes = { "integer", "number", "string", "boolean" };

        public string Name { get; }
        public string Type { get; }
        public bool Required { get; }
        public string Description { get; }

        public QueryParameterSpec(string name, string type, bool required, string description = null)
        {
            Name = name;
            Type = type;
            Required = required;
            Description = description;
        }

        /// <summary>
        /// Reads "name:type", "name:type?" or "name:type=description" (and the
        /// optional form with a description); anything else is a programming
        /// error, including a parameter whose meaning no text states.
        /// </summary>
        public static QueryParameterSpec Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentException("Invalid query parameter: a name and a type are required.", nameof(raw));

            string rest = raw;
            string description = null;
            int equals = rest.IndexOf('=');
            if (equals >= 0)
            {
                description = rest.Substring(equals + 1).Trim();
                rest = rest.Substring(0, equals);
            }

            int colon = rest.IndexOf(':');
            if (colon <= 0 || colon == rest.Length - 1)
                throw new ArgumentException(
                    "Invalid query parameter: expected \"name:type\" (or \"name:type?\"), got \"" + raw + "\".",
                    nameof(raw)
                );

            string name = rest.Substring(0, colon).Trim();
            string type = rest.Substring(colon + 1).Trim();
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

            if (description == null)
                description = ParameterDescriptions.For(name);

            string problem = ParameterDescriptions.Why(description, name);
            if (problem != null)
                throw new ArgumentException(
                    "Invalid query parameter: \"" + name + "\" " + problem
                        + "; write it after an '=' or add it to ParameterDescriptions.",
                    nameof(raw)
                );

            return new QueryParameterSpec(name, type, required, description);
        }

        public override string ToString() => Name + ":" + Type + (Required ? string.Empty : "?");
    }
}
