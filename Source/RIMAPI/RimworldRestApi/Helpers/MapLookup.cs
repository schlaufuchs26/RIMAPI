using System;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Why a map route has no map to report (fox ticket #3658). The routes
    /// resolved the id and handed the null straight to a helper: the helper
    /// either threw inside the mod, which reached the caller as a 500, or
    /// caught the throw and answered success with no data. A wrong map id read
    /// the same as an empty map. The sentence is the growing-zone read's
    /// (ticket #3613) and the create routes', so every route names the failure
    /// the same way.
    ///
    /// This file stays free of game types so the test project can compile it:
    /// the resolution takes the game's own finder as a function.
    /// </summary>
    public static class MapLookup
    {
        public static string MapNotFound(int mapId) => $"Map not found: {mapId}";

        /// <summary>
        /// The map a route asked for, or null with the reason in
        /// <paramref name="error"/>. <paramref name="find"/> is
        /// MapHelper.GetMapByID in the routes and a fake in the tests.
        /// </summary>
        public static T Resolve<T>(int mapId, Func<int, T> find, out string error)
            where T : class
        {
            var map = find(mapId);
            error = map == null ? MapNotFound(mapId) : null;
            return map;
        }
    }
}
