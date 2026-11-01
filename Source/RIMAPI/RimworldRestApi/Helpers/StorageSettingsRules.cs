namespace RIMAPI.Helpers
{
    /// <summary>
    /// The request shape behind a container's storage write (fox ticket #3888):
    /// which priority the game's scale holds and what a thing that is no
    /// container answers. A shelf, a hopper and a grave carry the same storage
    /// tab a stockpile does, so their priority and filter are writable through
    /// the same names; before this the tool could only read them, and a hopper
    /// competed with a Critical stockpile for the colony's potatoes.
    ///
    /// Plain C#, so the wording is tested without the game.
    /// </summary>
    public static class StorageSettingsRules
    {
        // The game's own scale: StoragePriority.Unstored through Critical.
        public const int MinPriority = 0;
        public const int MaxPriority = 5;

        public static bool PriorityInRange(int priority)
        {
            return priority >= MinPriority && priority <= MaxPriority;
        }

        public static string PriorityOutOfRange(int priority)
        {
            return $"Priority {priority} is out of range: 0 unstored, 1 low, 2 normal, 3 preferred, 4 important, 5 critical.";
        }

        // NotAStorage answers an id that names a thing without a storage tab:
        // a wall, a stove, a stack of rice. The sentence names where a
        // container's id comes from, because that is what the next call needs.
        public static string NotAStorage(string defName, string label)
        {
            return $"{defName} ({label}) has no storage settings: a shelf, a hopper, a grave and every other container take a priority and a filter, and the `things` read prints a container's id next to it";
        }

        public static string NoSuchThing(int thingId)
        {
            return $"No thing with id {thingId} on this map: the `things` read prints the ids it lists";
        }
    }
}
