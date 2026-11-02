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
    /// Every route declares the type it answers with (fox ticket #4304).
    /// </summary>
    /// <remarks>
    /// The OpenAPI document is only as good as the declarations on the
    /// handlers, and a hand-written schema string is what we are replacing: the
    /// route names the C# type, the renderer reflects it. These source-level
    /// tests keep the declaration equal to what the handler actually answers,
    /// by resolving the service call inside the body through the service
    /// interface. A route that has not been migrated yet is on the pending
    /// list, one entry per area, and an area leaves that list in the run that
    /// migrates it.
    /// </remarks>
    public class ResponseSchemaTests
    {
        private static readonly Regex RouteAttribute = new Regex(
            @"\[(Get|Post|Put|Delete)\(""([^""]+)""\)\]",
            RegexOptions.Compiled
        );

        // [ResponseBody(typeof(PawnDto))] or the bare [ResponseBody], which
        // says the route reports no payload.
        private static readonly Regex ResponseAttribute = new Regex(
            @"\[ResponseBody(?:\(typeof\((?<type>[^)]+)\)\))?\]",
            RegexOptions.Compiled
        );

        private static readonly Regex Field = new Regex(
            @"(?:private|public|protected)\s+readonly\s+(\w+)\s+(_\w+)\s*;",
            RegexOptions.Compiled
        );

        private static readonly Regex Call = new Regex(
            @"(\w+)\s*\.\s*(\w+)\s*\(",
            RegexOptions.Compiled
        );

        // A call without a receiver: a helper of the same file, such as the
        // SendRawResponse of the documentation controller.
        private static readonly Regex BareCall = new Regex(
            @"(?<![\w.])(?<name>\w+)\s*\(",
            RegexOptions.Compiled
        );

        private static readonly Regex InlineResult = new Regex(
            @"ApiResult<(?<type>[^;()]+?)>\s*\.\s*(?:Ok|Fail|Partial)\s*\(",
            RegexOptions.Compiled
        );

        private static readonly Regex Declaration = new Regex(
            @"^[ \t]*(?:public|private|protected|internal)\b[^\n;=\(\)]*?\b(?<name>\w+)[ \t]*\(",
            RegexOptions.Multiline | RegexOptions.Compiled
        );

        // Collection and primitive names a payload type may mention without
        // naming a model of its own.
        private static readonly HashSet<string> TypeWords = new HashSet<string>(
            new[]
            {
                "List", "Dictionary", "IList", "IEnumerable", "ICollection", "IReadOnlyList",
                "HashSet", "string", "int", "long", "float", "double", "bool", "byte",
                "decimal", "object", "DateTime",
            },
            StringComparer.Ordinal
        );

        [Fact]
        public void EveryRouteDeclaresTheResponseTypeItsServiceAnswers()
        {
            var problems = new List<string>();

            foreach (Route route in Routes())
            {
                bool pending = PendingResponseSchemas.Areas.Contains(route.Area);

                if (!route.HasDeclaration)
                {
                    if (!pending)
                        problems.Add(
                            route.Location + " " + route.Method + " " + route.Path
                                + " declares no [ResponseBody]; name the type it answers with, "
                                + "or the bare [ResponseBody] when it reports nothing back"
                        );
                    continue;
                }

                if (pending)
                    problems.Add(
                        route.Location + " " + route.Method + " " + route.Path
                            + " declares a response type, but its area '" + route.Area
                            + "' is still on the pending list in PendingResponseSchemas.cs"
                    );

                if (route.Answered == null)
                {
                    problems.Add(
                        route.Location + " " + route.Method + " " + route.Path
                            + " declares " + route.Declaration
                            + " but its answer cannot be read from the source: "
                            + route.AnswerHint
                    );
                    continue;
                }

                if (route.Declaration != route.Answered)
                    problems.Add(
                        route.Location + " " + route.Method + " " + route.Path
                            + " declares " + route.Declaration + " but answers " + route.Answered
                            + " (" + route.AnswerHint + ")"
                    );
            }

            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        [Fact]
        public void EveryDeclaredResponseTypeNamesAModel()
        {
            var models = ModelNames();
            var problems = new List<string>();

            foreach (Route route in Routes())
            {
                if (route.Declaration == null || route.Declaration == "none")
                    continue;

                foreach (string word in Regex.Split(route.Declaration, @"[^A-Za-z0-9_]+"))
                {
                    if (word.Length == 0 || TypeWords.Contains(word))
                        continue;
                    if (!models.Contains(word))
                        problems.Add(
                            route.Location + " " + route.Method + " " + route.Path
                                + " declares the response type '" + route.Declaration
                                + "', and '" + word + "' is no type under RIMAPI.Models"
                        );
                }
            }

            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        /// <summary>The model type names the API serializes.</summary>
        private static HashSet<string> ModelNames()
        {
            string root = SourceTree.RepoRoot();
            var names = new HashSet<string>(StringComparer.Ordinal);
            string models = Path.Combine(root, "Source", "RIMAPI", "RimworldRestApi", "Models");
            foreach (
                string path in Directory
                    .EnumerateFiles(models, "*.cs", SearchOption.AllDirectories)
                    .Where(p => !p.Contains("/obj/") && !p.Contains("/bin/"))
            )
            {
                string text = File.ReadAllText(path);
                foreach (
                    Match match in Regex.Matches(
                        text,
                        @"\b(?:class|struct|enum)\s+(?<name>\w+)"
                    )
                )
                    names.Add(match.Groups["name"].Value);
            }
            return names;
        }

        private static List<Route> Routes()
        {
            string root = SourceTree.RepoRoot();
            var routes = new List<Route>();
            var returns = ServiceReturns(root);

            foreach (string path in SourceTree.Sources(root, "RimworldRestApi"))
            {
                string text = File.ReadAllText(path);
                string[] lines = text.Split('\n');
                string relative = SourceTree.Relative(root, path);

                var fields = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (Match field in Field.Matches(text))
                    fields[field.Groups[2].Value] = field.Groups[1].Value;

                // A handler may answer through a method of its own file that
                // declares what it sends, the way a route declares its own
                // payload (fox ticket #4304).
                var declared = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (Match method in Declaration.Matches(text))
                {
                    string block = AttributeBlock(lines, LineOf(text, method.Index) - 1);
                    Match own = ResponseAttribute.Match(block);
                    if (!own.Success)
                        continue;
                    Group ownType = own.Groups["type"];
                    declared[method.Groups["name"].Value] =
                        ownType.Success ? ownType.Value : "none";
                }

                foreach (Match declaration in Declaration.Matches(text))
                {
                    int line = LineOf(text, declaration.Index);
                    string attributes = AttributeBlock(lines, line - 1);
                    Match attribute = RouteAttribute.Match(attributes);
                    if (!attribute.Success)
                        continue;

                    string body = Body(text, declaration.Index + declaration.Length - 1);
                    if (body == null)
                        continue;

                    routes.Add(
                        new Route(
                            attribute.Groups[1].Value.ToUpperInvariant(),
                            attribute.Groups[2].Value,
                            relative + ":" + line,
                            Area(relative, attribute.Groups[2].Value),
                            ResponseAttribute.Match(attributes),
                            ReadAnswer(body, fields, returns, declared)
                        )
                    );
                }
            }

            return routes;
        }

        /// <summary>(interface, method) → payload type, or null for a bare ApiResult.</summary>
        private static Dictionary<string, string> ServiceReturns(string root)
        {
            var returns = new Dictionary<string, string>(StringComparer.Ordinal);
            // A service answers either synchronously (ApiResult&lt;T&gt;) or as a
            // task the handler awaits (Task&lt;ApiResult&lt;T&gt;&gt;); both carry the same
            // envelope, so the task wrapper names no type of its own. Two
            // patterns, because one that made the wrapper optional would read
            // the wrapper's own closing angle as part of the payload type.
            var returnLine = new Regex(
                @"^\s*(?:public\s+|internal\s+)?ApiResult(?:<(?<type>[^;()]+)>)?\s+(?<name>\w+)\s*\(",
                RegexOptions.Compiled
            );
            var taskReturnLine = new Regex(
                @"^\s*(?:public\s+|internal\s+)?Task<ApiResult(?:<(?<type>[^;()]+)>)?>\s+(?<name>\w+)\s*\(",
                RegexOptions.Compiled
            );
            var interfaceLine = new Regex(@"\binterface\s+(?<name>\w+)", RegexOptions.Compiled);

            foreach (string path in SourceTree.Sources(root, "RimworldRestApi"))
            {
                string current = null;
                foreach (string line in File.ReadAllLines(path))
                {
                    Match declared = interfaceLine.Match(line);
                    if (declared.Success)
                    {
                        current = declared.Groups["name"].Value;
                        continue;
                    }
                    if (current == null)
                        continue;

                    Match match = returnLine.Match(line);
                    if (!match.Success)
                        match = taskReturnLine.Match(line);
                    if (match.Success)
                    {
                        Group type = match.Groups["type"];
                        returns[current + "." + match.Groups["name"].Value] =
                            type.Success && type.Value.Length > 0 ? type.Value : null;
                    }
                }
            }
            return returns;
        }

        /// <summary>
        /// What the handler answers: the payload of its inline ApiResult, or of
        /// the service method it calls. One string, "none" for no payload, and
        /// null when the source does not say (two different payloads, or none
        /// found at all).
        /// </summary>
        private static Answer ReadAnswer(
            string body,
            Dictionary<string, string> fields,
            Dictionary<string, string> returns,
            Dictionary<string, string> declared
        )
        {
            var payloads = new HashSet<string>(StringComparer.Ordinal);
            var hints = new List<string>();

            foreach (Match inline in InlineResult.Matches(body))
            {
                payloads.Add(inline.Groups["type"].Value.Trim());
                hints.Add("inline ApiResult<" + inline.Groups["type"].Value.Trim() + ">");
            }

            foreach (Match call in Call.Matches(body))
            {
                string interfaceName;
                if (!fields.TryGetValue(call.Groups[1].Value, out interfaceName))
                    continue;

                string key = interfaceName + "." + call.Groups[2].Value;
                string payload;
                if (!returns.TryGetValue(key, out payload))
                    continue;

                payloads.Add(payload ?? "none");
                hints.Add(key + " → " + (payload ?? "ApiResult"));
            }

            foreach (Match bare in BareCall.Matches(body))
            {
                string payload;
                if (!declared.TryGetValue(bare.Groups["name"].Value, out payload))
                    continue;

                payloads.Add(payload);
                hints.Add(bare.Groups["name"].Value + " → " + payload);
            }

            if (payloads.Count != 1)
                return new Answer(null, string.Join("; ", hints));
            return new Answer(payloads.First(), string.Join("; ", hints));
        }

        /// <summary>The wave a route belongs to; the dev namespace is its own area.</summary>
        private static string Area(string file, string path)
        {
            if (path.IndexOf("/api/v1/dev/", StringComparison.Ordinal) >= 0)
                return "dev";

            string[] parts = file.Split(Path.DirectorySeparatorChar);
            int controllers = Array.IndexOf(parts, "Controllers");
            string directory = controllers >= 0 && controllers + 1 < parts.Length
                ? parts[controllers + 1]
                : "other";

            switch (directory)
            {
                case "Pawns":
                    return "pawn";
                case "Colony":
                    return "colony";
                case "World":
                    return "map-world";
                case "System":
                    return "system";
                case "Client":
                    return "client";
                case "AI":
                    return "ai";
                default:
                    return "other";
            }
        }

        private sealed class Answer
        {
            public Answer(string payload, string hint)
            {
                Payload = payload;
                Hint = hint;
            }

            public string Payload { get; }
            public string Hint { get; }
        }

        private sealed class Route
        {
            private readonly Match _attribute;
            private readonly Answer _answer;

            public Route(
                string method,
                string path,
                string location,
                string area,
                Match attribute,
                Answer answer
            )
            {
                Method = method;
                Path = path;
                Location = location;
                Area = area;
                _attribute = attribute;
                _answer = answer;
            }

            public string Method { get; }
            public string Path { get; }
            public string Location { get; }
            public string Area { get; }

            public bool HasDeclaration => _attribute.Success;

            /// <summary>The declared type, "none" for a bare [ResponseBody], null when undeclared.</summary>
            public string Declaration
            {
                get
                {
                    if (!_attribute.Success)
                        return null;
                    Group type = _attribute.Groups["type"];
                    return type.Success ? type.Value : "none";
                }
            }

            /// <summary>What the handler answers; "none" or null as in Answer.</summary>
            public string Answered => _answer.Payload;

            public string AnswerHint => _answer.Hint;
        }

        private static string AttributeBlock(string[] lines, int declarationLine)
        {
            int start = declarationLine - 1;
            while (start >= 0)
            {
                string trimmed = lines[start].Trim();
                if (
                    trimmed.Length == 0
                    || trimmed == "}"
                    || trimmed == "{"
                    || trimmed.EndsWith(";", StringComparison.Ordinal)
                    || trimmed.StartsWith("//", StringComparison.Ordinal)
                    || trimmed.StartsWith("/*", StringComparison.Ordinal)
                )
                    break;
                start--;
            }
            return string.Join("\n", lines.Skip(start + 1).Take(declarationLine - start - 1));
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

        private static int LineOf(string text, int index)
        {
            int line = 1;
            for (int i = 0; i < index && i < text.Length; i++)
                if (text[i] == '\n')
                    line++;
            return line;
        }
    }
}
