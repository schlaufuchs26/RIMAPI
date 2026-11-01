using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// One lookup for the pawn an order names (fox ticket #3719). #3665 fixed
    /// `rescue` and `tend` and left the rest on `map.listerThings`, which holds
    /// only spawned things, so a colonist a raider carried off the map answered
    /// "Pawn with ID=N not found" on the other routes and read as a wrong id.
    /// These checks keep the one lookup and the carried sentence in place: a new
    /// order route that looks a pawn up in the spawned list, or that resolves a
    /// carried pawn and then orders it around, fails here.
    /// </summary>
    public class CarriedOrderLookupTests
    {
        // The routes whose named pawn is the one being acted on. Their answer
        // names the carrier, because no order can reach him there.
        private static readonly string[] TargetRoutes =
        {
            "MakeJobArrest",
            "MakeJobChat",
            "MakeJobFeed",
            "MakeJobPrioritize",
            "MakeJobRescue",
            "MakeJobSlaughter",
            "MakeJobTame",
            "MakeJobTend",
            "SetAllowedArea",
            "SetPrisonerInteraction",
        };

        // The routes whose named pawn would do the work himself. CombatRefusal's
        // state already reads "being carried by X", so these answer with
        // ActorRefusal.
        private static readonly string[] ActorRoutes =
        {
            "MakeJobEquip",
            "MakeJobHaul",
        };

        /// <summary>
        /// The spawned-things list holds no carried pawn: a service that reads
        /// `listerThings` for a Pawn re-opens #3719 for its own route.
        /// </summary>
        [Fact]
        public void NoServiceLooksAPawnUpInTheSpawnedThingsList()
        {
            string root = SourceTree.RepoRoot();
            List<SourceMethod> services = SourceTree
                .Methods(SourceTree.Sources(root, "RimworldRestApi", "Services"), root)
                .Where(m => m.Body.Contains(".listerThings") && m.Body.Contains("OfType<Pawn>"))
                .ToList();

            Assert.True(
                services.Count == 0,
                "listerThings holds only spawned things, so a carried pawn reads as \"not found\"; "
                    + "use PawnHelper.FindPawnForOrder:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, services.Select(m => m.Location + " (" + m.Name + ")")));
        }

        /// <summary>
        /// Every route that acts on the named pawn answers a carried one with the
        /// sentence that names the carrier, instead of resolving him and then
        /// failing on a rule that reads like something else.
        /// </summary>
        [Fact]
        public void EveryTargetRouteRefusesACarriedPawnByName()
        {
            List<string> missing = Missing(
                TargetRoutes,
                body => body.Contains("CarriedRefusal"),
                "a carried pawn is despawned into his carrier's hands; the route must say so "
                    + "with CombatRefusal.CarriedRefusal and its own verb");

            Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
        }

        /// <summary>
        /// The two routes where the named pawn would do the work: his own state
        /// already names the carrier, so ActorRefusal is the sentence that keeps
        /// "not found" off a carried pawn.
        /// </summary>
        [Fact]
        public void EveryActorRouteRefusesACarriedPawnByName()
        {
            List<string> missing = Missing(
                ActorRoutes,
                body => body.Contains("ActorRefusal") && body.Contains("RefusalStateOf"),
                "the named pawn would do the work, so his own state has to decide: "
                    + "CombatRefusal.ActorRefusal with PawnHelper.RefusalStateOf");

            Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
        }

        private static List<string> Missing(
            string[] routes,
            Func<string, bool> accepts,
            string complaint)
        {
            string root = SourceTree.RepoRoot();
            List<SourceMethod> services = SourceTree.Methods(
                SourceTree.Sources(root, "RimworldRestApi", "Services"), root);

            List<string> missing = routes
                .Select(name => new { name, method = services.FirstOrDefault(m => m.Name == name) })
                .Where(x => x.method == null || !accepts(x.method.Body))
                .Select(x => x.method == null
                    ? "no method named " + x.name
                    : x.method.Location + " (" + x.name + ")")
                .ToList();

            if (missing.Count == 0)
                return missing;

            missing.Insert(0, complaint + ":");
            return missing;
        }
    }
}
