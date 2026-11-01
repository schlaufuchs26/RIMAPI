using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3881: a quest offer letter says only that a quest is available, so a
    // run could read the defName of a forced-weather quest but never which
    // weather it would bring. The letter body now carries the quest's own
    // description after its own words.
    public class QuestOfferTextTests
    {
        [Fact]
        public void TheDescriptionFollowsTheLettersOwnWords()
        {
            var body = QuestOfferText.Body(
                "A new quest is available.\n\nThe quest is called 'Paid Cold Snap'.",
                "A machine persona wants to run an experiment. A cold snap will begin in 1 day."
            );

            Assert.Equal(
                "A new quest is available.\n\nThe quest is called 'Paid Cold Snap'."
                    + "\n\nA machine persona wants to run an experiment. A cold snap will begin in 1 day.",
                body
            );
        }

        [Fact]
        public void ALetterWithoutItsOwnTextCarriesTheDescriptionAlone()
        {
            Assert.Equal("A cold snap will begin.", QuestOfferText.Body("", "A cold snap will begin."));
            Assert.Equal("A cold snap will begin.", QuestOfferText.Body(null, "  A cold snap will begin.  "));
        }

        [Fact]
        public void AQuestWithoutADescriptionLeavesTheLetterAsItWas()
        {
            Assert.Equal("A new quest is available.", QuestOfferText.Body("A new quest is available.", ""));
            Assert.Equal("A new quest is available.", QuestOfferText.Body("A new quest is available.\r\n", null));
        }
    }
}
