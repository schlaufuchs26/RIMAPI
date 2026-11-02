using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3737: the raccoon nobody could hurt. Colony 14 drafted two colonists
    // onto a mad raccoon; both jobs read back as the attack job and both took
    // hits for an hour while the animal stayed at full health. The read now
    // names the gate the game applies instead of leaving the run to guess.
    //
    // fox #4160: the squirrel a gunner stood beside. The order queued the
    // ranged job one cell from a manhunter, where a projectile weapon cannot be
    // fired at all, and the answer's "too close: 1 cell away" sat next to a map
    // that showed the animal on the neighbouring cell. The job choice and the
    // cells the check measured are stated here.
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
                HasRanged = true,
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

        [Fact]
        public void ABlowOutOfReachNeedsANeighbouringCellNotARange()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.HasRanged = false;
            facts.IsMelee = true;
            facts.CanHit = false;
            facts.Distance = 2.2f;
            facts.Range = 1.4f;

            string reason = AttackBlockRules.Reason(facts);

            Assert.Contains("out of reach", reason);
            Assert.Contains("2.2 cells", reason);
            Assert.Contains("neighbouring cell", reason);
        }

        [Fact]
        public void TheReasonNamesTheCellsItMeasured()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.CanHit = false;
            facts.Distance = 2.2f;
            facts.Range = 1.4f;
            facts.From = "120,88";
            facts.Target = "122,89";

            string reason = AttackBlockRules.Reason(facts);

            Assert.Contains("he stands at 120,88", reason);
            Assert.Contains("the target at 122,89", reason);
        }

        [Fact]
        public void AGunnerTooCloseSwingsInsteadOfStanding()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.CanHit = false;
            facts.Distance = 1f;
            facts.MinRange = 1.4f;
            facts.MeleeCanHit = true;
            facts.From = "120,88";
            facts.Target = "121,88";

            Assert.Equal(AttackBlockRules.AttackJob.Melee, AttackBlockRules.Choose(facts));
            string note = AttackBlockRules.MeleeNote(facts);
            Assert.Contains("too close", note);
            Assert.Contains("fights in contact", note);
            Assert.Contains("121,88", note);
        }

        [Fact]
        public void AGunnerInRangeKeepsTheShot()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.MeleeCanHit = true;

            Assert.Equal(AttackBlockRules.AttackJob.Ranged, AttackBlockRules.Choose(facts));
            Assert.Equal("", AttackBlockRules.MeleeNote(facts));
        }

        [Fact]
        public void APawnWithoutAGunFightsInContact()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.HasRanged = false;
            facts.IsMelee = true;
            facts.MeleeCanHit = true;

            Assert.Equal(AttackBlockRules.AttackJob.Melee, AttackBlockRules.Choose(facts));
        }

        [Fact]
        public void AGunnerWhoseGunCannotLandStillQueuesTheShot()
        {
            AttackBlockRules.Facts facts = InRange();
            facts.CanHit = false;
            facts.Distance = 30f;
            facts.MeleeCanHit = false;

            Assert.Equal(AttackBlockRules.AttackJob.Ranged, AttackBlockRules.Choose(facts));
        }
    }
}
