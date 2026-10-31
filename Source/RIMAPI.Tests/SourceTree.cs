using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The source-level checks (tickets #3339, #3377) read the mod's own files
    /// instead of compiling them, so they all need the same two things: the
    /// repo root, and a file list that skips build output. Build output sits
    /// under the project directory too, and obj/ holds generated sources that
    /// repeat whatever the tests are looking for.
    /// </summary>
    internal static class SourceTree
    {
        // A method declaration: access modifier, return type, name, then the
        // argument list. A call inside a body starts with a type or a variable,
        // never with an access modifier, so calls do not match.
        private static readonly Regex Declaration = new Regex(
            @"^[ \t]*(?:public|private|protected|internal)\b[^;=\(\)]*?\b(?<name>\w+)[ \t]*\(",
            RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Every .cs file under Source/RIMAPI/&lt;subdirs&gt;, build output skipped.
        /// </summary>
        public static IEnumerable<string> Sources(string root, params string[] subdirs)
        {
            string dir = Path.Combine(new[] { root, "Source", "RIMAPI" }.Concat(subdirs).ToArray());
            if (!Directory.Exists(dir))
                throw new InvalidOperationException("no source directory at " + dir);

            return Directory
                .EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Where(IsSource)
                .OrderBy(path => path, StringComparer.Ordinal);
        }

        /// <summary>
        /// Every method in those files: its name, its body and `file:line`. Two
        /// checks read method bodies (a service copying a helper's, a route
        /// reading one query parameter twice), so the splitting lives here
        /// instead of in each of them.
        /// </summary>
        public static List<SourceMethod> Methods(IEnumerable<string> paths, string root)
        {
            var methods = new List<SourceMethod>();
            foreach (string path in paths)
            {
                string text = File.ReadAllText(path);
                foreach (Match match in Declaration.Matches(text))
                {
                    string body = Body(text, match.Index + match.Length - 1);
                    if (body == null || body.Trim().Length == 0)
                        continue;

                    methods.Add(
                        new SourceMethod(
                            match.Groups["name"].Value,
                            body,
                            Relative(root, path) + ":" + LineNumber(text, match.Index)));
                }
            }
            return methods;
        }

        /// <summary>Whitespace removed, so two bodies compare by their code alone.</summary>
        public static string Squash(string text) => Whitespace.Replace(text, string.Empty);

        /// <summary>
        /// The test binary lives in Source/RIMAPI.Tests/bin/&lt;config&gt;/&lt;tfm&gt;/;
        /// walk up until the mod project is a sibling, so the test finds its
        /// repo from any checkout (the worker worktree included).
        /// </summary>
        public static string RepoRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null
                   && !File.Exists(Path.Combine(dir.FullName, "Source", "RIMAPI", "RimApi.csproj")))
            {
                dir = dir.Parent;
            }

            if (dir == null)
                throw new InvalidOperationException(
                    "no Source/RIMAPI/RimApi.csproj above " + AppContext.BaseDirectory);

            return dir.FullName;
        }

        public static string Relative(string root, string path) =>
            path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);

        private static bool IsSource(string path)
        {
            string[] parts = path.Split(Path.DirectorySeparatorChar);
            return !parts.Contains("obj") && !parts.Contains("bin");
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
    }

    /// <summary>One method of the mod's source, as the source-level checks see it.</summary>
    internal sealed class SourceMethod
    {
        public SourceMethod(string name, string body, string location)
        {
            Name = name;
            Body = body;
            Location = location;
        }

        public string Name { get; }

        public string Body { get; }

        /// <summary>`file:line` of the declaration, for a readable failure.</summary>
        public string Location { get; }
    }
}
