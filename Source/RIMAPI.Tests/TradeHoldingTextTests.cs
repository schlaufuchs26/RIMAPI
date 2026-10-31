using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // A refused trade has to say where the colony's goods are when the game
    // counts none of them (fox ticket #3494): colony 13 held 847 silver in two
    // stacks and every purchase failed with "the colony has 0 silver".
    public class TradeHoldingTextTests
    {
        private const string BeaconRule =
            "an orbital trade counts only goods on the cells of a powered orbital trade beacon";

        private static TradeHoldingText Silver(int total, int counted, params string[] places)
        {
            return new TradeHoldingText
            {
                Label = "silver",
                DefName = "Silver",
                Total = total,
                Counted = counted,
                Stacks = places.Length,
                Places = new List<string>(places),
                Rule = BeaconRule,
            };
        }

        [Fact]
        public void GoodsTheTradeCountsAllOfSayNothing()
        {
            TradeHoldingText holding = Silver(847, 847, "#3311 x847 in a stockpile at (146,121)");
            Assert.False(holding.HasOutside);
            Assert.Equal("", holding.Sentence());
        }

        [Fact]
        public void NothingOnTheMapSaysNothing()
        {
            TradeHoldingText holding = Silver(0, 0);
            Assert.Equal("", holding.Sentence());
        }

        [Fact]
        public void TheUncountedStacksAreNamedWithTheirCell()
        {
            TradeHoldingText holding = Silver(
                847,
                0,
                "#3311 x347 on the ground at (146,121)",
                "#46879 x500 on the ground at (132,113)");

            string sentence = holding.Sentence();

            Assert.Contains("the colony holds 847 silver in 2 stacks on the map", sentence);
            Assert.Contains("but the trade counts 0 of it", sentence);
            Assert.Contains("#3311 x347 on the ground at (146,121)", sentence);
            Assert.Contains("#46879 x500 on the ground at (132,113)", sentence);
            Assert.Contains(BeaconRule, sentence);
        }

        [Fact]
        public void TheCountedPartIsNamedEvenWhenTheRestIsBigger()
        {
            TradeHoldingText holding = Silver(847, 347, "#46879 x500 on the ground at (132,113)");
            Assert.Contains("but the trade counts 347 of it", holding.Sentence());
        }

        [Fact]
        public void OneStackIsOneStack()
        {
            TradeHoldingText holding = Silver(50, 0, "#12 x50 on the ground at (100,101)");
            Assert.Contains("in 1 stack on the map", holding.Sentence());
        }

        [Fact]
        public void TheFourthStackIsTheLastOneNamed()
        {
            TradeHoldingText holding = Silver(
                60,
                0,
                "#1 x10 on the ground at (1,1)",
                "#2 x10 on the ground at (2,2)",
                "#3 x10 on the ground at (3,3)",
                "#4 x10 on the ground at (4,4)",
                "#5 x10 on the ground at (5,5)",
                "#6 x10 on the ground at (6,6)");

            string sentence = holding.Sentence();

            Assert.Contains("#4 x10 on the ground at (4,4)", sentence);
            Assert.DoesNotContain("#5 x10", sentence);
            Assert.Contains("and 2 more stacks", sentence);
        }

        [Fact]
        public void NoRuleLeavesTheSentenceWithoutADanglingSeparator()
        {
            TradeHoldingText holding = Silver(50, 0, "#12 x50 on the ground at (100,101)");
            holding.Rule = null;
            Assert.False(holding.Sentence().EndsWith("; "));
            Assert.DoesNotContain(";", holding.Sentence());
        }
    }
}
