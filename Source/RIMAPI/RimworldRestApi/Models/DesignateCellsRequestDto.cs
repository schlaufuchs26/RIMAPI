using System.Collections.Generic;

namespace RIMAPI.Models
{
    // The same order as DesignateRequestDto, over the cells the caller names
    // instead of a rectangle (fox ticket #4095). A zone whose cells are not a
    // rectangle has a box that reaches into the cells beside it, so an order
    // aimed at an L-shaped stockpile needs the zone's own cells.
    public class DesignateCellsRequestDto
    {
        public int MapId { get; set; }
        public string Type { get; set; } // "Mine", "Deconstruct", "Harvest", "Hunt", "Chop", "Blight"
        public List<PositionDto> Cells { get; set; }
    }
}
