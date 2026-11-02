using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The wording of settling, abandoning and trading (fox ticket #4226, wave 2).
    /// The game's own checks for these answer mostly yes or no, so the route has to
    /// name the fact that stopped it; these tests keep that wording honest and keep
    /// the two limit sentences the Settle command owns apart.
    /// </summary>
    public class SettlementRulesTests
    {
        [Fact]
        public void TheSettleLimitHasTheCommandsTwoSentences()
        {
            // With one settlement allowed the command says a base exists, with
            // several it says the maximum is reached; a caller should read the one
            // that applies, so both keys are the game's.
            Assert.Equal("CommandSettleFailAlreadyHaveBase", SettlementRules.SettleLimitKey(false));
            Assert.Equal(
                "CommandSettleFailReachedMaximumNumberOfBases",
                SettlementRules.SettleLimitKey(true)
            );
        }

        [Fact]
        public void TheAbandonRefusalsAreTheGamesOwnKeys()
        {
            Assert.Equal(
                "CommandAbandonHomeFailAllColonistsThere",
                SettlementRules.AbandonAllColonistsKey
            );
            Assert.Equal(
                "AbandonArchotechStructuresWarningDialog",
                SettlementRules.ArchotechWarningKey
            );
        }

        [Fact]
        public void AnUnknownSettlementNamesTheReadThatPrintsTheIds()
        {
            string refusal = SettlementRules.UnknownSettlement(42);
            Assert.Contains("42", refusal);
            Assert.Contains("/api/v1/world/settlements", refusal);
        }

        [Fact]
        public void AForeignSettlementNamesItsFaction()
        {
            string refusal = SettlementRules.NotPlayerSettlement("Rustburg", "Outlander");
            Assert.Contains("Rustburg", refusal);
            Assert.Contains("Outlander", refusal);
            Assert.Contains("/api/v1/world/player/settlements", refusal);
        }

        [Theory]
        [InlineData(false, true, "not on the world map")]
        [InlineData(true, false, "Visit")]
        public void AVisitRefusalNamesTheFactThatStoppedIt(bool spawned, bool visitable, string expected)
        {
            string problem = SettlementRules.VisitProblem(spawned, visitable);
            Assert.NotNull(problem);
            Assert.Contains(expected, problem);
        }

        [Fact]
        public void AVisitTheGameOffersHasNoProblem()
        {
            Assert.Null(SettlementRules.VisitProblem(spawned: true, visitable: true));
        }

        [Fact]
        public void ATradeTheGameOffersHasNoProblem()
        {
            Assert.Null(
                SettlementRules.TradeProblem(
                    spawned: true,
                    hasMap: false,
                    playerFaction: false,
                    permanentEnemy: false,
                    hostile: false,
                    canTradeNow: true,
                    hasNegotiator: true
                )
            );
        }

        [Theory]
        // The order is the one CaravanArrivalAction_Trade.CanTradeWith checks, so
        // the first fact that stops the trade is the one a caller reads.
        [InlineData(false, false, false, false, false, true, true, "not on the world map")]
        [InlineData(true, false, true, false, false, true, true, "player's own settlement")]
        [InlineData(true, true, false, false, false, true, true, "its map is open")]
        [InlineData(true, false, false, true, false, true, true, "permanent enemy")]
        [InlineData(true, false, false, false, true, true, true, "hostile")]
        [InlineData(true, false, false, false, false, false, true, "nothing to trade")]
        [InlineData(true, false, false, false, false, true, false, "negotiate")]
        public void ATradeRefusalNamesTheFirstFactThatStopsIt(
            bool spawned,
            bool hasMap,
            bool playerFaction,
            bool permanentEnemy,
            bool hostile,
            bool canTradeNow,
            bool hasNegotiator,
            string expected
        )
        {
            string problem = SettlementRules.TradeProblem(
                spawned,
                hasMap,
                playerFaction,
                permanentEnemy,
                hostile,
                canTradeNow,
                hasNegotiator
            );
            Assert.NotNull(problem);
            Assert.Contains(expected, problem);
        }

        [Fact]
        public void ASilentRefusalSaysThatTheGameGaveNoReason()
        {
            string refusal = SettlementRules.NoReasonGiven("Rustburg", "trade");
            Assert.Contains("Rustburg", refusal);
            Assert.Contains("trade", refusal);
            Assert.Contains("no reason", refusal);
        }
    }
}
