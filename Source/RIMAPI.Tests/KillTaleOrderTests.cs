using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #4014: KilledBy records the victim first, so a deaths read that
    // always takes firstPawnData as the killer named a living colonist as dead.
    public class KillTaleOrderTests
    {
        [Fact]
        public void AKillerFirstTaleIsLeftAsItIs()
        {
            // KilledLongRange and the other kill tales: firstPawnSymbol KILLER.
            Assert.False(KillTaleOrder.NamesVictimFirst("KILLER", "VICTIM", false, true));
        }

        [Fact]
        public void KilledByNamesTheVictimFirst()
        {
            // Tale_DoublePawnKilledBy: firstPawnSymbol VICTIM, secondPawnSymbol KILLER.
            Assert.True(KillTaleOrder.NamesVictimFirst("VICTIM", "KILLER", true, false));
        }

        [Fact]
        public void TheDeclaredOrderHoldsWhenBothPawnsAreDead()
        {
            // The killer died after the kill, so the pawns alone cannot settle
            // it; the def still says the victim comes first.
            Assert.True(KillTaleOrder.NamesVictimFirst("VICTIM", "KILLER", true, true));
            Assert.False(KillTaleOrder.NamesVictimFirst("KILLER", "VICTIM", true, true));
        }

        [Fact]
        public void AColonistTaleKeepsItsKillerFirst()
        {
            // KilledColonist: firstPawnSymbol KILLER, secondPawnSymbol COLONIST.
            Assert.False(KillTaleOrder.NamesVictimFirst("KILLER", "COLONIST", false, true));
        }

        [Fact]
        public void WithoutSymbolsADeadFirstPawnIsTheVictim()
        {
            // A tale def from a mod without symbols: the dead pawn is the victim.
            Assert.True(KillTaleOrder.NamesVictimFirst(null, null, true, false));
            Assert.False(KillTaleOrder.NamesVictimFirst(null, null, false, true));
            Assert.False(KillTaleOrder.NamesVictimFirst("", "", false, false));
        }
    }
}
