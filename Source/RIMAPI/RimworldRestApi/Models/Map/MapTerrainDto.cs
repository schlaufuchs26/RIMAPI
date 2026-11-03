using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class MapTerrainDto
    {
        // The rectangle the grid covers: x1/z1 are its top-left cell, so a
        // caller that asked for a part of the map still knows where the grid
        // sits (fox ticket #3381). A whole-map answer is 0/0.
        [ParameterDescription("The rectangle the grid covers: x1/z1 are its top-left cell, so a caller that asked for a part of the map still knows where the grid sits (fox ticket #3381).")]
        public int X1 { get; set; }
        [ParameterDescription("The grid's first corner z, in map cells.")]
        public int Z1 { get; set; }
        [ParameterDescription("How many cells wide the grid is.")]
        public int Width { get; set; }
        [ParameterDescription("How many cells tall the grid is.")]
        public int Height { get; set; }
        [ParameterDescription("The terrain def names, one per palette letter.")]
        public List<string> Palette { get; set; }  // Natural terrain types (soil, sand, stone)
        [ParameterDescription("One palette letter per cell, row by row.")]
        public List<int> Grid { get; set; }  // RLE compressed grid for terrain
        [ParameterDescription("The floor def names, one per palette letter.")]
        public List<string> FloorPalette { get; set; }  // Constructed floor types (wood, concrete, etc.)
        [ParameterDescription("One floor letter per cell, row by row.")]
        public List<int> FloorGrid { get; set; }  // RLE compressed grid for floors
    }
}
