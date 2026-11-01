namespace RIMAPI.Helpers
{
    /// <summary>
    /// The two answers the storage-capacity read gives for input it cannot
    /// answer (fox ticket #3648). They name the input, because a caller that
    /// guessed the def has to see what it asked for; a capacity of zero would
    /// read like a full store, which is the state the read exists to report.
    ///
    /// Plain strings, so the wording is checked without a game.
    /// </summary>
    public static class StorageCapacityText
    {
        /// <summary>
        /// The answer for a def name no def carries.
        /// </summary>
        public static string UnknownDefMessage(string defName) =>
            $"No def named {defName}: ask with a thing def (RawRice) or the plant it comes from (Plant_Rice)";

        /// <summary>
        /// The answer for a def that is no storable item: a wall or a floor
        /// has no storage room.
        /// </summary>
        public static string NotStorableMessage(string defName) =>
            $"{defName} is not a storable item, so no storage takes it";
    }
}
