using System.Collections.Generic;

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

        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }

        /// <summary>The constructor who takes the job; 0 lets the mod pick.</summary>
        public int PawnId { get; set; }
    }

    /// <summary>One queued build-roof job: who builds the block around which cell.</summary>
    public class RoofJobDto
    {
        public int PawnId { get; set; }
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
        public int RoomId { get; set; }
        public string Label { get; set; }
        public int Cells { get; set; }
        public int OpenRoofs { get; set; }
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
        public int Cells { get; set; }
        public int AlreadyRoofed { get; set; }
        public int RoofWork { get; set; }
        public int Unsupported { get; set; }
        public int Blocked { get; set; }
        public int Outside { get; set; }

        /// <summary>Build roof area cells this call turned on.</summary>
        public int AreaAdded { get; set; }

        /// <summary>Build roof area cells after the call.</summary>
        public int AreaCells { get; set; }

        public List<RoofJobDto> Jobs { get; set; }
        public List<RoofRoomDto> Rooms { get; set; }

        /// <summary>Why no job was queued, in the game's own terms; null when one was.</summary>
        public string Refusal { get; set; }
    }
}
