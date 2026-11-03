using System.Collections.Generic;
using RIMAPI.Core;

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
        [ParameterDescription("The storage's priority, 1 low to 5 critical.")]
        public int? Priority { get; set; }
        [ParameterDescription("The item defs to allow.")]
        public List<string> AddItemDefs { get; set; }
        [ParameterDescription("The item defs to stop allowing.")]
        public List<string> RemoveItemDefs { get; set; }
        [ParameterDescription("The item categories to allow in the filter.")]
        public List<string> AddItemCategories { get; set; }
        [ParameterDescription("The categories to stop allowing.")]
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
        [ParameterDescription("The storage's def name.")]
        public string Def { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The storage's priority, 1 low to 5 critical.")]
        public int Priority { get; set; }
        [ParameterDescription("The priority, as the game names it.")]
        public string PriorityName { get; set; }
        [ParameterDescription("The filter as the storage holds it.")]
        public StockpileFilterDto Filter { get; set; }
        [ParameterDescription("The item defs the filter now allows.")]
        public List<string> AppliedDefs { get; set; }
        [ParameterDescription("The categories the filter now allows.")]
        public List<string> AppliedCategories { get; set; }
        [ParameterDescription("The names the call did not recognise.")]
        public List<string> Unknown { get; set; }
    }
}
