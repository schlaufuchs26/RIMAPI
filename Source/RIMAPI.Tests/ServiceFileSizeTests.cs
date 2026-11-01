using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// A service file stays readable (ticket #3809). MapService.cs reached 999
    /// lines and nothing in the build or the suite said so; only a hand-written
    /// ticket noticed, weeks after the fact. This test is the Go repo's
    /// filesize gate in the mod's own terms: a file under Services/ that
    /// crosses the cap fails the suite, so the split happens in the run that
    /// grows the file instead of in a later cleanup ticket.
    ///
    /// Six files were already past the cap when the gate landed. They are
    /// listed below and stay allowed as they are, because blocking every
    /// one-line edit to them would turn each unrelated feature into a
    /// refactor; the entry goes away when the file is split.
    /// </summary>
    public class ServiceFileSizeTests
    {
        // 600 lines is about the point where a service no longer fits in one
        // reading. The Go side caps at 500; the mod's services carry more
        // boilerplate per line (using blocks, XML doc comments, null checks),
        // so the same intent needs the higher number.
        private const int LineCap = 600;

        // Files that were already over the cap when this test was written, with
        // their size at that moment. Delete a line when you split the file.
        private static readonly Dictionary<string, int> AlreadyOverTheCap =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "Source/RIMAPI/RimworldRestApi/Services/Colony/TradeService/TradeService.cs", 618 },
                { "Source/RIMAPI/RimworldRestApi/Services/Client/LetterService/LetterService.cs", 706 },
                { "Source/RIMAPI/RimworldRestApi/Services/Pawns/CombatService/CombatService.cs", 774 },
                { "Source/RIMAPI/RimworldRestApi/Services/Colony/IdeoService.cs", 984 },
                { "Source/RIMAPI/RimworldRestApi/Services/Pawns/ColonistService/ColonistService.cs", 1048 },
                { "Source/RIMAPI/RimworldRestApi/Services/System/DevActionService/DevActionService.cs", 1150 },
            };

        [Fact]
        public void NoServiceFileCrossesTheLineCap()
        {
            string root = SourceTree.RepoRoot();
            var problems = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (string path in SourceTree.Sources(root, "RimworldRestApi", "Services"))
            {
                string relative = SourceTree.Relative(root, path);
                if (AlreadyOverTheCap.ContainsKey(relative))
                {
                    seen.Add(relative);
                    continue;
                }

                int lines = File.ReadAllLines(path).Length;
                if (lines > LineCap)
                {
                    problems.Add(
                        $"{relative}: {lines} lines, cap {LineCap} "
                            + $"(split it into partial files or a new service)");
                }
            }

            // A stale entry would quietly allow a recreated file to grow again.
            foreach (string missing in AlreadyOverTheCap.Keys.Where(p => !seen.Contains(p)))
            {
                problems.Add(
                    $"{missing}: listed as already over the cap, but no such file under "
                        + "Services/ (the file was split or renamed: remove the entry)");
            }

            Assert.True(
                problems.Count == 0,
                "A service file that big is read by nobody:" + Environment.NewLine
                    + string.Join(Environment.NewLine, problems));
        }

        [Fact]
        public void TheRecordedSizeIsNotAnAllowanceToGrow()
        {
            // The number beside a listed file is its size when the gate landed,
            // not a limit to keep under. A file that grew since then has to be
            // split rather than re-recorded, so the test names the growth.
            string root = SourceTree.RepoRoot();
            var problems = new List<string>();

            foreach (KeyValuePair<string, int> entry in AlreadyOverTheCap)
            {
                string path = Path.Combine(root, entry.Key.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path))
                    continue; // reported by the other test

                int lines = File.ReadAllLines(path).Length;
                if (lines > entry.Value)
                {
                    problems.Add(
                        $"{entry.Key}: {lines} lines, was {entry.Value} when the gate landed "
                            + "(split it instead of raising the number)");
                }
            }

            Assert.True(
                problems.Count == 0,
                "A listed service file grew instead of being split:" + Environment.NewLine
                    + string.Join(Environment.NewLine, problems));
        }
    }
}
