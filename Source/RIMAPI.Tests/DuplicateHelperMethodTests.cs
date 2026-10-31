using System;
using System.Collections.Generic;
using System.Linq;
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
        [Fact]
        public void NoServiceRepeatsAHelperMethodBody()
        {
            string root = SourceTree.RepoRoot();
            List<SourceMethod> helpers = Methods(root, "RimworldRestApi", "Helpers");
            List<SourceMethod> services = Methods(root, "RimworldRestApi", "Services");

            List<string> problems = helpers
                .SelectMany(helper =>
                    services
                        .Where(service => service.Name == helper.Name && Same(service, helper))
                        .Select(service => $"{service.Location} repeats {helper.Location} ({helper.Name})"))
                .ToList();

            Assert.True(
                problems.Count == 0,
                "A service method that copies a helper's body drifts from it; forward to the helper instead:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, problems));
        }

        /// <summary>
        /// Two services that carry the same method are the same drift risk one
        /// step earlier (ticket #3446): QuestService and LetterService each had
        /// their own Quote, and an escape fix would have landed in one copy.
        /// One of the two moves to a helper and both forward to it, which the
        /// test above then keeps in place.
        /// </summary>
        [Fact]
        public void NoTwoServicesShareAMethodBody()
        {
            string root = SourceTree.RepoRoot();
            List<SourceMethod> services = Methods(root, "RimworldRestApi", "Services");

            List<string> problems = services
                .SelectMany((service, index) =>
                    services
                        .Skip(index + 1)
                        .Where(other => other.Name == service.Name && Same(other, service))
                        .Select(other => $"{other.Location} repeats {service.Location} ({service.Name})"))
                .ToList();

            Assert.True(
                problems.Count == 0,
                "Two services carry the same method, so an edit lands in one copy only; move it into a helper:"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine, problems));
        }

        private static List<SourceMethod> Methods(string root, params string[] subdirs) =>
            SourceTree.Methods(SourceTree.Sources(root, subdirs), root);

        private static bool Same(SourceMethod a, SourceMethod b) =>
            SourceTree.Squash(a.Body) == SourceTree.Squash(b.Body);
    }
}
