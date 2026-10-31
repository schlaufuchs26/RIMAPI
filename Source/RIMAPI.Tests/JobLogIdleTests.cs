using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The job record's idle flag (ticket #3392): the game itself knows whether
    /// a colonist has something to do (`Pawn_MindState.IsIdle`, the same state
    /// its "colonists idle" alert shows). The tool used to keep its own list of
    /// wander job names, so a new wander job in a game update counted as work
    /// and the idle share measured zero for weeks. These tests pin the change
    /// rule that the record and the tool both depend on.
    /// </summary>
    public class JobLogIdleTests
    {
        [Fact]
        public void TheSameStateWritesNoLine()
        {
            // StartJob and EndCurrentJob both fire on one job change, so the
            // second hook must recognise the state the first one recorded.
            Assert.True(JobLogState.Same("GotoWander", "", true, "GotoWander", "", true));
        }

        [Fact]
        public void ANewJobWritesALine()
        {
            Assert.False(JobLogState.Same("GotoWander", "", true, "DoBill", "", false));
        }

        [Fact]
        public void ANewTargetWritesALine()
        {
            Assert.False(JobLogState.Same("HaulToCell", "steel", false, "HaulToCell", "wood", false));
        }

        [Fact]
        public void AnIdleFlipAloneWritesALine()
        {
            // A pawn can keep the same job def and target and still stop being
            // idle (the game takes the idle tag off); without this the record
            // would carry the old idle state until the job changed.
            Assert.False(JobLogState.Same("GotoWander", "", true, "GotoWander", "", false));
            Assert.False(JobLogState.Same("GotoWander", "", false, "GotoWander", "", true));
        }
    }
}
