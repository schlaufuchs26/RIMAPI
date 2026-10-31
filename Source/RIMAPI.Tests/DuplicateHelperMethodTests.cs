using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// One copy of a read, in the helper (ticket #3398). MapHelper and MapService
    /// each carried their own GetMapTileId with the same body, and the two drifted
    /// far enough that #3339 had to fix the same lines twice: a version
    /// conditional and an unreachable throw lived in both. A service method that
    /// names a helper's method forwards to it (`=> MapHelper.X(...)`, the way
    /// UpdateStockpile does); this test fails when the bodies are the same text,
    /// because then the next edit lands in one copy and the other keeps the old
    /// behaviour.
    /// </summary>
    public class DuplicateHelperMethodTests
    {
        // A method declaration: access modifier, return type, name, then the
        // argument list. A call inside a body starts with a type or a variable,
        // never with an access modifier, so calls do not match.
        private static readonly Regex Declaration = new Regex(
            @"^[ \t]*(?:public|private|protected|internal)\b[^;=\(\)]*?\b(?<name>\w+)[ \t]*\(",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);

        [Fact]
        public void NoServiceRepeatsAHelperMethodBody()
        {
            string root = SourceTree.RepoRoot();
            List<MethodBody> helpers = Methods(root, "RimworldRestApi", "Helpers");
            List<MethodBody> services = Methods(root, "RimworldRestApi", "Services");

            List<string> problems = helpers
                .SelectMany(helper =>
                    services
                        .Where(service => service.Name == helper.Name && service.Body == helper.Body)
                        .Select(service => $"{service.Location} repeats {helper.Location} ({helper.Name})"))
                .ToList();

            Assert.True(
                problems.Count == 0,
                "A service method that copies a helper's body drifts from it; forward to the helper instead:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, problems));
        }

        private static List<MethodBody> Methods(string root, params string[] subdirs)
        {
            var methods = new List<MethodBody>();
            foreach (string path in SourceTree.Sources(root, subdirs))
            {
                string text = File.ReadAllText(path);
                foreach (Match match in Declaration.Matches(text))
                {
                    string body = Body(text, match.Index + match.Length - 1);
                    if (body == null)
                        continue;

                    body = Whitespace.Replace(body, string.Empty);
                    if (body.Length == 0)
                        continue;

                    methods.Add(
                        new MethodBody(
                            match.Groups["name"].Value,
                            body,
                            SourceTree.Relative(root, path) + ":" + LineNumber(text, match.Index)));
                }
            }
            return methods;
        }

        /// <summary>
        /// The body that follows an argument list: a brace block, or an
        /// expression-bodied member up to its semicolon. Null for a declaration
        /// without one (an abstract or interface member).
        /// </summary>
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

            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
            if (i >= text.Length)
                return null;

            if (text[i] == '{')
                return BraceBlock(text, i);

            if (text[i] == '=' && i + 1 < text.Length && text[i + 1] == '>')
            {
                int end = text.IndexOf(';', i + 2);
                return end < 0 ? null : text.Substring(i + 2, end - i - 2);
            }

            return null;
        }

        private static string BraceBlock(string text, int openBrace)
        {
            int depth = 0;
            for (int i = openBrace; i < text.Length; i++)
            {
                if (text[i] == '{')
                    depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return text.Substring(openBrace + 1, i - openBrace - 1);
                }
            }
            return null;
        }

        private static int LineNumber(string text, int index)
        {
            int line = 1;
            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                    line++;
            }
            return line;
        }

        private sealed class MethodBody
        {
            public MethodBody(string name, string body, string location)
            {
                Name = name;
                Body = body;
                Location = location;
            }

            public string Name { get; }

            public string Body { get; }

            public string Location { get; }
        }
    }
}
