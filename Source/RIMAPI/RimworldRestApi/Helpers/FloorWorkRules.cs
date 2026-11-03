using System;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// What one cell of a floor order turns into (fox ticket #4316, parity
    /// wave 7). The game's own designators decide this per cell and refuse with
    /// a translated sentence, so a route that marks a rectangle has to answer
    /// the same verdicts: Designator_Smooth.CanDesignateCell and
    /// Designator_RemoveFloor.CanDesignateCell are the sources, read in the same
    /// order. No RimWorld type appears here, so RIMAPI.Tests covers the order
    /// of the checks (FloorWorkRulesTests).
    /// </summary>
    public static class FloorWorkRules
    {
        public enum Verdict
        {
            /// <summary>The cell takes a designation.</summary>
            Designate,

            /// <summary>A smooth order on a wall: the designation goes on the wall, not the cell's floor.</summary>
            DesignateWall,

            /// <summary>The cell already carries this order.</summary>
            Already,

            /// <summary>No cell to mark: outside the map, or under fog the player cannot see.</summary>
            Unseen,

            /// <summary>The game refuses the cell; Reason says why.</summary>
            Refused,
        }

        /// <summary>The facts one cell of a smooth order carries.</summary>
        public struct SmoothCell
        {
            public bool InBounds;
            public bool Fogged;
            public bool TooCloseToMapEdge;
            public bool AlreadyDesignated;
            public bool EdificeSmoothable;
            public bool EdificeBlocksFloor;
            public bool HasSmoothableStone;
        }

        /// <summary>The facts one cell of a remove-floor order carries.</summary>
        public struct RemoveFloorCell
        {
            public bool InBounds;
            public bool Fogged;
            public bool AlreadyDesignated;
            public bool EdificeBlocksFloor;
            public bool TerrainRemovable;
            public bool BuildingBlocksRemoval;
        }

        /// <summary>
        /// Smooth is Designator_SmoothSurface: a smoothable edifice is the wall
        /// order, a smoothable stone affordance the floor order, and anything
        /// else is refused with the game's own "must designate a smoothable
        /// surface".
        /// </summary>
        public static Verdict Smooth(SmoothCell cell, out string reason)
        {
            reason = null;
            if (!cell.InBounds || cell.Fogged)
            {
                return Verdict.Unseen;
            }

            if (cell.TooCloseToMapEdge)
            {
                reason = "TooCloseToMapEdge";
                return Verdict.Refused;
            }

            if (cell.AlreadyDesignated)
            {
                return Verdict.Already;
            }

            if (cell.EdificeSmoothable)
            {
                return Verdict.DesignateWall;
            }

            if (cell.EdificeBlocksFloor || !cell.HasSmoothableStone)
            {
                reason = "MessageMustDesignateSmoothableSurface";
                return Verdict.Refused;
            }

            return Verdict.Designate;
        }

        /// <summary>
        /// RemoveFloor is Designator_RemoveFloor: a full impassable building on
        /// the cell and a building that blocks the removal are refusals without
        /// a sentence of their own, a terrain with nothing to remove carries
        /// "TerrainMustBeRemovable".
        /// </summary>
        public static Verdict RemoveFloor(RemoveFloorCell cell, out string reason)
        {
            reason = null;
            if (!cell.InBounds || cell.Fogged)
            {
                return Verdict.Unseen;
            }

            if (cell.AlreadyDesignated)
            {
                return Verdict.Already;
            }

            if (cell.EdificeBlocksFloor || cell.BuildingBlocksRemoval)
            {
                reason = "the cell holds a building that blocks floor removal";
                return Verdict.Refused;
            }

            if (!cell.TerrainRemovable)
            {
                reason = "TerrainMustBeRemovable";
                return Verdict.Refused;
            }

            return Verdict.Designate;
        }
    }
}
