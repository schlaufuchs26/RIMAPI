using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The rule behind a boarding order (fox ticket #3862): the game takes the
    // order for a pawn the shuttle allows and who can reach it, and every other
    // combination is refused with the fact that stops it rather than a bare
    // "failed".
    public class ShuttleRulesTests
    {
        private static ShuttleRules.State LodgerTheShuttleWaitsFor()
        {
            return new ShuttleRules.State
            {
                Shuttle = "Shuttle",
                ShuttleId = 91,
                Cell = "(128, 94)",
                Pawn = "Eagle",
                PawnId = 291311,
                Required = true,
                Allowed = true,
                CanReach = true,
                AcceptsColonists = false,
                PawnIsQuestLodger = true,
            };
        }

        [Fact]
        public void ARequiredPawnTheShuttleAllowsIsSent()
        {
            Assert.Null(ShuttleRules.Problem(LodgerTheShuttleWaitsFor()));
        }

        [Fact]
        public void APawnAlreadyAboardOrdersNothing()
        {
            var state = LodgerTheShuttleWaitsFor();
            state.AlreadyAboard = true;
            Assert.Null(ShuttleRules.Problem(state));
            Assert.True(ShuttleRules.NothingToDo(state));
        }

        [Fact]
        public void AShuttleThatLeftIsNamed()
        {
            var state = LodgerTheShuttleWaitsFor();
            state.ShuttleSpawned = false;
            string problem = ShuttleRules.Problem(state);
            Assert.Contains("not on the map any more", problem);
            Assert.Contains("id 91", problem);
        }

        [Fact]
        public void ADeadPawnIsRefused()
        {
            var state = LodgerTheShuttleWaitsFor();
            state.PawnDead = true;
            Assert.Contains("is dead", ShuttleRules.Problem(state));
        }

        [Fact]
        public void APawnOffTheMapSaysWhereHeIs()
        {
            var state = LodgerTheShuttleWaitsFor();
            state.PawnSpawned = false;
            state.PawnWhere = "in a caravan";
            string problem = ShuttleRules.Problem(state);
            Assert.Contains("in a caravan", problem);
            Assert.Contains("has to stand on the shuttle's map", problem);
        }

        [Fact]
        public void AShuttleThatTakesOnlyItsOwnPeopleNamesThat()
        {
            // A plain quest shuttle that names nobody: the game's check refuses
            // every colonist, and the answer says why instead of "not allowed".
            var state = LodgerTheShuttleWaitsFor();
            state.Required = false;
            state.Allowed = false;
            string problem = ShuttleRules.Problem(state);
            Assert.Contains("takes only the people its quest names", problem);
            Assert.Contains("Eagle", problem);
        }

        [Fact]
        public void AShuttleForColonistsOnlyRefusesAQuestLodger()
        {
            var state = LodgerTheShuttleWaitsFor();
            state.Required = false;
            state.Allowed = false;
            state.AcceptsColonists = true;
            state.OnlyAcceptColonists = true;
            Assert.Contains("quest lodger", ShuttleRules.Problem(state));
        }

        [Fact]
        public void AShuttleForHealthyPeopleSaysThePawnIsNot()
        {
            var state = LodgerTheShuttleWaitsFor();
            state.Required = false;
            state.Allowed = false;
            state.AcceptsColonists = true;
            state.OnlyAcceptHealthy = true;
            state.PawnUnhealthy = true;
            Assert.Contains("tended and rested", ShuttleRules.Problem(state));
        }

        [Fact]
        public void AFullShuttleNamesItsCap()
        {
            var state = LodgerTheShuttleWaitsFor();
            state.Required = false;
            state.Allowed = false;
            state.AcceptsColonists = true;
            state.MaxColonistCount = 2;
            state.AtColonistLimit = true;
            string problem = ShuttleRules.Problem(state);
            Assert.Contains("at most 2", problem);
        }

        [Fact]
        public void ADownedPawnIsNotBoarded()
        {
            // The game's own check lets a downed pawn through unless the quest
            // asks for healthy people; the walk to the shuttle is what fails, so
            // the refusal names that instead of queueing a job that ends at once.
            var state = LodgerTheShuttleWaitsFor();
            state.PawnDowned = true;
            string problem = ShuttleRules.Problem(state);
            Assert.Contains("down", problem);
            Assert.Contains("(128, 94)", problem);
        }

        [Fact]
        public void AnUnreachableShuttleIsRefused()
        {
            var state = LodgerTheShuttleWaitsFor();
            state.CanReach = false;
            Assert.Contains("no path", ShuttleRules.Problem(state));
        }
    }
}
