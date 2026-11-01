using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The refusal a grave answers with (fox ticket #3784). Three empty graves
    // refused every dead colonist while the answer named three possible causes
    // at once, so a run could not tell which lever to pull. Each cause the
    // game checks has its own sentence now, and the assignment one names the
    // colonist the grave belongs to.
    public class CasketRefusalTests
    {
        private static CasketRefusal.Casket Grave()
        {
            return new CasketRefusal.Casket
            {
                Label = "Grave",
                Id = 148781,
                Filter = "corpses",
            };
        }

        [Fact]
        public void AnOccupiedGraveNamesTheCorpseItHolds()
        {
            var grave = Grave();
            grave.Holds = "Healey, Drone (dead)";

            string cause = CasketRefusal.Cause(grave, "Bucky, Drone (dead)", true, "Bucky, Drone");

            Assert.Contains("Grave #148781 already holds Healey, Drone (dead)", cause);
            Assert.Contains("build name=Grave", cause);
            // A full grave refuses before the assignment and the filter are
            // asked, which is the order Building_Grave.Accepts uses.
            Assert.DoesNotContain("assigned", cause);
            Assert.DoesNotContain("storage filter", cause);
        }

        [Fact]
        public void AnAssignedGraveNamesItsColonistAndTheCorpse()
        {
            var grave = Grave();
            grave.AssignedPawn = "Bucky";

            string cause = CasketRefusal.Cause(grave, "Healey, Drone (dead)", true, "Healey, Drone");

            Assert.Contains("Grave #148781 is assigned to Bucky", cause);
            Assert.Contains("this corpse is Healey, Drone", cause);
            Assert.Contains("job item_id=<corpse>", cause);
            // The filter decides nothing while a grave is assigned, so it
            // must not appear as a second candidate cause.
            Assert.DoesNotContain("storage filter", cause);
        }

        [Fact]
        public void AnAssignedGraveRefusingSomethingOtherThanACorpseSaysSo()
        {
            var grave = Grave();
            grave.AssignedPawn = "Bucky";

            string cause = CasketRefusal.Cause(grave, "steel", false, "");

            Assert.Contains("is assigned to Bucky", cause);
            Assert.Contains("not steel", cause);
        }

        [Fact]
        public void AFreeGraveBlamesItsFilter()
        {
            string cause = CasketRefusal.Cause(Grave(), "Muffalo, animal (dead)", true, "Muffalo");

            Assert.Contains("Grave #148781's storage filter (corpses)", cause);
            Assert.Contains("does not allow Muffalo, animal (dead)", cause);
            Assert.Contains("storage tab", cause);
        }

        [Fact]
        public void FactsThatExplainNothingLeaveTheGeneralSentenceToTheCaller()
        {
            // A casket whose filter summary is empty (a bridge state the game
            // does not fill) must not turn into "does not allow" with a blank
            // parenthesis.
            var grave = Grave();
            grave.Filter = "";

            Assert.Null(CasketRefusal.Cause(grave, "steel", false, ""));
        }
    }
}
