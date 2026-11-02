using System.Collections.Generic;
using RIMAPI.Models;

namespace RIMAPI.Helpers
{
    // A cell list arrives from a caller that may name the same cell twice, e.g.
    // a zone read and a hand-written list added together (fox ticket #4095). A
    // repeated cell would count twice in the answer and read as "already
    // marked" on its second visit, so the sweep takes each cell once.
    public static class DesignationCells
    {
        // Unique keeps the order the caller sent and drops every later mention
        // of a cell it already holds. A null list is empty, not an error: the
        // caller then gets the "cells is empty" refusal with the same wording.
        public static List<PositionDto> Unique(IEnumerable<PositionDto> cells)
        {
            List<PositionDto> unique = new List<PositionDto>();
            if (cells == null) return unique;

            HashSet<long> seen = new HashSet<long>();
            foreach (PositionDto cell in cells)
            {
                if (cell == null) continue;
                // x and z are the map's plane; y is always the map's altitude
                // layer and does not make a second cell.
                long key = ((long)cell.X << 32) ^ (uint)cell.Z;
                if (!seen.Add(key)) continue;
                unique.Add(cell);
            }
            return unique;
        }
    }
}
