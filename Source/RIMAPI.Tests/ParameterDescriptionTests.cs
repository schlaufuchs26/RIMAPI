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
    /// Every request parameter and every response field says what it means
    /// (fox ticket #4313).
    /// </summary>
    /// <remarks>
    /// A name and a type are not documentation: a reader of the docs page
    /// cannot tell what `count`, `radius` or `quality` mean, what unit they
    /// take or what the default is. The text sits next to the C# parameter or
    /// property (a [ParameterDescription], or the `=text` part of a
    /// [QueryParameters] entry) and the shared names take it from
    /// ParameterDescriptions, so the document is rendered from the source and
    /// cannot drift from it. These checks read the source files: the test
    /// project compiles the documentation model, not the controllers or the
    /// models.
    /// </remarks>
    public class ParameterDescriptionTests
    {
        private static readonly Regex QueryAttribute = new Regex(
            @"\[QueryParameters\((?<body>[^)]*)\)\]",
            RegexOptions.Compiled
        );

        private static readonly Regex StringLiteral = new Regex(
            "\"(?:[^\"\\\\]|\\\\.)*\"",
            RegexOptions.Compiled
        );

        private static readonly Regex ModelClass = new Regex(
            @"^    (?:public|internal)\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|struct)\s+(?<name>\w+)",
            RegexOptions.Multiline | RegexOptions.Compiled
        );

        private static readonly Regex ModelProperty = new Regex(
            @"^        public\s+[\w<>\[\],\.\? ]+?\s+(?<name>\w+)\s*\{\s*get;\s*set;\s*\}",
            RegexOptions.Multiline | RegexOptions.Compiled
        );

        private static readonly Regex Handler = new Regex(
            @"^[ \t]*(?:public|private|protected|internal)\b[^;=\(\)]*?\b(?<name>\w+)[ \t]*\((?<args>[^)]*)\)",
            RegexOptions.Multiline | RegexOptions.Compiled
        );

        private static readonly Regex JsonName = new Regex(
            @"\[JsonProperty\(\s*""(?<name>[^""]+)""",
            RegexOptions.Compiled
        );

        [Fact]
        public void EveryQueryParameterDeclaresWhatItMeans()
        {
            var problems = new List<string>();

            foreach (string path in ControllerFiles())
            {
                string[] lines = File.ReadAllLines(path);
                for (int index = 0; index < lines.Length; index++)
                {
                    Match attribute = QueryAttribute.Match(lines[index]);
                    if (!attribute.Success)
                        continue;

                    foreach (Match literal in StringLiteral.Matches(attribute.Groups["body"].Value))
                    {
                        string spec = Unescape(literal.Value);
                        try
                        {
                            QueryParameterSpec.Parse(spec);
                        }
                        catch (ArgumentException error)
                        {
                            problems.Add(
                                SourceTree.Relative(SourceTree.RepoRoot(), path) + ":" + (index + 1)
                                    + " " + error.Message
                            );
                        }
                    }
                }
            }

            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        [Fact]
        public void EveryHandlerParameterDeclaresWhatItMeans()
        {
            var problems = new List<string>();

            foreach (string path in ControllerFiles())
            {
                string text = File.ReadAllText(path);
                string[] lines = File.ReadAllLines(path);
                foreach (Match handler in Handler.Matches(text))
                {
                    // Only a route handler takes parameters from the request;
                    // a constructor's arguments are services, not the API.
                    int line = text.Substring(0, handler.Index).Split('\n').Length - 1;
                    if (!AttributeBlock(lines, line).Contains("[Get(")
                        && !AttributeBlock(lines, line).Contains("[Post(")
                        && !AttributeBlock(lines, line).Contains("[Put(")
                        && !AttributeBlock(lines, line).Contains("[Delete("))
                        continue;

                    string args = handler.Groups["args"].Value;
                    foreach (string argument in SplitArguments(args))
                    {
                        string trimmed = argument.Trim();
                        if (trimmed.Length == 0 || trimmed.Contains("HttpListenerContext"))
                            continue;

                        Match name = Regex.Match(trimmed, @"(?<name>\w+)\s*(?:=|$)");
                        string parameter = name.Success ? name.Groups["name"].Value : trimmed;
                        if (trimmed.Contains("ParameterDescription(")
                            || ParameterDescriptions.For(parameter) != null)
                            continue;

                        problems.Add(
                            SourceTree.Relative(SourceTree.RepoRoot(), path) + " "
                                + handler.Groups["name"].Value + "(" + parameter
                                + ") has no description"
                        );
                    }
                }
            }

            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        [Fact]
        public void EveryModelFieldDeclaresWhatItMeans()
        {
            var problems = new List<string>();

            foreach (string path in ModelFiles())
            {
                string[] lines = File.ReadAllLines(path);
                string current = null;

                for (int index = 0; index < lines.Length; index++)
                {
                    Match type = ModelClass.Match(lines[index]);
                    if (type.Success)
                    {
                        current = type.Groups["name"].Value;
                        continue;
                    }

                    Match property = ModelProperty.Match(lines[index]);
                    if (!property.Success || current == null)
                        continue;
                    if (PendingFieldDescriptions.Types.Contains(current))
                        continue;

                    string block = AttributeBlock(lines, index);
                    Match declared = Regex.Match(
                        block,
                        @"\[ParameterDescription\(\s*""(?<text>(?:[^""\\]|\\.)*)"""
                    );
                    Match renamed = JsonName.Match(block);
                    string field = renamed.Success
                        ? renamed.Groups["name"].Value
                        : Wire(property.Groups["name"].Value);

                    string description = declared.Success
                        ? Unescape("\"" + declared.Groups["text"].Value + "\"")
                        : ParameterDescriptions.For(field);

                    string problem = ParameterDescriptions.Why(description, field);
                    if (problem != null)
                    {
                        problems.Add(
                            SourceTree.Relative(SourceTree.RepoRoot(), path) + " "
                                + current + "." + property.Groups["name"].Value
                                + " " + problem
                        );
                    }
                }
            }

            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        [Fact]
        public void ADescriptionMustSayMoreThanTheName()
        {
            Assert.False(ParameterDescriptions.IsUseful("pawn id", "pawn_id"));
            Assert.False(ParameterDescriptions.IsUseful("The map id", "map_id"));
            Assert.False(ParameterDescriptions.IsUseful("Two words", "count"));
            Assert.True(
                ParameterDescriptions.IsUseful(
                    "The map's numeric id; 0 is the player's home map.",
                    "map_id"
                )
            );
        }

        [Fact]
        public void ASharedNameKeepsOneSentence()
        {
            QueryParameterSpec spec = QueryParameterSpec.Parse("map_id:integer");
            Assert.Equal(ParameterDescriptions.For("map_id"), spec.Description);

            QueryParameterSpec own = QueryParameterSpec.Parse(
                "id:integer=The colonist's numeric id, as the pawn list reports it."
            );
            Assert.Equal(
                "The colonist's numeric id, as the pawn list reports it.",
                own.Description
            );

            ArgumentException error = Assert.Throws<ArgumentException>(
                () => QueryParameterSpec.Parse("unknown_thing:integer")
            );
            Assert.Contains("no description", error.Message);
        }

        private static IEnumerable<string> ControllerFiles() =>
            SourceTree
                .Sources(SourceTree.RepoRoot(), "RimworldRestApi", "Controllers")
                .Concat(
                    SourceTree.Sources(SourceTree.RepoRoot(), "RimworldRestApi", "BaseControllers")
                );

        private static IEnumerable<string> ModelFiles() =>
            SourceTree.Sources(SourceTree.RepoRoot(), "RimworldRestApi", "Models");

        /// <summary>The attribute lines directly above a member.</summary>
        private static string AttributeBlock(string[] lines, int line)
        {
            var block = new List<string>();
            for (int index = line - 1; index >= 0; index--)
            {
                string trimmed = lines[index].Trim();
                if (trimmed.StartsWith("[", StringComparison.Ordinal)
                    || trimmed.StartsWith("///", StringComparison.Ordinal))
                {
                    block.Add(trimmed);
                    continue;
                }
                break;
            }
            return string.Join("\n", block);
        }

        /// <summary>The wire name the snake_case resolver gives a property.</summary>
        private static string Wire(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var text = new System.Text.StringBuilder();
            text.Append(char.ToLowerInvariant(name[0]));
            for (int index = 1; index < name.Length; index++)
            {
                if (char.IsUpper(name[index]))
                {
                    text.Append('_');
                    text.Append(char.ToLowerInvariant(name[index]));
                }
                else
                {
                    text.Append(name[index]);
                }
            }
            return text.ToString();
        }

        private static IEnumerable<string> SplitArguments(string args)
        {
            int depth = 0;
            int start = 0;
            for (int index = 0; index < args.Length; index++)
            {
                char c = args[index];
                if (c == '<' || c == '(' || c == '[')
                    depth++;
                else if (c == '>' || c == ')' || c == ']')
                    depth--;
                else if (c == ',' && depth == 0)
                {
                    yield return args.Substring(start, index - start);
                    start = index + 1;
                }
            }
            yield return args.Substring(start);
        }

        private static string Unescape(string literal) =>
            literal.Substring(1, literal.Length - 2).Replace("\\\"", "\"");
    }
}
