using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RIMAPI.Core;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The route metadata the OpenAPI document is rendered from (fox ticket
    /// #4234). A handler takes only the HttpListenerContext, so its body DTO
    /// and its query parameters are invisible to reflection and have to be
    /// declared on the method. These source-level tests keep the declaration
    /// and the handler's own code in step: a new RequestParser call or a
    /// changed ReadBodyAsync&lt;T&gt;() that nobody declares fails here, not in
    /// a client that reads a spec with a missing parameter.
    /// </summary>
    public class RouteMetadataTests
    {
        private static readonly Regex RouteAttribute = new Regex(
            @"\[(Get|Post|Put|Delete)\(""([^""]+)""\)\]",
            RegexOptions.Compiled
        );

        private static readonly Regex BodyAttribute = new Regex(
            @"\[RequestBody\(typeof\((?<type>[A-Za-z0-9_]+)\)\)\]",
            RegexOptions.Compiled
        );

        private static readonly Regex QueryAttribute = new Regex(
            @"\[QueryParameters\((?<args>[^\]]*)\)\]",
            RegexOptions.Compiled
        );

        private static readonly Regex ReadBody = new Regex(
            @"ReadBodyAsync<(?<type>[A-Za-z0-9_]+)>",
            RegexOptions.Compiled
        );

        private static readonly Regex Declaration = new Regex(
            @"^[ \t]*(?:public|private|protected|internal)\b[^\n;=\(\)]*?\b(?<name>\w+)[ \t]*\(",
            RegexOptions.Multiline | RegexOptions.Compiled
        );

        [Fact]
        public void EveryDeclaredQueryParameterParses()
        {
            foreach (RouteMethod route in Routes())
            {
                foreach (QueryParameterSpec spec in route.DeclaredQueryParameters.Values)
                {
                    // Parse throws on a bad declaration; naming the route makes
                    // the failure actionable.
                    Assert.True(
                        spec.Name.Length > 0 && spec.Type.Length > 0,
                        route.Location + " declares an empty query parameter"
                    );
                }
            }
        }

        [Fact]
        public void EveryBodyReadIsDeclaredWithTheSameDto()
        {
            var problems = new List<string>();

            foreach (RouteMethod route in Routes())
            {
                if (route.BodyTypes.Count > 1)
                {
                    problems.Add(
                        route.Location + " reads " + string.Join(", ", route.BodyTypes)
                            + "; a route declares one body DTO"
                    );
                    continue;
                }

                if (route.BodyTypes.Count == 0)
                {
                    if (route.DeclaredBodyType != null)
                        problems.Add(
                            route.Location + " declares RequestBody(typeof(" + route.DeclaredBodyType
                                + ")) but reads no body"
                        );
                    continue;
                }

                string expected = route.BodyTypes[0];
                if (route.DeclaredBodyType == null)
                    problems.Add(
                        route.Location + " reads ReadBodyAsync<" + expected
                            + ">() but has no [RequestBody(typeof(" + expected + "))]"
                    );
                else if (route.DeclaredBodyType != expected)
                    problems.Add(
                        route.Location + " reads ReadBodyAsync<" + expected
                            + ">() but declares [RequestBody(typeof(" + route.DeclaredBodyType + "))]"
                    );
            }

            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        [Fact]
        public void EveryQueryParameterTheHandlerReadsIsDeclared()
        {
            var problems = new List<string>();

            foreach (RouteMethod route in Routes())
            {
                foreach (QueryRead read in route.ReadQueryParameters)
                {
                    QueryParameterSpec declared;
                    if (!route.DeclaredQueryParameters.TryGetValue(read.Name, out declared))
                    {
                        problems.Add(
                            route.Location + " reads query parameter '" + read.Name
                                + "' but no [QueryParameters] declares it"
                        );
                        continue;
                    }

                    if (declared.Type != read.Type)
                        problems.Add(
                            route.Location + " declares '" + read.Name + "' as " + declared.Type
                                + " but reads it as " + read.Type
                        );
                    if (declared.Required != read.Required)
                        problems.Add(
                            route.Location + " declares '" + read.Name + "' as "
                                + (declared.Required ? "required" : "optional")
                                + " but the handler treats it as "
                                + (read.Required ? "required" : "optional")
                        );
                }

                foreach (string declared in route.DeclaredQueryParameters.Keys)
                {
                    if (!route.ReadQueryParameters.Any(r => r.Name == declared))
                        problems.Add(
                            route.Location + " declares query parameter '" + declared
                                + "' but the handler never reads it"
                        );
                }
            }

            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        [Fact]
        public void NoTwoHandlersClaimTheSameRoute()
        {
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            var problems = new List<string>();

            foreach (RouteMethod route in Routes())
            {
                string key = route.HttpMethod + " " + route.Path;
                string owner;
                if (seen.TryGetValue(key, out owner))
                    problems.Add(key + " is claimed by " + owner + " and " + route.Location);
                else
                    seen[key] = route.Location;
            }

            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        [Fact]
        public void EveryDocumentedRouteCanBeBoundByTheRegistry()
        {
            var problems = new List<string>();

            foreach (RouteMethod route in Routes())
            {
                if (!route.ReturnsTask)
                    problems.Add(route.Location + " carries a route attribute but returns no Task");

                foreach (ParameterShape parameter in route.Parameters)
                {
                    if (parameter.Name == "context")
                        continue;
                    if (route.Path.IndexOf("{" + parameter.Name + "}", StringComparison.Ordinal) >= 0)
                        continue;
                    if (parameter.HasDefault)
                        continue;
                    problems.Add(
                        route.Location + " takes '" + parameter.Name
                            + "', which the route cannot fill: no {" + parameter.Name
                            + "} segment and no default value"
                    );
                }
            }

            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        private static IEnumerable<RouteMethod> Routes()
        {
            string root = SourceTree.RepoRoot();
            foreach (string path in SourceTree.Sources(root, "RimworldRestApi"))
            {
                string text = File.ReadAllText(path);
                string[] lines = text.Split('\n');

                foreach (Match declaration in Declaration.Matches(text))
                {
                    int line = LineOf(text, declaration.Index);
                    List<string> attributes = AttributeLines(lines, line - 1);

                    Match route = null;
                    foreach (string attribute in attributes)
                    {
                        route = RouteAttribute.Match(attribute);
                        if (route.Success)
                            break;
                    }
                    if (route == null)
                        continue;

                    string body = Body(text, declaration.Index + declaration.Length - 1);
                    if (body == null)
                        continue;

                    int open = declaration.Index + declaration.Length - 1;
                    int close = MatchingParen(text, open);
                    string signature = text.Substring(declaration.Index, open - declaration.Index);

                    yield return new RouteMethod(
                        route.Groups[1].Value.ToUpperInvariant(),
                        route.Groups[2].Value,
                        SourceTree.Relative(root, path) + ":" + line,
                        attributes,
                        body,
                        signature,
                        close > open ? text.Substring(open + 1, close - open - 1) : string.Empty
                    );
                }
            }
        }

        /// <summary>The contiguous attribute block above a declaration.</summary>
        private static List<string> AttributeLines(string[] lines, int declarationLine)
        {
            var block = new List<string>();
            for (int i = declarationLine - 1; i >= 0; i--)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("[", StringComparison.Ordinal)
                    || trimmed.StartsWith("\"", StringComparison.Ordinal)
                    || trimmed.StartsWith("*", StringComparison.Ordinal))
                {
                    block.Insert(0, trimmed);
                    continue;
                }
                break;
            }
            return block;
        }

        private static string Body(string text, int openParen)
        {
            int depth = 0;
            int i = openParen;
            for (; i < text.Length; i++)
            {
                if (text[i] == '(')
                    depth++;
                else if (text[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        i++;
                        break;
                    }
                }
            }

            while (i < text.Length && text[i] != '{')
                i++;
            if (i >= text.Length)
                return null;

            int start = i;
            depth = 0;
            for (; i < text.Length; i++)
            {
                if (text[i] == '{')
                    depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return text.Substring(start, i - start + 1);
                }
            }
            return null;
        }

        private static int MatchingParen(string text, int open)
        {
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '(')
                    depth++;
                else if (text[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }
            return -1;
        }

        private static int LineOf(string text, int index)
        {
            int line = 1;
            for (int i = 0; i < index && i < text.Length; i++)
                if (text[i] == '\n')
                    line++;
            return line;
        }

        private sealed class RouteMethod
        {
            private static readonly Regex[] Patterns =
            {
                new Regex(@"GetMapId\(\s*context\s*\)", RegexOptions.Compiled),
                new Regex(@"GetZoneId\(\s*context\s*\)", RegexOptions.Compiled),
                new Regex(@"GetBuildingId\(\s*context\s*\)", RegexOptions.Compiled),
                new Regex(@"GetIntParameter\(\s*context\s*,\s*""(?<name>[a-z_]+)""\s*\)", RegexOptions.Compiled),
                new Regex(@"GetStringParameter\(\s*context\s*,\s*""(?<name>[a-z_]+)""\s*(?:,\s*(?<required>true|false)\s*)?\)", RegexOptions.Compiled),
                new Regex(@"GetBooleanParameter\(\s*context\s*,\s*""(?<name>[a-z_]+)""", RegexOptions.Compiled),
                new Regex(@"GetFloatParameter\(\s*context\s*,\s*""(?<name>[a-z_]+)""", RegexOptions.Compiled),
                new Regex(@"GetDoubleParameter\(\s*context\s*,\s*""(?<name>[a-z_]+)""", RegexOptions.Compiled),
                new Regex(@"HasParameter\(\s*context\s*,\s*""(?<name>[a-z_]+)""\s*\)", RegexOptions.Compiled),
                new Regex(@"QueryString\[""(?<name>[a-z_]+)""\]", RegexOptions.Compiled),
            };

            public RouteMethod(
                string httpMethod,
                string path,
                string location,
                List<string> attributes,
                string body,
                string signature,
                string parameterList
            )
            {
                HttpMethod = httpMethod;
                Path = path;
                Location = location;
                ReturnsTask = signature.IndexOf("Task", StringComparison.Ordinal) >= 0;
                Parameters = ParameterShape.ParseList(parameterList);

                foreach (string attribute in attributes)
                {
                    Match bodyAttribute = BodyAttribute.Match(attribute);
                    if (bodyAttribute.Success)
                        DeclaredBodyType = bodyAttribute.Groups["type"].Value;

                    Match queryAttribute = QueryAttribute.Match(attribute);
                    if (queryAttribute.Success)
                    {
                        foreach (Match literal in Regex.Matches(queryAttribute.Groups["args"].Value, "\"([^\"]*)\""))
                        {
                            QueryParameterSpec spec = QueryParameterSpec.Parse(literal.Groups[1].Value);
                            DeclaredQueryParameters[spec.Name] = spec;
                        }
                    }
                }

                foreach (Match match in ReadBody.Matches(body))
                    BodyTypes.Add(match.Groups["type"].Value);

                ReadQueryParameters = ReadQuery(body);
            }

            public string HttpMethod { get; }
            public string Path { get; }
            public bool ReturnsTask { get; }
            public List<ParameterShape> Parameters { get; }
            public string Location { get; }
            public string DeclaredBodyType { get; }
            public List<string> BodyTypes { get; } = new List<string>();
            public Dictionary<string, QueryParameterSpec> DeclaredQueryParameters { get; } =
                new Dictionary<string, QueryParameterSpec>(StringComparer.Ordinal);
            public List<QueryRead> ReadQueryParameters { get; }

            private static List<QueryRead> ReadQuery(string body)
            {
                var reads = new Dictionary<string, QueryRead>(StringComparer.Ordinal);
                var optional = new HashSet<string>(StringComparer.Ordinal);

                foreach (Match match in Patterns[0].Matches(body))
                    reads["map_id"] = new QueryRead("map_id", "integer", true);
                foreach (Match match in Patterns[1].Matches(body))
                    reads["zone_id"] = new QueryRead("zone_id", "integer", true);
                foreach (Match match in Patterns[2].Matches(body))
                    reads["building_id"] = new QueryRead("building_id", "integer", true);

                AddMatches(reads, optional, Patterns[3], body, "integer", true);
                AddMatches(reads, optional, Patterns[4], body, "string", true);
                AddMatches(reads, optional, Patterns[5], body, "boolean", false);
                AddMatches(reads, optional, Patterns[6], body, "number", true);
                AddMatches(reads, optional, Patterns[7], body, "number", true);
                AddMatches(reads, optional, Patterns[8], body, "string", false);
                AddMatches(reads, optional, Patterns[9], body, "string", false);

                // "HasParameter(x) ? GetIntParameter(x) : 0" reads an optional
                // integer: the presence check decides whether it is required.
                foreach (string name in optional)
                {
                    QueryRead read;
                    if (reads.TryGetValue(name, out read))
                        reads[name] = new QueryRead(read.Name, read.Type, false);
                }

                return reads.Values.OrderBy(r => r.Name, StringComparer.Ordinal).ToList();
            }

            private static void AddMatches(
                Dictionary<string, QueryRead> reads,
                HashSet<string> optional,
                Regex pattern,
                string body,
                string type,
                bool required
            )
            {
                foreach (Match match in pattern.Matches(body))
                {
                    string name = match.Groups["name"].Value;
                    bool isRequired = required;
                    Group requiredGroup = match.Groups["required"];
                    if (requiredGroup.Success && requiredGroup.Value == "false")
                        isRequired = false;

                    reads[name] = new QueryRead(name, type, isRequired);
                    if (!isRequired)
                        optional.Add(name);
                }
            }
        }

        private sealed class ParameterShape
        {
            private ParameterShape(string name, bool hasDefault)
            {
                Name = name;
                HasDefault = hasDefault;
            }

            public string Name { get; }
            public bool HasDefault { get; }

            public static List<ParameterShape> ParseList(string list)
            {
                var shapes = new List<ParameterShape>();
                if (string.IsNullOrWhiteSpace(list))
                    return shapes;

                int depth = 0;
                int start = 0;
                for (int i = 0; i <= list.Length; i++)
                {
                    if (i < list.Length && (list[i] == '<' || list[i] == '('))
                        depth++;
                    else if (i < list.Length && (list[i] == '>' || list[i] == ')'))
                        depth--;
                    else if (i == list.Length || (list[i] == ',' && depth == 0))
                    {
                        string part = list.Substring(start, i - start).Trim();
                        start = i + 1;
                        if (part.Length == 0)
                            continue;

                        bool hasDefault = part.IndexOf('=') >= 0;
                        string declaration = hasDefault ? part.Substring(0, part.IndexOf('=')) : part;
                        string[] words = declaration.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length > 0)
                            shapes.Add(new ParameterShape(words[words.Length - 1], hasDefault));
                    }
                }

                return shapes;
            }
        }

        private sealed class QueryRead
        {
            public QueryRead(string name, string type, bool required)
            {
                Name = name;
                Type = type;
                Required = required;
            }

            public string Name { get; }
            public string Type { get; }
            public bool Required { get; }
        }
    }
}
