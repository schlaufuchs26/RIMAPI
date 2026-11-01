using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// One sentence for a map id no map carries (ticket #3811). MapService has
    /// answered "Map not found: 7" since #3658, but TradeService, IdeoService,
    /// BillService, OrderService, BuilderService, ThingsService and
    /// ColonistService kept their own wordings ("Map with id: 7 not found",
    /// "Map 7 not found.", "Map with ID=7 not found"). A caller that matches the
    /// sentence to tell a wrong map id from an empty colony missed those routes,
    /// and a digest read the same failure as a different one. The sentence lives
    /// in MapLookup now; this test fails when a route words its own again.
    /// </summary>
    public class MapNotFoundWordingTests
    {
        // A string literal that names a map and says it is missing, with or
        // without the interpolation sigil. MapLookup.cs is the one file that
        // carries the sentence.
        private static readonly Regex OwnWording = new Regex(
            @"""[^""\n]*\bMap\b[^""\n]*not found", RegexOptions.Compiled);

        [Fact]
        public void NoRouteWordsTheMapNotFoundSentenceItself()
        {
            string root = SourceTree.RepoRoot();
            var problems = new List<string>();

            foreach (string path in SourceTree.Sources(root, "RimworldRestApi"))
            {
                string relative = SourceTree.Relative(root, path);
                if (relative.EndsWith("Helpers/MapLookup.cs", StringComparison.Ordinal))
                    continue;

                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    // A comment that mentions the sentence is documentation, not
                    // a second source of it.
                    int comment = lines[i].IndexOf("//", StringComparison.Ordinal);
                    string code = comment < 0 ? lines[i] : lines[i].Substring(0, comment);
                    if (!OwnWording.IsMatch(code))
                        continue;

                    problems.Add(
                        $"{relative}:{i + 1}: words the missing map itself "
                            + "(fail with MapLookup.Resolve's error, or MapLookup.MapNotFound(mapId))");
                }
            }

            Assert.True(
                problems.Count == 0,
                "A route that words its own missing-map sentence drifts from the others:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, problems));
        }
    }
}
