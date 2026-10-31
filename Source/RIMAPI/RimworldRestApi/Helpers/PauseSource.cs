namespace RIMAPI.Helpers
{
    // Why the game's clock stands still (fox ticket #3490). TickManager.Paused
    // is one boolean over several causes, so a caller that finds the game
    // paused could only guess "an alert, a modal or a save". The game knows:
    // Paused is true when its own speed switch is Paused, or when
    // TickManager.ForcePaused holds, and ForcePaused is one expression over a
    // handful of named holds. This names the hold that stopped the colony.
    //
    // The game read lives in GamePlayHelper; this file stays free of game
    // types so the test project can compile it (Source/RIMAPI.Tests).
    public static class PauseSource
    {
        public const string LongEvent = "long_event";   // a save, a load, world generation
        public const string Window = "window";          // an open window with forcePause
        public const string TilePicker = "tile_picker"; // picking a tile on the world map
        public const string Debug = "debug";            // the map generator's debug mode
        public const string Other = "other";            // ForcePaused by a hold this build cannot name

        // Describe names the first hold that applies. A long event comes first
        // because it is the one that ends by itself; forcePaused is the game's
        // own catch-all, so a hold this build does not name still says that
        // something of the game's holds the clock instead of nothing at all.
        public static string Describe(bool longEvent, bool window, bool tilePicker, bool debug, bool forcePaused)
        {
            if (longEvent) return LongEvent;
            if (window) return Window;
            if (tilePicker) return TilePicker;
            if (debug) return Debug;
            if (forcePaused) return Other;
            return "";
        }
    }
}
