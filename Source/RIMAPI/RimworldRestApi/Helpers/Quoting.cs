namespace RIMAPI.Helpers
{
    /// <summary>
    /// A value wrapped in double quotes for a message a player reads (fox
    /// ticket #3446). QuestService and LetterService each carried a private
    /// Quote with the same body, the same drift risk #3398 removed for
    /// GetMapTileId: an escape fix would have landed in one copy only.
    /// </summary>
    public static class Quoting
    {
        /// <summary>The value between quotes; null reads as the empty string.</summary>
        public static string Quote(string s)
        {
            return "\"" + (s ?? "") + "\"";
        }
    }
}
