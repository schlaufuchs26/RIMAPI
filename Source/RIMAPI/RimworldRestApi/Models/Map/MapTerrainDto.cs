using System.Collections.Generic;

namespace RIMAPI.Models
{
    public class MapTerrainDto
    {
        // The rectangle the grid covers: x1/z1 are its top-left cell, so a
        // caller that asked for a part of the map still knows where the grid
        // sits (fox ticket #3381). A whole-map answer is 0/0.
        public int X1 { get; set; }
        public int Z1 { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<string> Palette { get; set; }  // Natural terrain types (soil, sand, stone)
        public List<int> Grid { get; set; }  // RLE compressed grid for terrain
        public List<string> FloorPalette { get; set; }  // Constructed floor types (wood, concrete, etc.)
        public List<int> FloorGrid { get; set; }  // RLE compressed grid for floors
    }
}
