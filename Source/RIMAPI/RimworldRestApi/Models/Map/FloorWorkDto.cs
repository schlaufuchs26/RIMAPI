using System.Collections.Generic;
using RIMAPI.Core;

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
        [ParameterDescription("The floor order: smooth or remove-floor.")]
        public string Type { get; set; }

        [ParameterDescription("One corner of the rectangle to work on.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the rectangle.")]
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
        [ParameterDescription("The floor order that ran.")]
        public string Type { get; set; }
        [ParameterDescription("How many cells the rectangle holds.")]
        public int Cells { get; set; }
        [ParameterDescription("How many cells the call marked.")]
        public int Designated { get; set; }
        [ParameterDescription("How many cells were marked before the call.")]
        public int Already { get; set; }
        [ParameterDescription("How many cells the fog still hides.")]
        public int Unseen { get; set; }
        [ParameterDescription("How many cells the game refused.")]
        public int Refused { get; set; }
        [ParameterDescription("How many smoothable floors the rectangle holds.")]
        public int SmoothFloor { get; set; }
        [ParameterDescription("How many smoothable walls it holds.")]
        public int SmoothWall { get; set; }
        [ParameterDescription("The refused cells with the game's reason.")]
        public List<FloorRefusalDto> Refusals { get; set; }

        /// <summary>True when the refusals list is a sample of the refused count.</summary>
        [ParameterDescription("True when the refusals list is a sample of the refused count.")]
        public bool RefusalsTruncated { get; set; }
    }

    public class FloorRefusalDto
    {
        public int X { get; set; }
        public int Z { get; set; }
        [ParameterDescription("The game's own reason for the refusal.")]
        public string Reason { get; set; }
    }
}
