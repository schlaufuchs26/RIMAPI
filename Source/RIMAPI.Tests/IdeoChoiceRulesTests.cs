using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The rules behind a hand-picked precept set (fox ticket #3927): two
    // precepts of one issue cannot stand together, a precept needs its meme,
    // and an effect line states the game's own number.
    public class IdeoChoiceRulesTests
    {
        private static IdeoChoiceRules.PreceptFacts Precept(
            string def, string issue, string tipLabel = null,
            string[] requiredMemes = null, string[] conflictingMemes = null)
        {
            return new IdeoChoiceRules.PreceptFacts
            {
                Def = def,
                TipLabel = tipLabel ?? issue + ": " + def,
                Issue = issue,
                RequiredMemes = new List<string>(requiredMemes ?? new string[0]),
                ConflictingMemes = new List<string>(conflictingMemes ?? new string[0]),
            };
        }

        [Fact]
        public void TwoPreceptsOfOneIssueAreRefused()
        {
            string problem = IdeoChoiceRules.Problem(
                "Individualist",
                new[] { "Structure_Ideological", "Individualist" },
                new List<IdeoChoiceRules.PreceptFacts>
                {
                    Precept("Research_VeryFast", "Research"),
                    Precept("Research_Slow", "Research"),
                });

            Assert.NotNull(problem);
            Assert.Contains("Research", problem);
        }

        [Fact]
        public void APreceptWhoseMemeTheIdeoDoesNotCarryIsRefused()
        {
            string problem = IdeoChoiceRules.Problem(
                "Individualist",
                new[] { "Structure_Ideological", "Individualist" },
                new List<IdeoChoiceRules.PreceptFacts>
                {
                    Precept("MiningYield_High", "Mining", requiredMemes: new[] { "Tunneler" }),
                });

            Assert.NotNull(problem);
            Assert.Contains("Tunneler", problem);
        }

        [Fact]
        public void APreceptOfTheChosenMemeStands()
        {
            string problem = IdeoChoiceRules.Problem(
                "Individualist",
                new[] { "Structure_Ideological", "Individualist" },
                new List<IdeoChoiceRules.PreceptFacts>
                {
                    Precept("IdeoDiversity_Exalted", "IdeoDiversity", requiredMemes: new[] { "Individualist" }),
                    Precept("Research_VeryFast", "Research"),
                });

            Assert.Null(problem);
        }

        [Fact]
        public void APreceptThatConflictsWithTheChosenMemeIsRefused()
        {
            string problem = IdeoChoiceRules.Problem(
                "Individualist",
                new[] { "Structure_Ideological", "Individualist" },
                new List<IdeoChoiceRules.PreceptFacts>
                {
                    Precept("Collectivist_Thing", "WorkDrive", conflictingMemes: new[] { "Individualist" }),
                });

            Assert.NotNull(problem);
            Assert.Contains("Individualist", problem);
        }

        [Fact]
        public void AnEmptySetIsNoProblem()
        {
            Assert.Null(IdeoChoiceRules.Problem("Individualist", new[] { "Individualist" }, null));
        }

        [Fact]
        public void StatFactorsAndOffsetsReadAsTheGameStoresThem()
        {
            Assert.Equal("Research speed x1.5", IdeoChoiceRules.StatFactor("Research speed", 1.5f));
            Assert.Equal("Mining yield +0.1", IdeoChoiceRules.StatOffset("Mining yield", 0.1f));
            Assert.Equal("Work speed -0.08", IdeoChoiceRules.StatOffset("Work speed", -0.08f));
        }

        [Fact]
        public void AMoodRangeNamesBothEnds()
        {
            Assert.Equal("mood -4..+9", IdeoChoiceRules.MoodRange(new[] { 0f, -4f, 9f }));
            Assert.Equal("mood +3", IdeoChoiceRules.MoodRange(new[] { 3f, 3f }));
            Assert.Null(IdeoChoiceRules.MoodRange(new[] { 0f, 0f }));
            Assert.Null(IdeoChoiceRules.MoodRange(new float[0]));
        }
    }
}
