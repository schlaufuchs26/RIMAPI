using System;
using System.Collections.Generic;
using System.Linq;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Decides where a `grow` rectangle belongs (fox ticket #3388). The old
    /// endpoint always made a new zone from the free cells of the rectangle, so
    /// a call over an existing field produced a one-cell zone beside it, and a
    /// second call over the field itself failed with "no free cells". A grow
    /// over ground that already carries a growing zone has to join that zone
    /// instead: same plant, sowing switched on, the free cells added to it.
    ///
    /// The decision is plain integer work and lives apart from FarmHelper so
    /// the xunit project can check it without a game object graph.
    /// </summary>
    public static class GrowingZoneMerge
    {
        /// <summary>One map cell; y is ignored because zones are flat.</summary>
        public struct Cell : IEquatable<Cell>
        {
            public Cell(int x, int z)
            {
                X = x;
                Z = z;
            }

            public int X { get; }
            public int Z { get; }

            public bool Equals(Cell other) => X == other.X && Z == other.Z;

            public override bool Equals(object obj) => obj is Cell other && Equals(other);

            public override int GetHashCode() => (X * 397) ^ Z;
        }

        /// <summary>A growing zone as the merge sees it: its id and its cells.</summary>
        public class Zone
        {
            public int Id { get; set; }
            public List<Cell> Cells { get; set; } = new List<Cell>();
        }

        /// <summary>
        /// What a grow rectangle resolves to. TargetZoneId is the zone the
        /// rectangle joins, or null when the rectangle's free cells have to
        /// become a new zone. NewCells are the in-bounds cells no zone holds
        /// yet; the rest are counted so the answer can say what it left alone.
        /// </summary>
        public class Plan
        {
            public int? TargetZoneId { get; set; }
            public List<Cell> NewCells { get; } = new List<Cell>();
            public int AlreadyInTarget { get; set; }
            public int InOtherZone { get; set; }
            public int OutOfBounds { get; set; }
        }

        /// <summary>
        /// The rectangle between two corners, both ends included.
        /// </summary>
        public static List<Cell> Rectangle(Cell a, Cell b)
        {
            var cells = new List<Cell>();
            int minX = Math.Min(a.X, b.X);
            int maxX = Math.Max(a.X, b.X);
            int minZ = Math.Min(a.Z, b.Z);
            int maxZ = Math.Max(a.Z, b.Z);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    cells.Add(new Cell(x, z));
                }
            }

            return cells;
        }

        /// <summary>
        /// Picks the zone a rectangle joins: the one holding most of its cells,
        /// ties going to the lower id so the same rectangle always resolves to
        /// the same zone. A rectangle that touches no growing zone creates one.
        /// </summary>
        public static Plan Decide(
            IEnumerable<Cell> rectangle,
            Func<Cell, bool> inBounds,
            IEnumerable<Zone> zones)
        {
            var cells = rectangle.ToList();
            var owner = new Dictionary<Cell, int>();
            foreach (Zone zone in zones)
            {
                foreach (Cell cell in zone.Cells)
                {
                    owner[cell] = zone.Id;
                }
            }

            var overlap = new Dictionary<int, int>();
            foreach (Cell cell in cells)
            {
                if (!owner.TryGetValue(cell, out int zoneId))
                {
                    continue;
                }

                overlap.TryGetValue(zoneId, out int count);
                overlap[zoneId] = count + 1;
            }

            var plan = new Plan();
            if (overlap.Count > 0)
            {
                plan.TargetZoneId = overlap
                    .OrderByDescending(pair => pair.Value)
                    .ThenBy(pair => pair.Key)
                    .First()
                    .Key;
            }

            foreach (Cell cell in cells)
            {
                if (!inBounds(cell))
                {
                    plan.OutOfBounds++;
                    continue;
                }

                if (owner.TryGetValue(cell, out int zoneId))
                {
                    if (zoneId == plan.TargetZoneId)
                    {
                        plan.AlreadyInTarget++;
                    }
                    else
                    {
                        plan.InOtherZone++;
                    }

                    continue;
                }

                plan.NewCells.Add(cell);
            }

            return plan;
        }
    }
}
