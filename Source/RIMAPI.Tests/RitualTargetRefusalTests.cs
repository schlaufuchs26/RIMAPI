using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3676: on colony 14 the leader speech without `pawn_id` answered "no
    // target: pass the cell the ritual is held at (x1/z1) or the building or
    // spot (target_id)", while the same call with pawn_id=1066 found the
    // campfire at 110,130 by itself and refused for the missing role instead.
    // fox #3804: the start now picks an organizer by itself (named pawn, else
    // role holder, else first colonist), so the no-organizer sentence only
    // reaches a colony with no colonist at all; where `pawn_id` is a lever
    // that cannot work. It says that instead.
    public class RitualTargetRefusalTests
    {
        [Fact]
        public void AStartInAColonyWithoutAColonistSaysNobodyCanHoldTheRitual()
        {
            string text = RitualTargetRefusal.Text(hasOrganizer: false);

            Assert.Contains("no colonist", text);
            Assert.Contains("organizer", text);
            // pawn_id cannot supply an organizer the colony does not have.
            Assert.DoesNotContain("pawn_id", text);
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
