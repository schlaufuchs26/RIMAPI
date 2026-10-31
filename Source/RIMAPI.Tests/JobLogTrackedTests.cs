using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// Who the job record follows (ticket #3505). The record used the game's
    /// IsColonistPlayerControlled, which is Spawned &amp;&amp; IsColonist &amp;&amp;
    /// MentalStateDef == null &amp;&amp; HostFaction == null, so it dropped a colonist
    /// exactly during a tantrum or a berserk: the history had a gap just when a
    /// run wants to see what he is doing.
    /// </summary>
    public class JobLogTrackedTests
    {
        [Fact]
        public void AColonistInAMentalStateIsTracked()
        {
            // The game's player-controlled flag is false for him; the colony
            // flag and the player faction are not.
            Assert.True(JobLogState.Tracked(true, true));
        }

        [Fact]
        public void SomeoneOutsideTheColonyIsNotTracked()
        {
            // A prisoner, a guest and a raider are on the map but not the
            // colony's: the colonist flag fails, and so does the faction.
            Assert.False(JobLogState.Tracked(false, false));
        }
    }
}
