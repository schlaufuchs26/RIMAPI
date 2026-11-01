using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3737: the raccoon nobody could hurt. Colony 14 drafted two colonists
    // onto a mad raccoon; both jobs read back as the attack job and both took
    // hits for an hour while the animal stayed at full health. The read now
    // names the gate the game applies instead of leaving the run to guess.
    public class AttackBlockRulesTests
    {
        private static AttackBlockRules.Facts InRange()
        {
            return new AttackBlockRules.Facts
            {
                CanFight = true,
                HasVerb = true,
                CanHit = true,
                Distance = 8f,
                Range = 26f,
            };
        }

        [Fact]
        public void AnAttackTheGameAcceptsNamesNoReason()
        {
            Assert.Equal("", AttackBlockRules.Reason(InRange()));
        }

        [Fact]
        public void APawnTheGameDisablesViolenceForIsNamedFirst()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.CanFight = false;
            facts.CanHit = false;
            facts.Distance = 40f;

            string reason = AttackBlockRules.Reason(facts);

            Assert.Contains("disables violence", reason);
        }

        [Fact]
        public void APawnWithoutAVerbIsNoFighter()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.HasVerb = false;
            facts.CanHit = false;

            Assert.Contains("no attack verb", AttackBlockRules.Reason(facts));
        }

        [Fact]
        public void ATargetBeyondTheRangeIsOutOfRange()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.CanHit = false;
            facts.Distance = 30f;
            facts.Range = 26f;

            string reason = AttackBlockRules.Reason(facts);

            Assert.Contains("out of range", reason);
            Assert.Contains("30 cells", reason);
            Assert.Contains("26 cells", reason);
        }

        [Fact]
        public void ATargetInsideTheMinimumRangeIsTooClose()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.CanHit = false;
            facts.Distance = 2f;
            facts.MinRange = 4f;

            string reason = AttackBlockRules.Reason(facts);

            Assert.Contains("too close", reason);
            Assert.Contains("minimum is 4 cells", reason);
        }

        [Fact]
        public void ARefusedHitInsideTheRangeIsALineOfSightProblem()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.CanHit = false;
            facts.Distance = 14f;

            Assert.Equal("no line of sight from where he stands", AttackBlockRules.Reason(facts));
        }
    }
}
