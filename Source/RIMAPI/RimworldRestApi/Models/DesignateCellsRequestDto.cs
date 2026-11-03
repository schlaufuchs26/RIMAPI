using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // The same order as DesignateRequestDto, over the cells the caller names
    // instead of a rectangle (fox ticket #4095). A zone whose cells are not a
    // rectangle has a box that reaches into the cells beside it, so an order
    // aimed at an L-shaped stockpile needs the zone's own cells.
    public class DesignateCellsRequestDto
    {
        public int MapId { get; set; }
        [ParameterDescription("The designation to place, e.g. mine or haul.")]
        public string Type { get; set; } // "Mine", "Deconstruct", "Harvest", "Hunt", "Chop", "Blight"
        [ParameterDescription("The cells to mark, as x and z pairs.")]
        public List<PositionDto> Cells { get; set; }
    }
}
