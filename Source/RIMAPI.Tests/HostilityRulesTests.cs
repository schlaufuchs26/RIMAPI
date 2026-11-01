using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3630: the wild man. Colony 14's DiLeo (id 54348) refused an arrest and
    // then beat Veniamin from 100% to 40% health with fifteen fist wounds. The
    // fight read answered "0 hostiles" and the wait ran its full hour, because
    // the rule only asked for a hostile faction or a manhunter animal. What
    // makes a pawn a threat is now also its own job: an attack on the colony.
    public class HostilityRulesTests
    {
        private static HostilityRules.PawnState Raider()
        {
            return new HostilityRules.PawnState { EnemyFaction = true };
        }

        [Fact]
        public void AnEnemyFactionIsAThreat()
        {
            Assert.True(HostilityRules.ThreatensColony(Raider()));
        }

        [Fact]
        public void AManhunterAnimalIsAThreat()
        {
            var warg = new HostilityRules.PawnState { Animal = true, MentalState = "Manhunter" };
            Assert.True(HostilityRules.ThreatensColony(warg));
        }

        [Fact]
        public void AWildManBeatingAColonistIsAThreat()
        {
            // The measured case: factionless, no lord, job AttackMelee on a colonist.
            var wildMan = new HostilityRules.PawnState { JobDef = "AttackMelee", JobTargetsColony = true };
            Assert.True(HostilityRules.ThreatensColony(wildMan));
        }

        [Fact]
        public void AShooterOfNoFactionAimingAtAColonistIsAThreat()
        {
            var sniper = new HostilityRules.PawnState { JobDef = "AttackStatic", JobTargetsColony = true };
            Assert.True(HostilityRules.ThreatensColony(sniper));
        }

        [Fact]
        public void AWildManGoingAboutHisDayIsNoThreat()
        {
            var wildMan = new HostilityRules.PawnState { JobDef = "GotoWander" };
            Assert.False(HostilityRules.ThreatensColony(wildMan));
        }

        [Fact]
        public void AnAttackOnSomethingThatIsNotTheColonyIsNoThreat()
        {
            // A factionless scavenger brawling with a raider is not the colony's
            // fight, and counting him would put a bystander in the target list.
            var brawler = new HostilityRules.PawnState { JobDef = "AttackMelee", JobTargetsColony = false };
            Assert.False(HostilityRules.ThreatensColony(brawler));
        }

        [Fact]
        public void ABerserkGuestIsAThreat()
        {
            var guest = new HostilityRules.PawnState { MentalState = "Berserk" };
            Assert.True(HostilityRules.ThreatensColony(guest));
        }

        [Fact]
        public void AFactionlessPawnInAnAssaultIsAThreat()
        {
            var attacker = new HostilityRules.PawnState { LordJob = "LordJob_AssaultColony" };
            Assert.True(HostilityRules.ThreatensColony(attacker));
        }

        [Fact]
        public void AHiveThatDefendsItsCaveIsNoThreat()
        {
            var insect = new HostilityRules.PawnState { Animal = true, LordJob = "LordJob_DefendAndExpandHive" };
            Assert.False(HostilityRules.ThreatensColony(insect));
        }

        [Fact]
        public void ABerserkColonistStaysInTheColonistList()
        {
            // The colonists list owns him and the digest names his mental state;
            // counting him as a hostile too would report a threat nobody lists.
            var berserk = new HostilityRules.PawnState { Colonist = true, MentalState = "Berserk" };
            Assert.False(HostilityRules.ThreatensColony(berserk));
        }

        [Fact]
        public void AColonistIsNeverAHostile()
        {
            var colonist = new HostilityRules.PawnState { Colonist = true, JobDef = "AttackMelee", JobTargetsColony = true };
            Assert.False(HostilityRules.ThreatensColony(colonist));
        }

        [Fact]
        public void AnAttackJobIsRecognisedByItsName()
        {
            Assert.True(HostilityRules.IsAttackJob("AttackMelee"));
            Assert.True(HostilityRules.IsAttackJob("AttackStatic"));
            Assert.False(HostilityRules.IsAttackJob("GotoWander"));
            Assert.False(HostilityRules.IsAttackJob(""));
            Assert.False(HostilityRules.IsAttackJob(null));
        }

        [Fact]
        public void TheAssaultNamesAreTheOnesTheWaitTreatsAsAttacks()
        {
            Assert.True(HostilityRules.IsAssaultingLord("LordJob_AssaultColony"));
            Assert.True(HostilityRules.IsAssaultingLord("LordJob_ChimeraAssault"));
            Assert.True(HostilityRules.IsAssaultingLord("LordJob_Siege"));
            Assert.False(HostilityRules.IsAssaultingLord("LordJob_DefendAndExpandHive"));
            Assert.False(HostilityRules.IsAssaultingLord("LordJob_TradeWithColony"));
            Assert.False(HostilityRules.IsAssaultingLord(null));
        }

        // fox #3690: the fight list called DiLeo "factionless" while he beat
        // Veniamin, which says neither why he is a threat nor that the colony
        // could arrest or tame him. One label for the whole tool.
        [Fact]
        public void AWildManIsNamedAWildMan()
        {
            var wildMan = new HostilityRules.PawnState { WildMan = true };
            Assert.Equal("wild man", HostilityRules.KindOf(wildMan));
        }

        [Fact]
        public void AFactionlessWandererStaysFactionless()
        {
            var wanderer = new HostilityRules.PawnState { JobDef = "GotoWander" };
            Assert.Equal("factionless", HostilityRules.KindOf(wanderer));
        }

        [Fact]
        public void ARaiderIsNamedHostile()
        {
            var raider = new HostilityRules.PawnState { EnemyFaction = true, HasFaction = true };
            Assert.Equal("hostile", HostilityRules.KindOf(raider));
        }

        [Fact]
        public void AManhunterAnimalIsNamedManhunter()
        {
            var warg = new HostilityRules.PawnState { Animal = true, MentalState = "Manhunter" };
            Assert.Equal("manhunter", HostilityRules.KindOf(warg));
        }

        [Fact]
        public void APrisonerKeepsHisOwnLabelWhateverHisFactionIs()
        {
            var prisoner = new HostilityRules.PawnState { Prisoner = true, EnemyFaction = true, HasFaction = true };
            Assert.Equal("prisoner", HostilityRules.KindOf(prisoner));
        }

        [Fact]
        public void ANeutralTraderIsNeitherHostileNorFactionless()
        {
            var trader = new HostilityRules.PawnState { HasFaction = true };
            Assert.Equal("neutral", HostilityRules.KindOf(trader));
        }
    }
}
