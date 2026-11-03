using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// Roof one rectangle (fox ticket #3418). A sealed room stays at the
    /// outside temperature until somebody builds the roof: the game's build
    /// roof area only marks the cells, a constructor does the work, and a play
    /// run that painted the area read "20 open roofs" for a day without a
    /// lever. The order marks the area and queues the game's own build-roof
    /// job, so the work starts instead of waiting for the work giver to get to
    /// it.
    /// </summary>
    public class RoofRequestDto
    {
        public int MapId { get; set; }

        [ParameterDescription("One corner of the rectangle to roof.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the rectangle.")]
        public PositionDto PointB { get; set; }

        /// <summary>The constructor who takes the job; 0 lets the mod pick.</summary>
        public int PawnId { get; set; }
    }

    /// <summary>One queued build-roof job: who builds the block around which cell.</summary>
    public class RoofJobDto
    {
        public int PawnId { get; set; }
        [ParameterDescription("The pawn the call put on the roof job.")]
        public string Pawn { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
    }

    /// <summary>
    /// A room the rectangle touches, as it reads before the work: the numbers a
    /// caller compares the next `rooms` read against.
    /// </summary>
    public class RoofRoomDto
    {
        [ParameterDescription("The room's numeric id.")]
        public int RoomId { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How many cells the room holds.")]
        public int Cells { get; set; }
        [ParameterDescription("How many of them lack a roof.")]
        public int OpenRoofs { get; set; }
        [ParameterDescription("The room's temperature, in Celsius.")]
        public float TemperatureC { get; set; }
    }

    /// <summary>
    /// What the order did, counted over the rectangle's cells: cells already
    /// roofed, cells a roof can be built on, cells no roof holder reaches
    /// (a roof there would collapse), cells a plant blocks, and cells off the
    /// map. Jobs names the pawns that started, Refusal why none did.
    /// </summary>
    public class RoofResultDto
    {
        [ParameterDescription("How many cells the rectangle holds.")]
        public int Cells { get; set; }
        [ParameterDescription("How many cells already carry a roof.")]
        public int AlreadyRoofed { get; set; }
        [ParameterDescription("How many cells the call marked for roof work.")]
        public int RoofWork { get; set; }
        [ParameterDescription("How many cells nothing can carry a roof on.")]
        public int Unsupported { get; set; }
        [ParameterDescription("How many cells something blocks.")]
        public int Blocked { get; set; }
        [ParameterDescription("How many cells lie outside the home area.")]
        public int Outside { get; set; }

        /// <summary>Build roof area cells this call turned on.</summary>
        [ParameterDescription("Build roof area cells this call turned on.")]
        public int AreaAdded { get; set; }

        /// <summary>Build roof area cells after the call.</summary>
        [ParameterDescription("Build roof area cells after the call.")]
        public int AreaCells { get; set; }

        [ParameterDescription("The pawns the call put on the work.")]
        public List<RoofJobDto> Jobs { get; set; }
        [ParameterDescription("The rooms the rectangle touches, with their temperatures.")]
        public List<RoofRoomDto> Rooms { get; set; }

        /// <summary>Why no job was queued, in the game's own terms; null when one was.</summary>
        [ParameterDescription("Why no job was queued, in the game's own terms; null when one was.")]
        public string Refusal { get; set; }
    }
}
