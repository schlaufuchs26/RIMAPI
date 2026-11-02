using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // Which cell of a haul destination an ordered haul uses (fox ticket
    // #4058). A steel shelf the colony was itself hauling into answered "has no
    // free cell that takes simple meal" while one of its cells stood empty: the
    // cell the carrier's own running job had reserved failed the game's
    // can-reserve-new check. The order replaces that job, so its reservation is
    // no blocker, and the choice now says so.
    public class HaulCellChoiceTests
    {
        private static HaulCellChoice.Verdict[] Cells(params HaulCellChoice.Verdict[] cells)
        {
            return cells;
        }

        [Fact]
        public void ACleanCellIsPreferred()
        {
            int index = HaulCellChoice.Choose(
                Cells(
                    HaulCellChoice.Verdict.Blocked,
                    HaulCellChoice.Verdict.CarrierReserved,
                    HaulCellChoice.Verdict.Good
                )
            );

            Assert.Equal(2, index);
        }

        [Fact]
        public void ACellOnlyTheCarrierReservedIsStillUsed()
        {
            int index = HaulCellChoice.Choose(
                Cells(HaulCellChoice.Verdict.Blocked, HaulCellChoice.Verdict.CarrierReserved)
            );

            Assert.Equal(1, index);
        }

        [Fact]
        public void ACleanCellAfterAReservedOneWins()
        {
            int index = HaulCellChoice.Choose(
                Cells(HaulCellChoice.Verdict.CarrierReserved, HaulCellChoice.Verdict.Good)
            );

            Assert.Equal(1, index);
        }

        [Fact]
        public void NoUsableCellAnswersMinusOne()
        {
            Assert.Equal(
                -1,
                HaulCellChoice.Choose(Cells(HaulCellChoice.Verdict.Blocked, HaulCellChoice.Verdict.Blocked))
            );
            Assert.Equal(-1, HaulCellChoice.Choose(Cells()));
            Assert.Equal(-1, HaulCellChoice.Choose(null));
        }

        [Fact]
        public void TheFirstReservedCellIsTheOneChosen()
        {
            var cells = new List<HaulCellChoice.Verdict>
            {
                HaulCellChoice.Verdict.Blocked,
                HaulCellChoice.Verdict.CarrierReserved,
                HaulCellChoice.Verdict.CarrierReserved,
            };

            Assert.Equal(1, HaulCellChoice.Choose(cells));
        }
    }
}
