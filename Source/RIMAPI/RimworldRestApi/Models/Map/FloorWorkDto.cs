using System.Collections.Generic;

namespace RIMAPI.Models.Map
{
    /// <summary>
    /// A floor order over a rectangle (fox ticket #4316, parity wave 7): the
    /// Architect tab's Smooth and Remove floor designators, dragged at once.
    /// </summary>
    public class FloorOrderRequestDto
    {
        public int MapId { get; set; }

        /// <summary>"smooth" or "remove-floor".</summary>
        public string Type { get; set; }

        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }
    }

    /// <summary>
    /// What a floor order did with its rectangle: how many cells it marked, how
    /// many already carried the order, how many the player could not see, and
    /// the refusals with the cell that caused each. Smoothing a wall and a floor
    /// are two different designations, so the answer counts them apart.
    /// </summary>
    public class FloorWorkResultDto
    {
        public int MapId { get; set; }
        public string Type { get; set; }
        public int Cells { get; set; }
        public int Designated { get; set; }
        public int Already { get; set; }
        public int Unseen { get; set; }
        public int Refused { get; set; }
        public int SmoothFloor { get; set; }
        public int SmoothWall { get; set; }
        public List<FloorRefusalDto> Refusals { get; set; }

        /// <summary>True when the refusals list is a sample of the refused count.</summary>
        public bool RefusalsTruncated { get; set; }
    }

    public class FloorRefusalDto
    {
        public int X { get; set; }
        public int Z { get; set; }
        public string Reason { get; set; }
    }
}
