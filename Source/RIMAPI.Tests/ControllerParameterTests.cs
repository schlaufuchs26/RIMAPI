using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// One query parameter, one read (ticket #3430). MapController's single-zone
    /// growing read assigned both `mapId` and `zoneId` from
    /// RequestParser.GetMapId, so the route read the map id twice and answered
    /// null for every real zone. A second read of the same parameter inside one
    /// method is almost always the wrong name, so this test fails on that shape
    /// rather than on the one route that carried it.
    /// </summary>
    public class ControllerParameterTests
    {
        // The parsers that take no parameter name, and the query parameter each
        // one reads. A new such parser belongs here with its parameter.
        private static readonly Dictionary<string, string> ParserParameter =
            new Dictionary<string, string>
            {
                { "GetMapId", "map_id" },
                { "GetZoneId", "zone_id" },
            };

        // Presence checks, not reads: `HasParameter(context, "x") ? GetIntParameter(context, "x") : -1`
        // asks twice about one parameter on purpose, so they never count as a read.
        private static readonly HashSet<string> PresenceOnly = new HashSet<string>
        {
            "HasParameter",
        };

        // RequestParser.X(context) or RequestParser.X(context, "literal").
        private static readonly Regex ParserCall = new Regex(
            @"RequestParser\.(?<method>\w+)[ \t]*\((?<args>[^)]*)\)",
            RegexOptions.Compiled);

        private static readonly Regex LiteralName = new Regex(
            @"""(?<name>\w+)""", RegexOptions.Compiled);

        [Fact]
        public void NoRouteReadsOneQueryParameterTwice()
        {
            string root = SourceTree.RepoRoot();
            List<SourceMethod> routes = SourceTree.Methods(
                SourceTree.Sources(root, "RimworldRestApi", "Controllers"), root);

            List<string> problems = routes
                .SelectMany(route => Reads(route)
                    .GroupBy(read => read)
                    .Where(same => same.Count() > 1)
                    .Select(same => $"{route.Location}: {route.Name} reads \"{same.Key}\" {same.Count()} times"))
                .ToList();

            Assert.True(
                problems.Count == 0,
                "A route that reads one query parameter twice reads the wrong name the second time:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, problems));
        }

        /// <summary>
        /// The single-zone growing read answers for one zone on one map, so it
        /// needs both ids: the map's and the zone's.
        /// </summary>
        [Fact]
        public void TheGrowingZoneReadAsksForAMapAndAZoneId()
        {
            string root = SourceTree.RepoRoot();
            SourceMethod route = SourceTree
                .Methods(SourceTree.Sources(root, "RimworldRestApi", "Controllers"), root)
                .SingleOrDefault(method => method.Name == "GetMapGrowingZoneById");

            Assert.True(route != null, "no controller method named GetMapGrowingZoneById");

            List<string> reads = Reads(route).Distinct().OrderBy(name => name, StringComparer.Ordinal).ToList();
            Assert.True(
                reads.SequenceEqual(new[] { "map_id", "zone_id" }),
                $"{route.Location} reads [{string.Join(", ", reads)}] instead of [map_id, zone_id]");
        }

        /// <summary>The query parameters a method reads, one entry per call.</summary>
        private static IEnumerable<string> Reads(SourceMethod method)
        {
            foreach (Match call in ParserCall.Matches(method.Body))
            {
                if (PresenceOnly.Contains(call.Groups["method"].Value))
                    continue;

                string name;
                if (ParserParameter.TryGetValue(call.Groups["method"].Value, out name))
                {
                    yield return name;
                    continue;
                }

                Match literal = LiteralName.Match(call.Groups["args"].Value);
                if (literal.Success)
                    yield return literal.Groups["name"].Value;
            }
        }
    }
}
