using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// Why a pawn cannot be drafted or ordered (fox ticket #3433): a colonist
    /// in a mental state used to be refused as "not one of the colony's
    /// colonists", which a run reads as "he died or left".
    /// </summary>
    public class CombatRefusalTests
    {
        private static CombatRefusal.PawnState Colonist()
        {
            return new CombatRefusal.PawnState
            {
                Spawned = true,
                Colonist = true,
                HasDrafter = true,
            };
        }

        [Fact]
        public void A_standing_colonist_is_not_refused()
        {
            Assert.Null(CombatRefusal.DraftNote(Colonist()));
            Assert.Null(CombatRefusal.OrderRefusal("Gray", Colonist()));
        }

        [Fact]
        public void A_colonist_in_a_tantrum_names_the_state()
        {
            CombatRefusal.PawnState pawn = Colonist();
            pawn.MentalState = "tantrum";

            Assert.Equal(
                "in a mental state (tantrum), so he cannot be drafted",
                CombatRefusal.DraftNote(pawn));
            Assert.Equal(
                "Gray is in a mental state (tantrum), so he cannot act",
                CombatRefusal.OrderRefusal("Gray", pawn));
        }

        [Fact]
        public void A_colonist_in_a_mental_state_is_never_called_a_foreigner()
        {
            CombatRefusal.PawnState pawn = Colonist();
            pawn.MentalState = "berserk";
            pawn.Downed = true;

            Assert.DoesNotContain("not one of the colony's colonists", CombatRefusal.DraftNote(pawn));
        }

        [Fact]
        public void A_carried_colonist_names_the_carrier()
        {
            CombatRefusal.PawnState pawn = Colonist();
            pawn.Spawned = false;
            pawn.CarriedBy = "Marie";

            Assert.Equal(
                "being carried by Marie, so he cannot be drafted",
                CombatRefusal.DraftNote(pawn));
        }

        [Fact]
        public void A_colonist_off_the_map_is_not_a_foreigner()
        {
            CombatRefusal.PawnState pawn = Colonist();
            pawn.Spawned = false;

            Assert.Equal(
                "not on the map (a caravan, a pod or a cryptosleep casket), so he cannot be drafted",
                CombatRefusal.DraftNote(pawn));
        }

        [Fact]
        public void A_dead_colonist_reads_dead_before_anything_else()
        {
            CombatRefusal.PawnState pawn = Colonist();
            pawn.Dead = true;
            pawn.Spawned = false;

            Assert.Equal("dead, so he cannot be drafted", CombatRefusal.DraftNote(pawn));
            Assert.Equal("Gray is dead, so he cannot act", CombatRefusal.OrderRefusal("Gray", pawn));
        }

        [Fact]
        public void A_prisoner_is_not_one_of_the_colonists()
        {
            CombatRefusal.PawnState pawn = Colonist();
            pawn.Colonist = false;

            Assert.Equal(
                "not one of the colony's colonists, so he cannot be drafted",
                CombatRefusal.DraftNote(pawn));
        }

        [Fact]
        public void A_downed_colonist_and_one_without_a_drafter_say_which()
        {
            CombatRefusal.PawnState downed = Colonist();
            downed.Downed = true;
            Assert.Equal("downed, so he cannot be drafted", CombatRefusal.DraftNote(downed));

            CombatRefusal.PawnState noDrafter = Colonist();
            noDrafter.HasDrafter = false;
            Assert.Equal(
                "Gray is without a draft controller, so he cannot act",
                CombatRefusal.OrderRefusal("Gray", noDrafter));
        }

        [Fact]
        public void A_colonist_in_a_mental_state_cannot_arrest_and_the_answer_says_so()
        {
            CombatRefusal.PawnState pawn = Colonist();
            pawn.MentalState = "tantrum";

            Assert.Equal(
                "Gray (id 628) is in a mental state (tantrum), so he cannot arrest",
                CombatRefusal.ActorRefusal("Gray (id 628)", "arrest", pawn));
            Assert.DoesNotContain(
                "not one of the colony's colonists",
                CombatRefusal.ActorRefusal("Gray (id 628)", "arrest", pawn));
        }

        [Fact]
        public void A_sane_colonist_is_not_refused_as_an_actor()
        {
            Assert.Null(CombatRefusal.ActorRefusal("Gray", "rescue", Colonist()));
        }

        [Fact]
        public void An_actor_who_is_no_colonist_is_still_named_one()
        {
            CombatRefusal.PawnState pawn = Colonist();
            pawn.Colonist = false;

            Assert.Equal(
                "Gray is not one of the colony's colonists, so he cannot feed",
                CombatRefusal.ActorRefusal("Gray", "feed", pawn));
        }

        [Fact]
        public void A_state_without_a_label_falls_back_to_its_defname()
        {
            Assert.Equal("tantrum", CombatRefusal.MentalStateName("tantrum", "Tantrum"));
            Assert.Equal("Berserk", CombatRefusal.MentalStateName("", "Berserk"));
            Assert.Equal("Berserk", CombatRefusal.MentalStateName(null, "Berserk"));
            Assert.Equal("unknown", CombatRefusal.MentalStateName(null, null));
        }

        [Fact]
        public void The_bare_state_reads_in_a_work_refusal_too()
        {
            CombatRefusal.PawnState pawn = Colonist();
            Assert.Null(CombatRefusal.StateOf(pawn));

            pawn.MentalState = "tantrum";
            Assert.Equal("in a mental state (tantrum)", CombatRefusal.StateOf(pawn));

            // The draft controller is a draft condition, not a work one.
            CombatRefusal.PawnState noDrafter = Colonist();
            noDrafter.HasDrafter = false;
            Assert.Null(CombatRefusal.StateOf(noDrafter));
        }
    }
}
