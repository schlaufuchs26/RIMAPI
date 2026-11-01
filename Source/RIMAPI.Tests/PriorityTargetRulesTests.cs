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

        // Two jobs build a planned build: FinishFrame for a plan that needs
        // material, PlaceNoCostFrame for one that costs nothing (a sleeping
        // spot). The WorkGiverDef names are not JobDefs, so a prefix test on
        // "Construct" recognised no build at all and skipped the job it meant
        // to protect (fox #3706, the no-cost plan from #3708).
        [Fact]
        public void Only_the_build_jobs_build_a_blueprint()
        {
            Assert.True(PriorityTargetRules.IsConstructJob("FinishFrame"));
            Assert.True(PriorityTargetRules.IsConstructJob("finishframe"));
            Assert.True(PriorityTargetRules.IsConstructJob("PlaceNoCostFrame"));
            Assert.False(PriorityTargetRules.IsConstructJob("ConstructFinishFrames"));
            Assert.False(PriorityTargetRules.IsConstructJob("BuildRoof"));
            Assert.False(PriorityTargetRules.IsConstructJob("DeconstructForBlueprint"));
            Assert.False(PriorityTargetRules.IsConstructJob(""));
        }

        // A deconstruct named on the colony's own blueprint or frame is the one
        // order that destroys the plan, in the menu's word or as the defName the
        // game prints, so the call refuses it instead of queueing it (#3706).
        [Fact]
        public void The_removal_words_name_a_deconstruct()
        {
            Assert.True(PriorityTargetRules.NamesRemoval("deconstruct"));
            Assert.True(PriorityTargetRules.NamesRemoval("DeconstructForBlueprint"));
            Assert.True(PriorityTargetRules.NamesRemoval("uninstall"));
            Assert.False(PriorityTargetRules.NamesRemoval("build"));
            Assert.False(PriorityTargetRules.NamesRemoval("FinishFrame"));
            Assert.False(PriorityTargetRules.NamesRemoval(""));
            Assert.False(PriorityTargetRules.NamesRemoval(null));
        }

        // A blueprint that waits for its material is not built first: the game
        // hauls the material to it, so the delivery is the build's first step
        // and the guard has to let it through (fox #3777). HaulToCell is the
        // same delivery to a plan standing on a cell; a plain haul is not.
        [Fact]
        public void The_delivery_to_a_planned_build_is_a_build_step()
        {
            Assert.True(PriorityTargetRules.IsBuildStepJob("HaulToContainer", "ConstructDeliverResourcesToBlueprints"));
            Assert.True(PriorityTargetRules.IsBuildStepJob("HaulToCell", "DeliverResourcesToFrames"));
            Assert.True(PriorityTargetRules.IsBuildStepJob("FinishFrame", "ConstructFinishFrames"));
            Assert.False(PriorityTargetRules.IsBuildStepJob("HaulToContainer", "HaulGeneral"));
            Assert.False(PriorityTargetRules.IsBuildStepJob("HaulGeneral", "ConstructDeliverResourcesToBlueprints"));
            Assert.False(PriorityTargetRules.IsBuildStepJob("BuildRoof", "ConstructDeliverResourcesToBlueprints"));
            Assert.False(PriorityTargetRules.IsBuildStepJob("", ""));
            Assert.False(PriorityTargetRules.IsBuildStepJob(null, null));
        }

        // The four givers that carry material to a plan are named after the job
        // they do, so the name is what tells a delivery apart from a haul that
        // happens to end in a container on the same cell (fox #3777).
        [Fact]
        public void The_deliver_givers_are_recognised_by_their_defname()
        {
            Assert.True(PriorityTargetRules.IsDeliverResourcesGiver("ConstructDeliverResourcesToBlueprints"));
            Assert.True(PriorityTargetRules.IsDeliverResourcesGiver("DeliverResourcesToFrames"));
            Assert.False(PriorityTargetRules.IsDeliverResourcesGiver("HaulGeneral"));
            Assert.False(PriorityTargetRules.IsDeliverResourcesGiver("ConstructFinishFrames"));
            Assert.False(PriorityTargetRules.IsDeliverResourcesGiver(""));
        }

        // The way-clearing deconstruct is the game's own initiative job; an
        // unnamed order must never take it, because its giver answers for any
        // building once the call is forced (fox #3706).
        [Fact]
        public void The_way_clearing_job_is_recognised_by_its_defname()
        {
            Assert.True(PriorityTargetRules.IsWayClearingJob("DeconstructForBlueprint"));
            Assert.True(PriorityTargetRules.IsWayClearingJob("deconstructforblueprint"));
            Assert.False(PriorityTargetRules.IsWayClearingJob("Deconstruct"));
            Assert.False(PriorityTargetRules.IsWayClearingJob("FinishFrame"));
            Assert.False(PriorityTargetRules.IsWayClearingJob(""));
        }
    }
}
