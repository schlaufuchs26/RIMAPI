namespace RIMAPI.Helpers
{
    // Which save file a colony writes into (fox ticket #3603). The save
    // handler takes a name from the caller, but the game only honours it in
    // the ordinary mode: a commitment-mode colony writes its own unique name.
    // A caller that always asked for one fixed name therefore wrote whatever
    // colony was loaded into that file, and a probe copy of the live colony
    // landed in the live colony's save.
    //
    // The game read lives in GamePlayHelper; this file stays free of game
    // types so the test project can compile it (Source/RIMAPI.Tests).
    public static class SaveNameRules
    {
        public const string Suffix = ".rws";

        // Normalize spells a save name the way the save folder does: no
        // folder and no .rws suffix. A load call carries the bare name, but
        // the game's own load path receives the file name it built.
        public static string Normalize(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "";
            string name = fileName.Trim();
            int slash = name.LastIndexOfAny(new[] { '/', '\\' });
            if (slash >= 0) name = name.Substring(slash + 1);
            if (name.EndsWith(Suffix, System.StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - Suffix.Length);
            return name;
        }

        // WrittenName is the file the game's next save writes: the colony's own
        // unique name in commitment mode, whatever the caller asked for.
        // Otherwise the caller picks the name, so the answer is empty and the
        // caller's own choice stands.
        public static string WrittenName(bool permadeathMode, string uniqueName)
        {
            if (!permadeathMode) return "";
            return Normalize(uniqueName);
        }
    }
}
