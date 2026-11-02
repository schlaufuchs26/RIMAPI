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
        public void APredatorHuntingAColonistIsAThreat()
        {
            // fox #3872, measured in colony 16: a wild cougar hunted Trocas with
            // the game's own PredatorHunt job (the letter said "a cougar is
            // hunting Trocas for food"), no manhunter state on it at all. The
            // fight read said "0 hostiles" while it bit him.
            var cougar = new HostilityRules.PawnState
            {
                Animal = true,
                JobDef = "PredatorHunt",
                JobTargetsColony = true,
            };
            Assert.True(HostilityRules.ThreatensColony(cougar));
        }

        [Fact]
        public void APredatorHuntingAWildAnimalIsNoThreat()
        {
            // A cougar after a hare is the map's business, not the colony's.
            var cougar = new HostilityRules.PawnState { Animal = true, JobDef = "PredatorHunt" };
            Assert.False(HostilityRules.ThreatensColony(cougar));
        }

        [Fact]
        public void AColonistsOwnHuntIsNoHuntingJob()
        {
            // The colony's hunter is in "Hunt", not in the predator's job; the
            // animal rule must not read that as a predator.
            Assert.True(HostilityRules.IsHuntingJob("PredatorHunt"));
            Assert.False(HostilityRules.IsHuntingJob("Hunt"));
            Assert.False(HostilityRules.IsHuntingJob("AttackMelee"));
            Assert.False(HostilityRules.IsHuntingJob(""));
            Assert.False(HostilityRules.IsHuntingJob(null));
        }

        [Fact]
        public void AHuntingPredatorIsNamedAsOne()
        {
            // "factionless" said nothing about why the cougar was in the list
            // (the same complaint as #3690 about the wild man).
            var cougar = new HostilityRules.PawnState
            {
                Animal = true,
                JobDef = "PredatorHunt",
                JobTargetsColony = true,
            };
            Assert.Equal("hunting predator", HostilityRules.KindOf(cougar));
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

        // fox #4187: a prisoner of an enemy faction is hostile by faction and
        // stands in the colony's own cell, so the danger line said "1 hostile
        // on the map → draft the fighters" while the colony held a captive.
        [Fact]
        public void APrisonerInHisCellIsHeldAndNoFight()
        {
            var prisoner = new HostilityRules.PawnState
            {
                Prisoner = true,
                EnemyFaction = true,
                HasFaction = true,
                JobDef = "GotoWander",
            };
            Assert.True(HostilityRules.IsHeld(prisoner));
            // He is still in the headcount the game's rule makes: the field is
            // about which of them the colony holds, not about hiding him.
            Assert.True(HostilityRules.ThreatensColony(prisoner));
        }

        [Fact]
        public void ASlaveIsHeldToo()
        {
            var slave = new HostilityRules.PawnState { Slave = true, EnemyFaction = true, HasFaction = true };
            Assert.True(HostilityRules.IsHeld(slave));
        }

        [Fact]
        public void APrisonBreakIsNoHeldPawn()
        {
            // The cap has to keep a breakout: the lord job is the difference.
            var escaping = new HostilityRules.PawnState
            {
                Prisoner = true,
                EnemyFaction = true,
                HasFaction = true,
                LordJob = "LordJob_PrisonBreak",
            };
            Assert.False(HostilityRules.IsHeld(escaping));
        }

        [Fact]
        public void APrisonerBeatingAWardenIsNoHeldPawn()
        {
            var rioting = new HostilityRules.PawnState
            {
                Prisoner = true,
                JobDef = "AttackMelee",
                JobTargetsColony = true,
            };
            Assert.False(HostilityRules.IsHeld(rioting));
        }

        [Fact]
        public void ABerserkPrisonerIsNoHeldPawn()
        {
            var berserk = new HostilityRules.PawnState { Prisoner = true, MentalState = "Berserk" };
            Assert.False(HostilityRules.IsHeld(berserk));
        }

        [Fact]
        public void ARaiderInTheOpenIsNoHeldPawn()
        {
            Assert.False(HostilityRules.IsHeld(Raider()));
            Assert.False(HostilityRules.IsHeld(new HostilityRules.PawnState { Guest = true }));
        }
    }
}
