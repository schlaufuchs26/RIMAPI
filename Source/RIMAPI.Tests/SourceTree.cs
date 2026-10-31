using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    }
}
