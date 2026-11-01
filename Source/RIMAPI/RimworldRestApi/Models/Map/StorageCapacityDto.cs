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
        public string PlantDef { get; set; }
        public string ThingDef { get; set; }
        public int Storages { get; set; }
        public int Cells { get; set; }
        public int FreeCells { get; set; }
    }
}
