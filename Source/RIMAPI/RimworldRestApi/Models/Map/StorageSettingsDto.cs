using System.Collections.Generic;

namespace RIMAPI.Models
{
    /// <summary>
    /// The body of a container's storage write (fox ticket #3888). It is the
    /// stockpile update's shape with a thing id instead of a zone id: a shelf
    /// or a hopper keeps its priority and filter in the same StorageSettings a
    /// stockpile does, and the game's storage tab writes both.
    ///
    /// A call with no priority and no name only reads the container back, so
    /// one route answers "what does this hopper take" and "make it take this".
    /// </summary>
    public class UpdateStorageRequestDto
    {
        public int MapId { get; set; }
        public int ThingId { get; set; }
        public int? Priority { get; set; }
        public List<string> AddItemDefs { get; set; }
        public List<string> RemoveItemDefs { get; set; }
        public List<string> AddItemCategories { get; set; }
        public List<string> RemoveItemCategories { get; set; }
    }

    /// <summary>
    /// What the container holds afterwards, in the same terms the `zones` read
    /// prints a stockpile's filter, plus the names the write applied and the
    /// ones the def database does not know.
    /// </summary>
    public class StorageSettingsDto
    {
        public int ThingId { get; set; }
        public string Def { get; set; }
        public string Label { get; set; }
        public int Priority { get; set; }
        public string PriorityName { get; set; }
        public StockpileFilterDto Filter { get; set; }
        public List<string> AppliedDefs { get; set; }
        public List<string> AppliedCategories { get; set; }
        public List<string> Unknown { get; set; }
    }
}
