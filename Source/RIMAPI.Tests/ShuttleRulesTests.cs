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
        public void ADownedPawnIsCarriedInByAColonist()
        {
            // A hospitality lodger is often paralyzed (the Abasia variant of the
            // quest). The game's right-click offers "Carry to shuttle" for him,
            // so a carrier makes the call a go-ahead (ticket #3862).
            var state = LodgerTheShuttleWaitsFor();
            state.PawnDowned = true;
            state.Carrier = "McClain";
            Assert.Null(ShuttleRules.Problem(state));
        }

        [Fact]
        public void ADownedPawnWithoutACarrierNamesWhatHeNeeds()
        {
            var state = LodgerTheShuttleWaitsFor();
            state.PawnDowned = true;
            string problem = ShuttleRules.Problem(state);
            Assert.Contains("down", problem);
            Assert.Contains("(128, 94)", problem);
            Assert.Contains("can haul", problem);
        }

        [Fact]
        public void AnUnreachableShuttleIsRefused()
        {
            var state = LodgerTheShuttleWaitsFor();
            state.CanReach = false;
            Assert.Contains("no path", ShuttleRules.Problem(state));
        }

        // The rule behind a send-away call (fox ticket #4150): the game's own
        // "Send shuttle" button is enabled once everything the quest requires is
        // aboard, and a call on a shuttle that is not ready names what is still
        // missing instead of "not ready".

        private static ShuttleRules.SendState LoadedShuttle()
        {
            return new ShuttleRules.SendState
            {
                Shuttle = "Imperial shuttle",
                ShuttleId = 203032,
                Cell = "(140, 107)",
                AllRequiredLoaded = true,
                Job = "WaitSendable",
            };
        }

        [Fact]
        public void ALoadedShuttleIsSentAway()
        {
            Assert.Null(ShuttleRules.SendProblem(LoadedShuttle()));
            Assert.False(ShuttleRules.NothingToSend(LoadedShuttle()));
        }

        [Fact]
        public void AShuttleThatIsStillMissingItsPeopleNamesThem()
        {
            var state = LoadedShuttle();
            state.AllRequiredLoaded = false;
            state.Missing.Add("Neda (id 203027) is not aboard yet");
            string problem = ShuttleRules.SendProblem(state);
            Assert.Contains("Neda (id 203027)", problem);
            Assert.Contains("(140, 107)", problem);
            Assert.Contains("not ready to leave", problem);
        }

        [Fact]
        public void AShuttleMissingSomethingUnnamedStillSaysSo()
        {
            var state = LoadedShuttle();
            state.AllRequiredLoaded = false;
            string problem = ShuttleRules.SendProblem(state);
            Assert.Contains("something it requires", problem);
        }

        [Fact]
        public void AShuttleAlreadyOnItsWayOrdersNothing()
        {
            var state = LoadedShuttle();
            state.AlreadyLeaving = true;
            Assert.Null(ShuttleRules.SendProblem(state));
            Assert.True(ShuttleRules.NothingToSend(state));
        }

        [Fact]
        public void AShuttleThatAlreadyLeftIsNamed()
        {
            var state = LoadedShuttle();
            state.Spawned = false;
            string problem = ShuttleRules.SendProblem(state);
            Assert.Contains("not on the map any more", problem);
            Assert.Contains("id 203032", problem);
        }

        // The game's own CompShuttle.CanLaunch is what the send order skips
        // (ticket #4154): a shuttle with everything aboard and nobody who can
        // pilot it fails inside the game after the call reported success, so
        // the rule refuses with the game's reason instead.

        [Fact]
        public void ALoadedShuttleWithoutAPilotIsRefusedWithTheGamesReason()
        {
            var state = LoadedShuttle();
            state.CanLaunch = false;
            state.LaunchProblem = "Cannot launch: no pilot aboard.";
            string problem = ShuttleRules.SendProblem(state);
            Assert.Contains("will not launch", problem);
            Assert.Contains("id 203032", problem);
            Assert.Contains("(140, 107)", problem);
            Assert.Contains("no pilot aboard", problem);
        }

        [Fact]
        public void ALaunchRefusalWithoutAWordingStillSaysTheGameRefused()
        {
            var state = LoadedShuttle();
            state.CanLaunch = false;
            string problem = ShuttleRules.SendProblem(state);
            Assert.Contains("the game's own launch check refuses it", problem);
        }

        [Fact]
        public void AShuttleMissingItsPeopleIsRefusedBeforeThePilotCheck()
        {
            var state = LoadedShuttle();
            state.AllRequiredLoaded = false;
            state.CanLaunch = false;
            state.LaunchProblem = "Cannot launch: no pilot aboard.";
            string problem = ShuttleRules.SendProblem(state);
            Assert.Contains("not ready to leave", problem);
            Assert.DoesNotContain("no pilot", problem);
        }
    }
}
