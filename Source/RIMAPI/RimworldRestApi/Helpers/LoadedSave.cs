namespace RIMAPI.Helpers
{
    // The save file the running colony was read from (fox ticket #3603). The
    // game keeps that name only in commitment mode, so the load hook records
    // what it was given; a new colony and the title screen clear it again.
    // The caller writes a release back into this file, which is what keeps a
    // probe copy out of the live colony's save.
    //
    // This file stays free of game types so the test project can compile it.
    public static class LoadedSave
    {
        private static string current = "";

        // Current is the bare save name (no folder, no .rws), empty when the
        // running colony was not read from a file: a new colony, the title
        // screen, or a load this build did not see.
        public static string Current => current;

        public static void Set(string fileName)
        {
            current = SaveNameRules.Normalize(fileName);
        }

        public static void Clear()
        {
            current = "";
        }
    }
}
