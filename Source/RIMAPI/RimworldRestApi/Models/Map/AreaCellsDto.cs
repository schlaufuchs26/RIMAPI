using System.Collections.Generic;

namespace RIMAPI.Models.Map
{
    /// <summary>
    /// One rectangle of cells, corners inclusive, the shape an area's cells are
    /// reported in (fox ticket #3095). A caller can test a cell against a
    /// handful of rectangles instead of walking a grid.
    /// </summary>
    public class AreaRectDto
    {
        public int X1 { get; set; }
        public int Z1 { get; set; }
        public int X2 { get; set; }
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
        public int Id { get; set; }

        /// <summary>The area's label as the game shows it ("Home", "No roof").</summary>
        public string Label { get; set; }

        /// <summary>The game's type name ("Area_Home"), which survives a translated label.</summary>
        public string Type { get; set; }

        public int CellsCount { get; set; }

        /// <summary>The smallest rectangle holding every cell, null on an empty area.</summary>
        public AreaRectDto Bounds { get; set; }

        /// <summary>Every cell as non-overlapping rectangles, row-major.</summary>
        public List<AreaRectDto> Rects { get; set; }

        /// <summary>Cells the expand call turned on; 0 on a plain read.</summary>
        public int Added { get; set; }

        /// <summary>Cells of the rectangle the area already held; 0 on a plain read.</summary>
        public int Already { get; set; }

        /// <summary>Cells of the rectangle outside the map; 0 on a plain read.</summary>
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

        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }
    }
}
