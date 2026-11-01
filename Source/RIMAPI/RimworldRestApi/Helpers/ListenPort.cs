namespace RIMAPI.Helpers
{
    /// <summary>
    /// The bridge's listen port when the game was started with
    /// <c>-rimapi-port=N</c> (fox ticket #3580).
    /// <para>A second game runs from its own save-data folder, but the mod's
    /// settings file is one per save-data folder, so two instances would
    /// otherwise want the same port and the second would fail to bind. The
    /// argument is what lets a debug instance listen beside the colony that is
    /// playing, without editing the settings the real game uses.</para>
    /// </summary>
    public static class ListenPort
    {
        /// <summary>
        /// Parses the argument's value: a whole number from 1 to 65535.
        /// </summary>
        public static bool TryParse(string raw, out int port)
        {
            return int.TryParse(raw, out port) && port >= 1 && port <= 65535;
        }

        /// <summary>
        /// The port the settings should use: the argument's when it carries a
        /// usable port, the saved value otherwise. An unusable argument is
        /// refused rather than silently ignored, so a mistyped debug start
        /// says so in the log instead of binding the real colony's port.
        /// </summary>
        public static int Override(string raw, int saved, out bool refused)
        {
            refused = false;
            if (string.IsNullOrEmpty(raw))
            {
                return saved;
            }
            if (!TryParse(raw, out int port))
            {
                refused = true;
                return saved;
            }
            return port;
        }
    }
}
