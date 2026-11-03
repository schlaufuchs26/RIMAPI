using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models.Map
{
    /// <summary>
    /// One rectangle of cells, corners inclusive, the shape an area's cells are
    /// reported in (fox ticket #3095). A caller can test a cell against a
    /// handful of rectangles instead of walking a grid.
    /// </summary>
    public class AreaRectDto
    {
        [ParameterDescription("The rectangle's first corner x, in map cells.")]
        public int X1 { get; set; }
        [ParameterDescription("The rectangle's first corner z, in map cells.")]
        public int Z1 { get; set; }
        [ParameterDescription("The rectangle's opposite corner x, in map cells.")]
        public int X2 { get; set; }
        [ParameterDescription("The rectangle's opposite corner z, in map cells.")]
        public int Z2 { get; set; }
    }

    /// <summary>
    /// One area and the cells it covers (fox ticket #3095). The Home area is
    /// what decides where colonists haul and therefore where a blueprint is
    /// ever built, and until this read existed the tool could neither see the
    /// area nor extend it: seven spike traps on plain soil at z 108-110 stood
    /// planned and unbuilt while walls inside the area went up.
    /// </summary>
    public class AreaCellsDto
    {
        [ParameterDescription("The area's numeric id, as the area list reports it.")]
        public int Id { get; set; }

        /// <summary>The area's label as the game shows it ("Home", "No roof").</summary>
        public string Label { get; set; }

        /// <summary>The game's type name ("Area_Home"), which survives a translated label.</summary>
        [ParameterDescription("The game's type name (\"Area_Home\"), which survives a translated label.")]
        public string Type { get; set; }

        [ParameterDescription("How many cells the area holds.")]
        public int CellsCount { get; set; }

        /// <summary>The smallest rectangle holding every cell, null on an empty area.</summary>
        [ParameterDescription("The smallest rectangle holding every cell, null on an empty area.")]
        public AreaRectDto Bounds { get; set; }

        /// <summary>Every cell as non-overlapping rectangles, row-major.</summary>
        [ParameterDescription("Every cell as non-overlapping rectangles, row-major.")]
        public List<AreaRectDto> Rects { get; set; }

        /// <summary>Cells the expand call turned on; 0 on a plain read.</summary>
        [ParameterDescription("Cells the expand call turned on; 0 on a plain read.")]
        public int Added { get; set; }

        /// <summary>Cells the shrink call turned off; 0 on a plain read.</summary>
        [ParameterDescription("Cells the shrink call turned off; 0 on a plain read.")]
        public int Removed { get; set; }

        /// <summary>Cells of the rectangle the area already held; 0 on a plain read.</summary>
        [ParameterDescription("Cells of the rectangle the area already held; 0 on a plain read.")]
        public int Already { get; set; }

        /// <summary>Cells of the rectangle outside the map; 0 on a plain read.</summary>
        [ParameterDescription("Cells of the rectangle outside the map; 0 on a plain read.")]
        public int Outside { get; set; }
    }

    /// <summary>
    /// Extend one area over a rectangle (fox ticket #3095). An area command is
    /// the game's own "expand area" designator, applied at once instead of by
    /// dragging over the map.
    /// </summary>
    public class AreaExpandRequestDto
    {
        public int MapId { get; set; }

        /// <summary>The area's label or type name; empty means Home.</summary>
        public string Label { get; set; }

        [ParameterDescription("One corner of the rectangle to add to the area.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the rectangle.")]
        public PositionDto PointB { get; set; }
    }
}
