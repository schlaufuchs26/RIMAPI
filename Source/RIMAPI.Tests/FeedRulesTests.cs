using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3680: `feed` without a meal id answered "no meal the patient may eat
    // is in reach" while the colony stored 16 meals, because the patient was
    // not hungry and the game's own gate refused the job. These state what a
    // refusal has to say instead.
    public class FeedRulesTests
    {
        // The incident itself: the patient had just eaten, so the answer must
        // name hunger, not a missing meal.
        [Fact]
        public void TheNotHungryAnswerNamesTheHungerGateAndTheStoredMeals()
        {
            string text = FeedRules.NotHungry("Metplaa (id 111417)", 78f, 30f, 16, 2);
            Assert.Contains("not hungry", text);
            Assert.Contains("78%", text);
            Assert.Contains("30%", text);
            Assert.Contains("16 meals in 2 stacks", text);
            Assert.Contains("nothing to cook", text);
            Assert.DoesNotContain("no meal", text);
        }

        [Fact]
        public void TheNotHungryAnswerWithoutStoredMealsSaysToWait()
        {
            string text = FeedRules.NotHungry("Diver (id 7)", 55f, 30f, 0, 0);
            Assert.Contains("wait rather than cook", text);
        }

        [Fact]
        public void OneStackIsNamedAsOneStack()
        {
            string text = FeedRules.NotHungry("Diver (id 7)", 60f, 30f, 3, 1);
            Assert.Contains("3 meals in one stack", text);
        }

        // The other half of the ticket: a patient who does need feeding and
        // gets nothing has to learn which stacks were looked at.
        [Fact]
        public void TheNoMealAnswerNamesEveryStackWithItsReason()
        {
            var considered = new List<MealRefusal>
            {
                new MealRefusal("simple meal", 4400, 10, "is forbidden"),
                new MealRefusal("simple meal", 4401, 6, "is out of reach or reserved by another pawn"),
                new MealRefusal("raw rice", 4402, 40, "is too unappetising while the patient is only hungry"),
            };
            string text = FeedRules.NoMealRefusal(considered);
            Assert.Contains("none of the 3 stored food stacks", text);
            Assert.Contains("simple meal x10 (id 4400) is forbidden", text);
            Assert.Contains("simple meal x6 (id 4401) is out of reach", text);
            Assert.Contains("raw rice x40 (id 4402)", text);
            Assert.Contains("`allow`", text);
            Assert.Contains("`bill`", text);
            Assert.DoesNotContain("and 0 more", text);
        }

        [Fact]
        public void ALongStoreIsListedUpToTheCapAndCounted()
        {
            var considered = new List<MealRefusal>();
            for (int i = 0; i < FeedRules.MaxListedStacks + 3; i++)
            {
                considered.Add(new MealRefusal("simple meal", 5000 + i, 2, "is rotten"));
            }
            string text = FeedRules.NoMealRefusal(considered);
            Assert.Contains("none of the 7 stored food stacks", text);
            Assert.Contains("(id 5003)", text);
            Assert.DoesNotContain("(id 5004)", text);
            Assert.Contains("and 3 more", text);
        }

        // Nothing stored at all is a different answer than "everything stored
        // is unusable": there is nothing to allow.
        [Fact]
        public void AnEmptyMapAsksForCookingInsteadOfAllowing()
        {
            string text = FeedRules.NoMealRefusal(new List<MealRefusal>());
            Assert.Contains("stores no food at all", text);
            Assert.Contains("`bill`", text);
            Assert.DoesNotContain("`allow`", text);
        }
    }
}
