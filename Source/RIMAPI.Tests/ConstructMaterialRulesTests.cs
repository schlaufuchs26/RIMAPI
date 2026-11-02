using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The material half of a planned build's refusal (fox ticket #4054): a
    // plan whose only wood carries the forbidden flag read "0 on the map he
    // may use", which is the line a colony that has none gets.
    public class ConstructMaterialRulesTests
    {
        private static ConstructMaterialRules.MaterialGap Gap(int needed, int usable)
        {
            return new ConstructMaterialRules.MaterialGap
            {
                DefName = "WoodLog",
                Label = "wood",
                Needed = needed,
                Usable = usable,
            };
        }

        [Fact]
        public void ACoveredPlanHasNoShortfall()
        {
            Assert.Null(ConstructMaterialRules.Shortfall(new List<ConstructMaterialRules.MaterialGap> { Gap(25, 25) }));
            Assert.Null(ConstructMaterialRules.Shortfall(new List<ConstructMaterialRules.MaterialGap> { Gap(25, 40) }));
        }

        [Fact]
        public void AShortPlanNamesNeededAndUsable()
        {
            string text = ConstructMaterialRules.Shortfall(new List<ConstructMaterialRules.MaterialGap> { Gap(25, 10) });

            Assert.Equal("25 wood needed, 10 on the map he may use", text);
        }

        [Fact]
        public void AForbiddenStackIsNamedWithTheAllowCall()
        {
            var gap = Gap(25, 0);
            gap.Forbidden = 50;
            gap.ForbiddenIds.Add(164157);

            string text = ConstructMaterialRules.Shortfall(new List<ConstructMaterialRules.MaterialGap> { gap });

            Assert.Equal(
                "25 wood needed, 0 on the map he may use; forbidden: 50 WoodLog (thing 164157); "
                + "action allow ids=164157 clears the flag",
                text);
        }

        [Fact]
        public void EveryForbiddenStackOfADefListsItsId()
        {
            var gap = Gap(25, 0);
            gap.Forbidden = 50;
            gap.ForbiddenIds.AddRange(new[] { 7, 9 });

            string text = ConstructMaterialRules.GapText(gap);

            Assert.Contains("forbidden: 50 WoodLog (things 7, 9)", text);
            Assert.Contains("action allow ids=7,9 clears the flag", text);
        }

        [Fact]
        public void PastThreeStacksTheClauseCountsTheRest()
        {
            var gap = Gap(100, 0);
            gap.Forbidden = 120;
            gap.ForbiddenIds.AddRange(new[] { 1, 2, 3, 4, 5 });

            string text = ConstructMaterialRules.GapText(gap);

            Assert.Contains("forbidden: 120 WoodLog (things 1, 2, 3 and 2 more)", text);
            Assert.Contains("action allow ids=1,2,3,4,5 clears the flag", text);
        }

        [Fact]
        public void AForbiddenStackWithoutAnIdNamesNoCall()
        {
            var gap = Gap(25, 0);
            gap.Forbidden = 50;

            string text = ConstructMaterialRules.GapText(gap);

            Assert.Equal("25 wood needed, 0 on the map he may use; forbidden: 50 WoodLog", text);
        }

        [Fact]
        public void AStackOutOfReachIsNamedAsSuch()
        {
            var gap = Gap(25, 0);
            gap.OutOfReach = 50;

            string text = ConstructMaterialRules.GapText(gap);

            Assert.Equal("25 wood needed, 0 on the map he may use; 50 WoodLog lie out of his reach", text);
        }

        [Fact]
        public void TwoShortMaterialsAreTwoClauses()
        {
            var wood = Gap(25, 10);
            var steel = new ConstructMaterialRules.MaterialGap
            {
                DefName = "Steel",
                Label = "steel",
                Needed = 40,
                Usable = 30,
            };

            string text = ConstructMaterialRules.Shortfall(
                new List<ConstructMaterialRules.MaterialGap> { wood, steel });

            Assert.Equal("25 wood needed, 10 on the map he may use; 40 steel needed, 30 on the map he may use", text);
        }

        [Fact]
        public void ACoveredMaterialIsLeftOutOfAMixedSentence()
        {
            var wood = Gap(25, 0);
            var steel = new ConstructMaterialRules.MaterialGap
            {
                DefName = "Steel",
                Label = "steel",
                Needed = 40,
                Usable = 40,
            };

            string text = ConstructMaterialRules.Shortfall(
                new List<ConstructMaterialRules.MaterialGap> { wood, steel });

            Assert.Equal("25 wood needed, 0 on the map he may use", text);
        }
    }
}
