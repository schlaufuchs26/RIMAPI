using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// A `prioritize` call that names a cell means the things standing on that
    /// cell, and a blueprint or frame there is what the caller wants built
    /// (fox ticket #3708): a wall frame's cell offered BuildRoof first, so the
    /// build job never came up. The ordering rule is what decides, and no
    /// RimWorld type appears in it, so it is tested here rather than in the
    /// game.
    /// </summary>
    public class PriorityTargetRulesTests
    {
        [Fact]
        public void A_blueprint_at_the_cell_comes_before_the_other_things()
        {
            List<string> things = new List<string> { "Plant", "Blueprint", "Filth" };
            List<string> ordered = PriorityTargetRules.ConstructiblesFirst(
                things, t => t == "Blueprint");

            Assert.Equal(new[] { "Blueprint", "Plant", "Filth" }, ordered);
        }

        // A cell can hold two constructibles, a frame and the blueprint of the
        // next wall; each keeps its place relative to the other.
        [Fact]
        public void The_constructibles_keep_their_own_order()
        {
            List<string> things = new List<string> { "Filth", "Frame", "Blueprint", "Plant" };
            List<string> ordered = PriorityTargetRules.ConstructiblesFirst(
                things, t => t == "Frame" || t == "Blueprint");

            Assert.Equal(new[] { "Frame", "Blueprint", "Filth", "Plant" }, ordered);
        }

        // Nothing to build there: the list reads as it came, so the cell's own
        // jobs are tried in the game's order and the answer stays what it was.
        [Fact]
        public void A_cell_without_a_blueprint_keeps_the_games_order()
        {
            List<string> things = new List<string> { "Plant", "Filth" };
            Assert.Equal(things, PriorityTargetRules.ConstructiblesFirst(things, t => false));
            Assert.Empty(PriorityTargetRules.ConstructiblesFirst<string>(null, t => true));
        }

        // The build jobs are the three the game files under Construct; the roof
        // above the cell and a deconstruct of the blueprint are not builds.
        [Fact]
        public void Only_the_construct_jobs_build_a_blueprint()
        {
            Assert.True(PriorityTargetRules.IsConstructJob("ConstructFinishFrames"));
            Assert.True(PriorityTargetRules.IsConstructJob("ConstructDeliverResourcesToBlueprints"));
            Assert.False(PriorityTargetRules.IsConstructJob("BuildRoof"));
            Assert.False(PriorityTargetRules.IsConstructJob("DeconstructForBlueprint"));
            Assert.False(PriorityTargetRules.IsConstructJob(""));
        }
    }
}
