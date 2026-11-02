using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The one place the dev boundary reads the game (fox ticket #4212): the
    /// router asks this before a controller runs, and the answer is the refusal
    /// to send or null. Prefs.DevMode is the game's own switch, the same one the
    /// debug dialog needs, so the boundary follows the line the player sees
    /// instead of a second list of our own.
    ///
    /// It sits in its own file so the tests can link it against a Prefs stub
    /// (Stubs/Verse.cs) and drive both answers; a file that only the game can
    /// compile proves nothing.
    /// </summary>
    public static class DevGate
    {
        /// <summary>Null when this route may run now, else the refusal.</summary>
        public static string Refuse(string path)
        {
            return DevOnlyRoutes.Refuse(path, Prefs.DevMode);
        }
    }
}
