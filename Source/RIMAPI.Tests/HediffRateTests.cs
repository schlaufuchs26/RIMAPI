using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The daily severity rate a hediff def states (fox, #3449). The digest
    /// projects a death from this number, so a def with no rate has to come
    /// back as 0 rather than as somebody's guess.
    /// </summary>
    public class HediffRateTests
    {
        [Fact]
        public void AnImmunizableInfectionUsesItsNotImmuneRate()
        {
            // WoundInfection: severityPerDayNotImmune 0.84.
            Assert.Equal(0.84f, HediffRate.Pick(0.84f, 0f));
        }

        [Fact]
        public void TheNotImmuneRateWinsOverAPlainRate()
        {
            // A def carrying both comps climbs on the immunizable one; that is
            // the clock until immunity overtakes it.
            Assert.Equal(0.2488f, HediffRate.Pick(0.2488f, 1f));
        }

        [Fact]
        public void ADefWithoutAnImmunizableRateFallsBackToThePlainRate()
        {
            // A SeverityPerDay hediff has no immunity race at all.
            Assert.Equal(0.5f, HediffRate.Pick(0f, 0.5f));
        }

        [Fact]
        public void ADefWithNoRateAtAllReadsAsZero()
        {
            // Gut worms are an infection by the game's flag but state no rate;
            // 0 is what tells the caller not to project a death.
            Assert.Equal(0f, HediffRate.Pick(0f, 0f));
        }

        [Fact]
        public void AnImmunizableInfectionStatesItsImmunityClock()
        {
            // WoundInfection: immunityPerDaySick 0.6441, the second clock of
            // the race against severity (fox, #3682).
            Assert.Equal(0.6441f, HediffRate.PickImmunity(0.6441f));
        }

        [Fact]
        public void ANonPositiveImmunityRateIsNoClock()
        {
            // A def without an Immunizable comp, or one whose immunity only
            // shrinks, gives no second clock: the digest must not invent one.
            Assert.Equal(0f, HediffRate.PickImmunity(0f));
            Assert.Equal(0f, HediffRate.PickImmunity(-0.4f));
        }

        [Fact]
        public void ANegativeRateIsNoRate()
        {
            // A def that shrinks by itself says nothing about how fast it
            // climbs, and a negative figure would project a death in the past.
            Assert.Equal(0.4f, HediffRate.Pick(-0.53f, 0.4f));
        }
    }
}
