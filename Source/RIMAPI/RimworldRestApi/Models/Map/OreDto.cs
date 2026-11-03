using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models.Map
{
    public class OreDataDto
    {
        [ParameterDescription("How many cells wide the map is.")]
        public int MapWidth { get; set; }

        // Key = DefName (e.g., "MineableSteel", "Granite")
        [ParameterDescription("Key = DefName (e.g., \"MineableSteel\", \"Granite\")")]
        public Dictionary<string, OreGroupDto> Ores { get; set; }
    }

    public class OreGroupDto
    {
        [ParameterDescription("The hit points of an untouched ore cell.")]
        public int MaxHp { get; set; } // Send MaxHP once per type

        // Flattened Map Indices: (z * MapWidth) + x
        [ParameterDescription("Flattened Map Indices: (z * MapWidth) + x")]
        public List<int> Cells { get; set; }

        // HP values matching the order of Cells
        [ParameterDescription("HP values matching the order of Cells")]
        public List<int> Hp { get; set; }
    }
}
