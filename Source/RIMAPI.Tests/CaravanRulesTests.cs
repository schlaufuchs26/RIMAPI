using System.IO;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The rules of forming a caravan (fox ticket #4212, wave 2). The route hands
    /// the caravan to the game's own form-and-send job, so what is left to test
    /// without a game is the arithmetic and the wording: the mass comparison the
    /// dialog refuses on, the game's own refusal keys, and the fact that stopped
    /// a named pawn.
    /// </summary>
    public class CaravanRulesTests
    {
        [Fact]
        public void AMassOverCapacityIsRefusedAndAnExactFitGoes()
        {
            // The dialog refuses only when MassUsage > MassCapacity, so an exact
            // fit is still a caravan the game sends.
            Assert.True(CaravanRules.Fits(40f, 40f));
            Assert.True(CaravanRules.Fits(39.9f, 40f));
            Assert.False(CaravanRules.Fits(40.1f, 40f));
        }

        [Fact]
        public void TheRefusalKeysAreTheGamesOwn()
        {
            // A caller should read the sentence the dialog shows, so the keys are
            // the game's translation keys and not wording of our own.
            Assert.Equal("CaravanMustHaveAtLeastOneColonist", CaravanRules.ColonistRequiredKey(ideologyActive: false));
            Assert.Equal("CaravanMustHaveAtLeastOneNonSlaveColonist", CaravanRules.ColonistRequiredKey(ideologyActive: true));
            Assert.Equal("TooBigCaravanMassUsage", CaravanRules.OverloadedKey);
            Assert.Equal("MessageNoValidExitTile", CaravanRules.NoValidExitTileKey);
            Assert.Equal("CaravanItemIsUnreachableSingle", CaravanRules.UnreachableItemKey(1));
            Assert.Equal("CaravanItemIsUnreachableMulti", CaravanRules.UnreachableItemKey(12));
        }

        [Theory]
        [InlineData(true, false, false, false, false, true, "he is dead")]
        [InlineData(false, true, false, false, false, true, "he is down")]
        [InlineData(false, false, true, false, false, true, "he is a prisoner")]
        [InlineData(false, false, false, true, false, true, "he is in a mental state")]
        [InlineData(false, false, false, false, true, true, "he is already forming a caravan")]
        [InlineData(false, false, false, false, false, false, "he is not on this map")]
        public void APawnProblemNamesTheFactThatStopsHim(bool dead, bool downed, bool prisoner,
            bool mentalState, bool forming, bool onMap, string expected)
        {
            Assert.Equal(
                expected,
                CaravanRules.PawnProblem(dead, downed, prisoner, mentalState, forming, onMap)
            );
        }

        [Fact]
        public void ASendablePawnTheListDoesNotExplainNamesTheList()
        {
            // A slave or a lodger the game does not offer has no visible cause;
            // the refusal says so instead of inventing one.
            string why = CaravanRules.PawnProblem(false, false, false, false, false, true);
            Assert.Contains("does not list him as sendable", why);
        }

        [Fact]
        public void TheNotSendableSentenceNamesThePawnAndTheFact()
        {
            string text = CaravanRules.NotSendable("Vulpes", "he is down");
            Assert.Contains("Vulpes", text);
            Assert.Contains("he is down", text);
        }

        [Fact]
        public void TheRouteRunsTheGatherJobsAndNotTheInstantExit()
        {
            // Two levers exist: StartFormingCaravan runs the dialog's own gather
            // and walk-out, ExitMapAndCreateCaravan teleports the pawns off the
            // map at once. A parity route takes the first; the second is the
            // shortcut a dev tool would use.
            string root = SourceTree.RepoRoot();
            string service = Path.Combine(
                root,
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Services",
                "World",
                "CaravanService",
                "CaravanService.cs"
            );

            string text = File.ReadAllText(service);
            Assert.Contains("StartFormingCaravan", text);
            Assert.DoesNotContain("ExitMapAndCreateCaravan", text);
            Assert.DoesNotContain("FormAndCreateCaravan", text);
        }

        // ---- Steering a formed caravan (fox ticket #4226) ----

        [Fact]
        public void TheThreeSteerActionsAreTravelVisitAndTrade()
        {
            // The order is the one the answer and the refusal print them in;
            // travel is first because it is the default the route takes.
            Assert.Equal(new[] { "travel", "visit", "trade" }, CaravanRules.Actions);
        }

        [Theory]
        [InlineData(null, "travel")]
        [InlineData("", "travel")]
        [InlineData("   ", "travel")]
        [InlineData("travel", "travel")]
        [InlineData("Visit", "visit")]
        [InlineData(" TRADE ", "trade")]
        public void AnActionNameIsReadCaseAndSpaceInsensitive(string raw, string expected)
        {
            Assert.True(CaravanRules.TryParseAction(raw, out string action));
            Assert.Equal(expected, action);
        }

        [Fact]
        public void AnActionTheRouteDoesNotTakeIsRefusedWithTheOnesItDoes()
        {
            // Attack and gifts are arrival actions too, but they are not this
            // route's; the refusal names the three it takes instead of guessing.
            Assert.False(CaravanRules.TryParseAction("attack", out _));
            string refusal = CaravanRules.UnknownAction("attack");
            Assert.Contains("attack", refusal);
            Assert.Contains("travel", refusal);
            Assert.Contains("visit", refusal);
            Assert.Contains("trade", refusal);
        }

        [Fact]
        public void ASteerRefusalNamesWhatIsWrong()
        {
            Assert.Contains("114", CaravanRules.UnknownCaravan(114));
            Assert.Contains("not the player's caravan", CaravanRules.NotOurs("Rosa's caravan"));
            Assert.Contains("999999", CaravanRules.BadTile(999999));
            Assert.Contains("already stands on tile 42", CaravanRules.AlreadyThere(42));
            Assert.Contains("no passable route", CaravanRules.NoPath(42));
        }

        [Fact]
        public void TheSteerRouteRunsTheWorldMapsOwnOrderRatherThanAPathOfItsOwn()
        {
            // The lever is the one a right-click pulls (WorldSelector's
            // AutoOrderToTileNow) plus the game's arrival actions; a route that
            // teleports or walks the caravan itself would be a god action.
            string root = SourceTree.RepoRoot();
            string service = Path.Combine(
                root,
                "Source",
                "RIMAPI",
                "RimworldRestApi",
                "Services",
                "World",
                "CaravanService",
                "CaravanService.cs"
            );

            string text = File.ReadAllText(service);
            Assert.Contains("BestGotoDestNear", text);
            Assert.Contains("caravan.pather.StartPath", text);
            Assert.Contains("CaravanArrivalAction_Trade", text);
            Assert.Contains("CaravanArrivalAction_VisitSettlement", text);
            // No teleport: the caravan walks, the game moves it.
            Assert.DoesNotContain("Teleport", text);
        }
    }
}
