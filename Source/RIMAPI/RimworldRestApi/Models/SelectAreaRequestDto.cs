using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class SelectAreaRequestDto
    {
        [ParameterDescription("One corner of the area to select.")]
        public PositionDto PositionA { get; set; }
        [ParameterDescription("The opposite corner of the area.")]
        public PositionDto PositionB { get; set; }
    }
}
