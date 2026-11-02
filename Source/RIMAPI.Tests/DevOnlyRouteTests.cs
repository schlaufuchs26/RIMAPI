using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RIMAPI.Helpers;
using Verse;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The dev boundary (fox ticket #4212). The mod is the interface to the
    /// game: it does what a player can do and nothing a player cannot. The
    /// routes that do what only the game's dev tools do stay, because a check
    /// builds its rare state with them, but they live under /api/v1/dev/ and
    /// answer a refusal unless the game itself runs in dev mode. These tests pin
    /// both halves: the namespace is complete, and the gate's two answers.
    /// </summary>
    public class DevOnlyRouteTests
    {
        private static readonly Regex RouteAttribute = new Regex(
            @"\[(Get|Post|Put|Delete)\(""([^""]+)""\)\]", RegexOptions.Compiled);

        // What a god route used to look like before the move: a route with one
        // of these prefixes outside /api/v1/dev/ is a route the parity rule
        // forbids, whatever its handler does.
        private static readonly string[] GodPrefixes =
        {
            "/api/v1/pawn/edit/",
            "/api/v1/pawn/spawn",
            "/api/v1/item/spawn",
            "/api/v1/map/droppod",
            "/api/v1/map/destroy/",
            "/api/v1/map/repair/",
            "/api/v1/map/weather/change",
            "/api/v1/incident/trigger",
            "/api/v1/faction/change/goodwill",
            "/api/v1/game/start/devquick",
            "/api/v1/game/send/letter",
            "/api/v1/lords/create",
            "/api/v1/pawns/interactions/force",
            "/api/v1/pawns/relations/add",
            "/api/v1/pawns/relations/remove",
        };

        [Theory]
        [InlineData("/api/v1/dev/action")]
        [InlineData("/api/v1/dev/console")]
        [InlineData("/api/v1/dev/pawn/edit/health")]
        [InlineData("/api/v1/dev/pawn/edit/position")]
        [InlineData("/api/v1/dev/pawn/spawn")]
        [InlineData("/api/v1/dev/item/spawn")]
        [InlineData("/api/v1/dev/map/droppod")]
        [InlineData("/api/v1/dev/map/destroy/rect")]
        [InlineData("/api/v1/dev/map/repair/rect")]
        [InlineData("/api/v1/dev/map/weather/change")]
        [InlineData("/api/v1/dev/incident/trigger")]
        [InlineData("/api/v1/dev/faction/change/goodwill")]
        [InlineData("/api/v1/dev/game/start/devquick")]
        [InlineData("/api/v1/dev/game/send/letter")]
        [InlineData("/api/v1/dev/lords/create")]
        [InlineData("/api/v1/dev/pawns/interactions/force")]
        [InlineData("/api/v1/dev/pawns/relations/add")]
        [InlineData("/api/v1/dev/pawns/relations/remove")]
        public void ADevRouteIsDevOnly(string path)
        {
            Assert.True(DevOnlyRoutes.IsDevOnly(path));
        }

        [Theory]
        [InlineData("/api/v1/colonists")]
        [InlineData("/api/v1/dev")]
        [InlineData("/api/v1/devx/action")]
        [InlineData("/api/v1/pawn/draft")]
        [InlineData("/api/v1/pawns/relations")]
        [InlineData("/api/v1/game/speed")]
        public void AnOrdinaryRouteIsNot(string path)
        {
            // The prefix must match the namespace and not a lookalike: /api/v1/dev
            // is not the namespace, /api/v1/devx is not either.
            Assert.False(DevOnlyRoutes.IsDevOnly(path));
        }

        [Fact]
        public void AQueryStringDoesNotHideADevRoute()
        {
            Assert.True(DevOnlyRoutes.IsDevOnly("/api/v1/dev/item/spawn?map_id=0"));
        }

        [Fact]
        public void DevModeOnLetsTheRouteThrough()
        {
            Assert.Null(DevOnlyRoutes.Refuse("/api/v1/dev/pawn/edit/skills", devMode: true));
        }

        [Fact]
        public void DevModeOffAnswersTheRefusal()
        {
            string refusal = DevOnlyRoutes.Refuse("/api/v1/dev/pawn/edit/skills", devMode: false);

            Assert.NotNull(refusal);
            // The sentence names the route, the rule and the namespace.
            Assert.Contains("/api/v1/dev/pawn/edit/skills", refusal);
            Assert.Contains("does not run in dev mode", refusal);
            Assert.Contains("does what a player can do and nothing more", refusal);
            Assert.Contains("/api/v1/dev/", refusal);
        }

        [Fact]
        public void AnOrdinaryRouteAnswersNothingEitherWay()
        {
            Assert.Null(DevOnlyRoutes.Refuse("/api/v1/colonists", devMode: false));
            Assert.Null(DevOnlyRoutes.Refuse("/api/v1/colonists", devMode: true));
        }

        [Fact]
        public void TheGateFollowsTheGamesOwnDevModeSwitch()
        {
            // The one line that reads the game, driven through the Prefs stub:
            // dev mode on is the debug game, off is the colony a play run watches.
            try
            {
                Prefs.DevMode = false;
                Assert.NotNull(DevGate.Refuse("/api/v1/dev/action"));

                Prefs.DevMode = true;
                Assert.Null(DevGate.Refuse("/api/v1/dev/action"));
            }
            finally
            {
                Prefs.DevMode = false;
            }
        }

        [Fact]
        public void NoGodRouteLivesOutsideTheDevNamespace()
        {
            // Evan's rule, as a test: a route that does what only the dev tools
            // do belongs under /api/v1/dev/. A new one in an ordinary controller
            // fails here, whether it is gated or not.
            string root = SourceTree.RepoRoot();
            var problems = new List<string>();
            int devRoutes = 0;
            int checkedRoutes = 0;

            foreach (string path in SourceTree.Sources(root, "RimworldRestApi"))
            {
                foreach (Match match in RouteAttribute.Matches(File.ReadAllText(path)))
                {
                    string route = match.Groups[2].Value;
                    checkedRoutes++;
                    if (DevOnlyRoutes.IsDevOnly(route))
                    {
                        devRoutes++;
                        continue;
                    }
                    foreach (string prefix in GodPrefixes)
                    {
                        if (route == prefix || route.StartsWith(prefix, StringComparison.Ordinal))
                            problems.Add($"{SourceTree.Relative(root, path)}: {route} is a god route outside /api/v1/dev/");
                    }
                }
            }

            Assert.True(checkedRoutes > 0, "no route was found at all");
            Assert.True(devRoutes > 20, "the dev namespace holds only " + devRoutes + " routes; the move looks incomplete");
            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        [Fact]
        public void TheRouterAsksTheGateBeforeAControllerRuns()
        {
            string root = SourceTree.RepoRoot();
            string registry = Path.Combine(
                root,
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Core",
                "Routing",
                "AutoRouteRegistry.cs"
            );

            string text = File.ReadAllText(registry);
            Assert.Contains("DevGate.Refuse(", text);
        }
    }
}
