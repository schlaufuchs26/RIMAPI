using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3676: on colony 14 the leader speech without `pawn_id` answered "no
    // target: pass the cell the ritual is held at (x1/z1) or the building or
    // spot (target_id)", while the same call with pawn_id=1066 found the
    // campfire at 110,130 by itself and refused for the missing role instead.
    // The game's target filter measures from an organizer, and pawn_id is what
    // supplies one, so the refusal has to name that field rather than send the
    // run after a cell the `rituals` read already prints.
    public class RitualTargetRefusalTests
    {
        [Fact]
        public void AStartWithoutAnOrganizerNamesPawnId()
        {
            string text = RitualTargetRefusal.Text(hasOrganizer: false);

            Assert.Contains("pawn_id", text);
            Assert.Contains("organizer", text);
            // The place still works, so the sentence must not drop it.
            Assert.Contains("x1/z1", text);
            Assert.Contains("target_id", text);
        }

        [Fact]
        public void AStartWithAnOrganizerKeepsThePlacesItCanPass()
        {
            string text = RitualTargetRefusal.Text(hasOrganizer: true);

            Assert.DoesNotContain("pawn_id", text);
            Assert.Contains("x1/z1", text);
            Assert.Contains("target_id", text);
        }
    }
}
