using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// A frame with no work left cannot be finished by the game: its failure
    /// roll divides by WorkToBuild, so a zero work fails the frame on every
    /// tick and the blueprint comes back forever (fox ticket #3610, the animal
    /// sleeping spot that never existed).
    /// </summary>
    public class FrameConstructionTests
    {
        [Fact]
        public void A_frame_with_no_work_is_finished_instead_of_failed()
        {
            Assert.True(FrameConstruction.FinishInsteadOfFailing(0f, workerMissing: false));
        }

        [Fact]
        public void A_frame_that_needs_work_keeps_the_game_roll()
        {
            Assert.False(FrameConstruction.FinishInsteadOfFailing(85f, workerMissing: false));
            Assert.False(FrameConstruction.FinishInsteadOfFailing(0.5f, workerMissing: false));
        }

        [Fact]
        public void A_frame_without_a_worker_keeps_the_game_path()
        {
            // Nobody to credit for the construction, so the rule does not apply.
            Assert.False(FrameConstruction.FinishInsteadOfFailing(0f, workerMissing: true));
        }

        [Fact]
        public void The_log_line_names_the_def_and_its_work()
        {
            Assert.Equal(
                "finished a zero-work frame instead of failing it: AnimalSleepingSpot needs 0 work",
                FrameConstruction.LogLine("AnimalSleepingSpot", 0f));
        }
    }
}
