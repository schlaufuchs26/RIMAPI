using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// The answer to "how much room has the map's storage for this product"
    /// (fox ticket #3648): the storages whose filter allows it, their cells,
    /// and how many of those cells the game would still fill. It is what a
    /// farm read needs when ripe plants stand unpicked and the store is full:
    /// every other read names the plants and never the room.
    ///
    /// The def asked for may be a plant, so PlantDef names the question and
    /// ThingDef the product the store would take (Plant_Rice asks for RawRice).
    /// </summary>
    public class StorageCapacityDto
    {
        public string DefName { get; set; }
        [ParameterDescription("The plant the capacity was measured for.")]
        public string PlantDef { get; set; }
        [ParameterDescription("The thing the capacity was measured for.")]
        public string ThingDef { get; set; }
        [ParameterDescription("How many storages the check counted.")]
        public int Storages { get; set; }
        [ParameterDescription("How many cells those storages hold.")]
        public int Cells { get; set; }
        [ParameterDescription("How many of them stand empty.")]
        public int FreeCells { get; set; }
    }
}
