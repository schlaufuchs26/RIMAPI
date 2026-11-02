using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #4175: the gunner that charged. Since #4163 an attack on a target
    // beyond the weapon's reach queues the melee job, which is what the game's
    // own right-click does; a revolver pawn ordered onto a raider 120 cells away
    // walked into contact instead of firing. `shoot` walks to a cell that has
    // the shot and fires from there, and the sentences it answers with are
    // stated here.
    public class ShootRulesTests
    {
        private static ShootRules.Plan Found()
        {
            return new ShootRules.Plan
            {
                Found = true,
                CellsWithShot = 5,
                ReachableCells = 3,
                Walk = 40,
                Distance = 12f,
                Range = 25.9f,
                Cell = "141,102",
                From = "120,88",
                Target = "145,3",
            };
        }

        [Fact]
        public void AShotFromWhereThePawnStandsNeedsNoWalk()
        {
            ShootRules.Plan plan = Found();
            plan.Found = false;
            plan.InRange = true;
            plan.Distance = 8f;

            string note = ShootRules.PlanNote(plan);

            Assert.Contains("already has the shot from 120,88", note);
            Assert.Contains("8 cells from the target", note);
            Assert.Contains("the weapon reaches 25.9 cells", note);
            Assert.Contains("stands and fires instead of closing in", note);
        }

        [Fact]
        public void AWalkedShotNamesTheCellAndTheShotBehindIt()
        {
            string note = ShootRules.PlanNote(Found());

            Assert.Contains("walks to 141,102", note);
            Assert.Contains("12 cells from the target", note);
            Assert.Contains("40 cells of path", note);
            Assert.Contains("the shot is queued behind the walk", note);
        }

        [Fact]
        public void AWalkOfUnknownLengthNamesNoLength()
        {
            ShootRules.Plan plan = Found();
            plan.Walk = 0;

            Assert.DoesNotContain("cells of path", ShootRules.PlanNote(plan));
        }

        [Fact]
        public void APawnWithoutARangedWeaponIsToldToAttack()
        {
            string reason = ShootRules.NoRanged("Sam");

            Assert.Contains("Sam has no ranged weapon", reason);
            Assert.Contains("`attack` closes in and swings", reason);
        }

        [Fact]
        public void ACellNothingHasALineToIsNamedAsSuch()
        {
            ShootRules.Plan plan = Found();
            plan.Found = false;
            plan.CellsWithShot = 0;

            string reason = ShootRules.NoCellReason(plan);

            Assert.Contains("no cell within 25.9 cells of the target at 145,3 has a line to it", reason);
            Assert.Contains("`attack` closes in and swings", reason);
        }

        [Fact]
        public void CellsWithAShotThePawnCannotReachAreSeparatedFromNoLineAtAll()
        {
            ShootRules.Plan plan = Found();
            plan.Found = false;
            plan.CellsWithShot = 3;
            plan.ReachableCells = 0;

            string reason = ShootRules.NoCellReason(plan);

            Assert.Contains("none of the 3 cells with a shot", reason);
            Assert.Contains("reachable", reason);
            Assert.Contains("from 120,88", reason);
        }

        [Fact]
        public void ACappedSearchNamesTheCapInsteadOfACountItNeverMade()
        {
            ShootRules.Plan plan = Found();
            plan.Found = false;
            plan.CellsWithShot = 400;

            string reason = ShootRules.NoCellReason(plan);

            Assert.Contains($"nearest {ShootRules.ReachChecks} cells with a shot", reason);
        }
    }
}
