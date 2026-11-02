using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    // Which cell of a haul destination an ordered haul may use (fox ticket
    // #4058). StoreUtility.IsGoodStoreCell also asks whether the carrier can
    // reserve the cell *new*, and a cell his own running job already reserved
    // fails that question. A shelf the colony was itself hauling into therefore
    // answered "has no free cell that takes simple meal" while a cell stood
    // empty, and no call could put a thing into a shelf for a live check.
    //
    // The verdicts arrive as plain values, so the choice is testable without a
    // game; ColonistService fills them from the live cell.
    public static class HaulCellChoice
    {
        // Verdict is why one cell of a slot group is or is not usable.
        public enum Verdict
        {
            // Good: the game's own check takes the cell.
            Good,

            // CarrierReserved: the only thing in the way is a reservation the
            // carrier himself holds. The order replaces his current job and
            // with it that reservation, so the cell counts as free.
            CarrierReserved,

            // Blocked: a full cell, another blocker, a fire, no path, or a
            // reservation by someone else.
            Blocked,
        }

        // Choose is the index of the cell to haul into: the first the game
        // takes, else the first only the carrier's own reservation blocked.
        // -1 when no cell is usable.
        public static int Choose(IReadOnlyList<Verdict> cells)
        {
            if (cells == null)
            {
                return -1;
            }
            int reserved = -1;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == Verdict.Good)
                {
                    return i;
                }
                if (cells[i] == Verdict.CarrierReserved && reserved < 0)
                {
                    reserved = i;
                }
            }
            return reserved;
        }
    }
}
