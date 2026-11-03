using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class DesignateRequestDto
    {
        public int MapId { get; set; }
        [ParameterDescription("The designation to place, e.g. mine or haul.")]
        public string Type { get; set; } // "Mine", "Deconstruct", "Harvest", "Hunt", "Chop", "Blight"
        [ParameterDescription("One corner of the area to mark.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the area.")]
        public PositionDto PointB { get; set; }
    }
}
