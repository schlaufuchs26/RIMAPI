using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // Ticket #3371: a digest line that says "the cook is too slow" needs the
    // colonist to name. The pick is the part of that line that can be wrong
    // without the game noticing, so it is tested here.
    public class SkillPickerTests
    {
        private static SkillPicker.Candidate C(string name, int level, bool works)
        {
            return new SkillPicker.Candidate { Name = name, Level = level, Works = works };
        }

        [Fact]
        public void TheBestWorkerWins()
        {
            SkillPicker.Best best = SkillPicker.Pick(new List<SkillPicker.Candidate>
            {
                C("Odette", 0, true),
                C("Joy", 7, true),
                C("Knives", 3, true),
            });

            Assert.Equal("Joy", best.Name);
            Assert.Equal(7, best.Level);
        }

        [Fact]
        public void AColonistWhoseWorkTypeIsOffIsNotTheAnswer()
        {
            // A level-12 miner with Cooking switched off cannot cook, so naming
            // him would send a run to the wrong lever.
            SkillPicker.Best best = SkillPicker.Pick(new List<SkillPicker.Candidate>
            {
                C("Odette", 2, true),
                C("Edmund", 12, false),
            });

            Assert.Equal("Odette", best.Name);
            Assert.Equal(2, best.Level);
        }

        [Fact]
        public void NobodyWorkingTheTypeStillNamesTheBestTheColonyHas()
        {
            SkillPicker.Best best = SkillPicker.Pick(new List<SkillPicker.Candidate>
            {
                C("Odette", 4, false),
                C("Edmund", 9, false),
            });

            Assert.Equal("Edmund", best.Name);
            Assert.Equal(9, best.Level);
        }

        [Fact]
        public void LevelZeroIsALevel()
        {
            // The whole point: Cooking 0 has to come out as 0 and not as
            // "unknown", so the line can say the cook is too slow.
            SkillPicker.Best best = SkillPicker.Pick(new List<SkillPicker.Candidate>
            {
                C("Odette", 0, true),
            });

            Assert.Equal("Odette", best.Name);
            Assert.Equal(0, best.Level);
        }

        [Fact]
        public void EqualLevelsKeepTheFirstSoTheAnswerIsStable()
        {
            SkillPicker.Best best = SkillPicker.Pick(new List<SkillPicker.Candidate>
            {
                C("Odette", 3, true),
                C("Joy", 3, true),
            });

            Assert.Equal("Odette", best.Name);
        }

        [Fact]
        public void NoCandidateAnswersNothing()
        {
            SkillPicker.Best best = SkillPicker.Pick(new List<SkillPicker.Candidate>());

            Assert.Null(best.Name);
            Assert.Equal(0, best.Level);
        }
    }
}
