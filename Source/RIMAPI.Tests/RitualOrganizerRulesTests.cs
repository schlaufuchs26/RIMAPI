using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3675: `rituals` named the campfire for the leader's speech, and the
    // same start without pawn_id answered "no target", because the read falls
    // back to the first colonist as the organizer and the start did not. These
    // state the order both sides have to take.
    public class RitualOrganizerRulesTests
    {
        private static readonly object Named = new object();
        private static readonly object RoleHolder = new object();
        private static readonly object FirstColonist = new object();

        [Fact]
        public void ANamedPawnStaysTheOrganizer()
        {
            Assert.Same(Named, RitualOrganizerRules.Pick(Named, RoleHolder, FirstColonist));
        }

        // The leader's speech is carried by the pawn who fills its role, so the
        // role holder beats the first colonist.
        [Fact]
        public void WithoutANameTheRoleHolderTakesIt()
        {
            Assert.Same(RoleHolder, RitualOrganizerRules.Pick(null, RoleHolder, FirstColonist));
        }

        // The fallback the read makes: a ritual whose role is empty still gets
        // an organizer, which is what BestTarget needs to name a place.
        [Fact]
        public void WithoutANameOrARoleHolderTheFirstColonistTakesIt()
        {
            Assert.Same(FirstColonist, RitualOrganizerRules.Pick(null, null, FirstColonist));
        }

        [Fact]
        public void AnEmptyColonyHasNoOrganizer()
        {
            Assert.Null(RitualOrganizerRules.Pick<object>(null, null, null));
        }
    }
}
