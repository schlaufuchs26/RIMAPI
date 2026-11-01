using System.Collections.Generic;
using RIMAPI.Helpers;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3467: the animals read. No read named a living animal's id, so a run
    // that wanted a muffalo for wool could not call `tame`, and the `designate`
    // hunt it used instead answered with a manhunter charge.
    public class AnimalRulesTests
    {
        [Fact]
        public void APlayerAnimalIsTame()
        {
            Assert.Equal(AnimalRules.Tame, AnimalRules.Side(true, "New Arrivals"));
        }

        [Fact]
        public void AnAnimalOfNoFactionIsWild()
        {
            Assert.Equal(AnimalRules.Wild, AnimalRules.Side(false, null));
            Assert.Equal(AnimalRules.Wild, AnimalRules.Side(false, ""));
        }

        [Fact]
        public void AnAnimalOfAnotherFactionNamesIt()
        {
            Assert.Equal("Rough Outlanders", AnimalRules.Side(false, "Rough Outlanders"));
        }

        // The order is what a run decides on first: the herd that attacks on
        // sight, then the wild animals it could tame or hunt, then its own.
        [Fact]
        public void AManhunterLeadsThenWildThenOwn()
        {
            var own = new AnimalDto { Id = 3, Label = "muffalo", Side = AnimalRules.Tame };
            var wild = new AnimalDto { Id = 2, Label = "muffalo", Side = AnimalRules.Wild };
            var mad = new AnimalDto { Id = 1, Label = "zebra", Side = AnimalRules.Wild, Manhunter = true };
            var list = new List<AnimalDto> { own, wild, mad };

            AnimalRules.Sort(list);

            Assert.Equal(new[] { 1, 2, 3 }, new[] { list[0].Id, list[1].Id, list[2].Id });
        }

        // A quest guest is listed before the colony's own herd: a run that picks
        // the first tame animal for slaughter must not pick the one the quest
        // charged it to protect (fox #3611).
        [Fact]
        public void AQuestGuestComesBeforeTheOwnHerd()
        {
            var own = new AnimalDto { Id = 3, Label = "sheep", Side = AnimalRules.Tame };
            var guest = new AnimalDto { Id = 4, Label = "sheep", Side = AnimalRules.Tame, QuestLodger = true };
            var list = new List<AnimalDto> { own, guest };

            AnimalRules.Sort(list);

            Assert.Equal(new[] { 4, 3 }, new[] { list[0].Id, list[1].Id });
        }

        [Fact]
        public void TheSameAnimalsComeBackInTheSameOrder()
        {
            var a = new AnimalDto { Id = 7, Label = "muffalo", Side = AnimalRules.Wild };
            var b = new AnimalDto { Id = 5, Label = "muffalo", Side = AnimalRules.Wild };
            var c = new AnimalDto { Id = 9, Label = "boomalope", Side = AnimalRules.Wild };
            var list = new List<AnimalDto> { a, b, c };

            AnimalRules.Sort(list);

            Assert.Equal(new[] { 9, 5, 7 }, new[] { list[0].Id, list[1].Id, list[2].Id });
        }

        [Fact]
        public void TheSummaryCountsWildTameAndManhunters()
        {
            var list = new List<AnimalDto>
            {
                new AnimalDto { Id = 1, Label = "muffalo", Side = AnimalRules.Wild },
                new AnimalDto { Id = 2, Label = "muffalo", Side = AnimalRules.Wild, Manhunter = true },
                new AnimalDto { Id = 3, Label = "muffalo", Side = AnimalRules.Tame },
            };

            Assert.Equal("3 animals on map 0: 2 wild (1 manhunter), 1 tame", AnimalRules.Summary(0, list));
        }

        // A hunt designation from an earlier run is why a tame call finds no
        // free animal, so the count belongs in the summary.
        [Fact]
        public void TheSummaryNamesTheMarkedAnimals()
        {
            var list = new List<AnimalDto>
            {
                new AnimalDto { Id = 1, Label = "muffalo", Side = AnimalRules.Wild, HuntDesignated = true },
                new AnimalDto { Id = 2, Label = "muffalo", Side = AnimalRules.Wild, TameDesignated = true },
            };

            Assert.Equal("2 animals on map 3: 2 wild; 1 marked for hunting, 1 for taming",
                AnimalRules.Summary(3, list));
        }

        [Fact]
        public void AnEmptyMapSaysSo()
        {
            Assert.Equal("No animal stands on map 0: nothing to hunt, tame or pen.",
                AnimalRules.Summary(0, new List<AnimalDto>()));
        }
    }
}
