using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// One code path for RimWorld 1.6 (ticket #3339). Before it, every
    /// `#if RIMWORLD_1_5 ... #elif RIMWORLD_1_6 ... #endif` had a half the
    /// compiler never saw, and a change written into that half (the walk-scan
    /// commits did exactly that) built green without ever running. The version
    /// symbols are gone from the csproj, so a reintroduced conditional would
    /// silently select nothing; this test fails instead.
    /// </summary>
    public class NoVersionConditionalTests
    {
        // Matches a conditional on a version symbol, e.g. "#if RIMWORLD_1_5".
        private static readonly Regex VersionConditional =
            new Regex(@"^\s*#(if|elif)\b.*\bRIMWORLD_1_", RegexOptions.Compiled);

        [Fact]
        public void TheModSourceCarriesNoRimWorldVersionConditional()
        {
            string root = RepoRoot();
            List<string> hits = Directory
                .EnumerateFiles(Path.Combine(root, "Source", "RIMAPI"), "*.cs", SearchOption.AllDirectories)
                .Where(IsModSource)
                .SelectMany(file =>
                    File.ReadLines(file)
                        .Select((line, index) => new { line, number = index + 1 })
                        .Where(l => VersionConditional.IsMatch(l.line))
                        .Select(l => $"{Relative(root, file)}:{l.number}: {l.line.Trim()}"))
                .ToList();

            Assert.True(
                hits.Count == 0,
                "RimWorld 1.6 is the only target (#3285), so a version conditional "
                    + "compiles dead code or nothing at all:" + Environment.NewLine
                    + string.Join(Environment.NewLine, hits));
        }

        // Build output sits under the project directory too; obj/ holds generated
        // sources that repeat the directives.
        private static bool IsModSource(string path)
        {
            string[] parts = path.Split(Path.DirectorySeparatorChar);
            return !parts.Contains("obj") && !parts.Contains("bin");
        }

        // The test binary lives in Source/RIMAPI.Tests/bin/<config>/<tfm>/; walk up
        // until the mod project is a sibling, so the test finds its repo from any
        // checkout (the worker worktree included).
        private static string RepoRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null
                   && !File.Exists(Path.Combine(dir.FullName, "Source", "RIMAPI", "RimApi.csproj")))
            {
                dir = dir.Parent;
            }

            Assert.NotNull(dir);
            return dir.FullName;
        }

        private static string Relative(string root, string path) =>
            path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
    }
}
