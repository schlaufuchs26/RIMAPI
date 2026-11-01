using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3507: the slaughter order. A colony animal tamed for wool had no
    // lever that ends its life for meat, because the hunt designation only
    // reaches wild animals. Each refusal names the order that does apply.
    public class SlaughterRulesTests
    {
        private static SlaughterRules.AnimalState ColonyAnimal()
        {
            return new SlaughterRules.AnimalState { Animal = true, PlayerFaction = true };
        }

        [Fact]
        public void AColonyAnimalMayBeSlaughtered()
        {
            Assert.Null(SlaughterRules.Refusal(ColonyAnimal()));
        }

        [Fact]
        public void AWildAnimalIsSentToTheHuntDesignation()
        {
            var wild = new SlaughterRules.AnimalState { Animal = true };
            string refusal = SlaughterRules.Refusal(wild);
            Assert.Contains("wild", refusal);
            Assert.Contains("type=hunt", refusal);
        }

        [Fact]
        public void AnAnimalOfAnotherFactionNamesItsOwner()
        {
            var theirs = new SlaughterRules.AnimalState { Animal = true, FactionName = "Rough Outlanders" };
            string refusal = SlaughterRules.Refusal(theirs);
            Assert.Contains("Rough Outlanders", refusal);
            Assert.DoesNotContain("type=hunt", refusal);
        }

        [Fact]
        public void AHumanlikePawnIsNotSlaughtered()
        {
            var human = new SlaughterRules.AnimalState { PlayerFaction = true };
            string refusal = SlaughterRules.Refusal(human);
            Assert.Contains("only animals", refusal);
        }

        // A quest lodger is the colony's by faction and still not the colony's
        // to kill: colony 14 slaughtered a quest sheep, the quest failed and
        // the guest faction dropped to -7 (fox #3611).
        [Fact]
        public void AQuestGuestIsRefusedBeforeTheOrderGoesOut()
        {
            var guest = ColonyAnimal();
            guest.QuestLodger = true;
            string refusal = SlaughterRules.Refusal(guest);
            Assert.Contains("quest guest", refusal);
            Assert.Contains("fails the quest", refusal);
        }

        [Fact]
        public void ADeadAnimalIsNamedAsDead()
        {
            var dead = ColonyAnimal();
            dead.Dead = true;
            Assert.Equal("he is already dead", SlaughterRules.Refusal(dead));
        }

        [Fact]
        public void AMentalStateIsNamedBeforeTheOrderGoesOut()
        {
            var mad = ColonyAnimal();
            mad.MentalState = "Manhunter";
            string refusal = SlaughterRules.Refusal(mad);
            Assert.Contains("mental state (Manhunter)", refusal);
        }
    }
}
