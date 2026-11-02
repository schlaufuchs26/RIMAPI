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
    /// builds its rare state with them, but they answer a refusal unless the
    /// game itself runs in dev mode. These tests pin both halves: the gate's
    /// two answers, and the fact that the routes which need it are on the list.
    /// </summary>
    public class DevOnlyRouteTests
    {
        private static readonly Regex RouteAttribute = new Regex(
            @"\[(Get|Post|Put|Delete)\(""([^""]+)""\)\]", RegexOptions.Compiled);

        [Theory]
        [InlineData("/api/v1/dev/action")]
        [InlineData("/api/v1/dev/console")]
        [InlineData("/api/v1/pawn/edit/health")]
        [InlineData("/api/v1/pawn/edit/position")]
        [InlineData("/api/v1/pawn/spawn")]
        [InlineData("/api/v1/item/spawn")]
        [InlineData("/api/v1/map/droppod")]
        [InlineData("/api/v1/map/destroy/rect")]
        [InlineData("/api/v1/map/repair/rect")]
        [InlineData("/api/v1/map/weather/change")]
        [InlineData("/api/v1/incident/trigger")]
        [InlineData("/api/v1/faction/change/goodwill")]
        [InlineData("/api/v1/game/start/devquick")]
        [InlineData("/api/v1/game/send/letter")]
        [InlineData("/api/v1/lords/create")]
        [InlineData("/api/v1/pawns/interactions/force")]
        [InlineData("/api/v1/pawns/relations/add")]
        [InlineData("/api/v1/pawns/relations/remove")]
        public void ADevRouteIsDevOnly(string path)
        {
            Assert.True(DevOnlyRoutes.IsDevOnly(path));
        }

        [Theory]
        [InlineData("/api/v1/colonists")]
        [InlineData("/api/v1/pawn/edit")]
        [InlineData("/api/v1/pawn/spawned")]
        [InlineData("/api/v1/map/repair")]
        [InlineData("/api/v1/pawns/relations")]
        [InlineData("/api/v1/pawns/interactions")]
        [InlineData("/api/v1/map/destroy")]
        [InlineData("/api/v1/pawn/draft")]
        public void AnOrdinaryRouteIsNot(string path)
        {
            // A prefix that ends in "/" must not match its own parent, and one
            // that does not must not swallow a longer route: /api/v1/pawn/spawn
            // is a route, /api/v1/pawn/spawned is not a dev action.
            Assert.False(DevOnlyRoutes.IsDevOnly(path));
        }

        [Fact]
        public void AQueryStringDoesNotHideADevRoute()
        {
            Assert.True(DevOnlyRoutes.IsDevOnly("/api/v1/item/spawn?map_id=0"));
        }

        [Fact]
        public void DevModeOnLetsTheRouteThrough()
        {
            Assert.Null(DevOnlyRoutes.Refuse("/api/v1/pawn/edit/skills", devMode: true));
        }

        [Fact]
        public void DevModeOffAnswersTheRefusal()
        {
            string refusal = DevOnlyRoutes.Refuse("/api/v1/pawn/edit/skills", devMode: false);

            Assert.NotNull(refusal);
            // The sentence names the route, the rule and where dev work runs.
            Assert.Contains("/api/v1/pawn/edit/skills", refusal);
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
        public void EveryRouteOfADevControllerIsOnTheList()
        {
            // PawnEditController, PawnSpawnController and DevToolsController exist
            // only for dev work. A route added to one of them is a god route by
            // construction, so the gate has to cover it without a second thought.
            string root = SourceTree.RepoRoot();
            var problems = new List<string>();
            int checkedRoutes = 0;

            foreach (string path in SourceTree.Sources(root, "RimworldRestApi"))
            {
                string name = Path.GetFileName(path);
                if (
                    name != "PawnEditController.cs"
                    && name != "PawnSpawnController.cs"
                    && name != "DevToolsController.cs"
                )
                    continue;

                foreach (Match match in RouteAttribute.Matches(File.ReadAllText(path)))
                {
                    checkedRoutes++;
                    string route = match.Groups[2].Value;
                    if (!DevOnlyRoutes.IsDevOnly(route))
                        problems.Add($"{SourceTree.Relative(root, path)}: {route} is not dev-only");
                }
            }

            Assert.True(checkedRoutes > 0, "no dev controller route was found at all");
            Assert.True(problems.Count == 0, string.Join("\n", problems));
        }

        [Fact]
        public void EveryGodRouteOutsideThoseControllersIsOnTheList()
        {
            // The god routes that live in an ordinary controller, one by one.
            // The check fails twice over: when a route is missing from the
            // source (renamed) and when it exists but the gate does not cover it.
            string root = SourceTree.RepoRoot();
            var all = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in SourceTree.Sources(root, "RimworldRestApi"))
            {
                foreach (Match match in RouteAttribute.Matches(File.ReadAllText(path)))
                    all.Add(match.Groups[2].Value);
            }

            var godRoutes = new[]
            {
                "/api/v1/item/spawn",
                "/api/v1/map/droppod",
                "/api/v1/map/destroy/rect",
                "/api/v1/map/destroy/corpses",
                "/api/v1/map/destroy/forbidden",
                "/api/v1/map/repair/rect",
                "/api/v1/map/repair/positions",
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

            var problems = new List<string>();
            foreach (string route in godRoutes)
            {
                if (!all.Contains(route))
                    problems.Add(route + ": no such route in the controllers any more");
                else if (!DevOnlyRoutes.IsDevOnly(route))
                    problems.Add(route + ": exists and is not dev-only");
            }

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
