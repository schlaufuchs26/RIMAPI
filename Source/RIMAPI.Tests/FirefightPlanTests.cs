using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// Pairing colonists with burning cells (fox ticket #3444). The order names
    /// cells, so the plan is plain arithmetic and the rule is stated here
    /// instead of only inside the running game.
    /// </summary>
    public class FirefightPlanTests
    {
        private static FirefightPlan.BurningCell Fire(int x, int z)
        {
            return new FirefightPlan.BurningCell { X = x, Z = z };
        }

        private static FirefightPlan.FreePawn Pawn(int id, int x, int z, bool firefighter = true)
        {
            return new FirefightPlan.FreePawn
            {
                Id = id,
                Name = "Pawn" + id,
                X = x,
                Z = z,
                FirefighterActive = firefighter,
            };
        }

        [Fact]
        public void One_colonist_goes_to_the_one_named_cell()
        {
            List<FirefightPlan.Assignment> plan = FirefightPlan.Assign(
                new List<FirefightPlan.BurningCell> { Fire(203, 141) },
                new List<FirefightPlan.FreePawn> { Pawn(12, 190, 140) },
                0);

            Assert.Single(plan);
            Assert.Equal(203, plan[0].FireX);
            Assert.Equal(141, plan[0].FireZ);
            Assert.Equal(12, plan[0].PawnId);
            Assert.Equal("Pawn12", plan[0].PawnName);
        }

        [Fact]
        public void The_nearest_cell_wins()
        {
            List<FirefightPlan.Assignment> plan = FirefightPlan.Assign(
                new List<FirefightPlan.BurningCell> { Fire(40, 40), Fire(12, 10) },
                new List<FirefightPlan.FreePawn> { Pawn(1, 10, 10) },
                0);

            Assert.Single(plan);
            Assert.Equal(12, plan[0].FireX);
            Assert.Equal(10, plan[0].FireZ);
        }

        [Fact]
        public void Two_colonists_never_share_a_cell()
        {
            List<FirefightPlan.Assignment> plan = FirefightPlan.Assign(
                new List<FirefightPlan.BurningCell> { Fire(10, 10), Fire(30, 30) },
                new List<FirefightPlan.FreePawn> { Pawn(1, 10, 11), Pawn(2, 30, 29) },
                0);

            Assert.Equal(2, plan.Count);
            Assert.Equal(10, plan[0].FireX);
            Assert.Equal(30, plan[1].FireX);
        }

        [Fact]
        public void More_cells_than_colonists_leave_the_rest_unassigned()
        {
            List<FirefightPlan.Assignment> plan = FirefightPlan.Assign(
                new List<FirefightPlan.BurningCell> { Fire(10, 10), Fire(20, 20), Fire(30, 30) },
                new List<FirefightPlan.FreePawn> { Pawn(1, 10, 10) },
                0);

            Assert.Single(plan);
        }

        [Fact]
        public void A_limit_caps_the_jobs_one_call_queues()
        {
            List<FirefightPlan.Assignment> plan = FirefightPlan.Assign(
                new List<FirefightPlan.BurningCell> { Fire(10, 10), Fire(20, 20), Fire(30, 30) },
                new List<FirefightPlan.FreePawn> { Pawn(1, 10, 10), Pawn(2, 20, 20), Pawn(3, 30, 30) },
                2);

            Assert.Equal(2, plan.Count);
        }

        [Fact]
        public void A_colonist_whose_work_is_firefighting_picks_before_a_nearer_other()
        {
            // The nearer colonist has Firefighter off; the game would send the
            // one whose work it is, so the plan does too.
            List<FirefightPlan.Assignment> plan = FirefightPlan.Assign(
                new List<FirefightPlan.BurningCell> { Fire(10, 10) },
                new List<FirefightPlan.FreePawn> { Pawn(1, 10, 11, false), Pawn(2, 40, 40, true) },
                0);

            Assert.Single(plan);
            Assert.Equal(2, plan[0].PawnId);
        }

        [Fact]
        public void No_fire_or_no_colonist_queues_nothing()
        {
            Assert.Empty(FirefightPlan.Assign(
                new List<FirefightPlan.BurningCell>(),
                new List<FirefightPlan.FreePawn> { Pawn(1, 10, 10) },
                0));
            Assert.Empty(FirefightPlan.Assign(
                new List<FirefightPlan.BurningCell> { Fire(10, 10) },
                new List<FirefightPlan.FreePawn>(),
                0));
            Assert.Empty(FirefightPlan.Assign(null, null, 0));
        }
    }
}
