using System;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The dev boundary (fox ticket #4212). The mod is the interface to the
    /// game: it does what a player can do, and nothing a player cannot. A few
    /// routes are past that line, because they do what only the game's own dev
    /// tools do: spawn a pawn or an item, teleport one, edit his health, skills
    /// or traits, delete a rectangle, drop a pod, change the weather, force an
    /// incident, hand out faction goodwill, start a game with dev tools on.
    ///
    /// Those routes keep working, because building a test situation is how a
    /// check reaches a rare state. They are refused unless the running game has
    /// dev mode on, which is the game's own line: a debug slot turns it on in
    /// its own Prefs.xml, and the colony the fox plays honestly never has it.
    /// The dev routes proper live under /api/v1/dev/.
    ///
    /// The list is route paths rather than controller names because the path is
    /// what a caller writes and what the refusal echoes back. A prefix that
    /// ends in "/" covers everything under it; one that does not must match the
    /// path exactly, so /api/v1/pawn/spawn cannot swallow a later route that
    /// merely starts with those characters.
    ///
    /// The dev-mode flag is a parameter instead of a read of Prefs.DevMode, so
    /// this file needs no game type and the tests drive both answers. DevGate
    /// is the one line that reads the game.
    /// </summary>
    public static class DevOnlyRoutes
    {
        private static readonly string[] Prefixes =
        {
            // The dev namespace: every route that does what only the game's own
            // dev tools do lives under it (pawn edits, spawning, droppod,
            // rectangle delete and repair, weather, forced incidents, faction
            // goodwill, devquick, lords, forced social, an injected letter), next
            // to the dev action and the debug console.
            "/api/v1/dev/",
        };

        /// <summary>
        /// Whether the route at <paramref name="path"/> is one only dev mode
        /// may run. The path may carry a query string; it is ignored.
        /// </summary>
        public static bool IsDevOnly(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            int query = path.IndexOf('?');
            string route = query < 0 ? path : path.Substring(0, query);

            foreach (string prefix in Prefixes)
            {
                if (prefix.EndsWith("/", StringComparison.Ordinal))
                {
                    if (route.StartsWith(prefix, StringComparison.Ordinal))
                        return true;
                    continue;
                }
                if (string.Equals(route, prefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Null when the route may run now, otherwise the refusal to answer with.
        /// </summary>
        public static string Refuse(string path, bool devMode)
        {
            if (devMode || !IsDevOnly(path))
                return null;
            return Refusal(path);
        }

        /// <summary>
        /// The refusal a dev-only route answers while dev mode is off. It names
        /// the rule, the route and where dev work is allowed, so a reader who
        /// hit it knows which of the two games he is talking to.
        /// </summary>
        public static string Refusal(string path)
        {
            return "refused: " + path + " is a dev-mode action, and this game does not run in dev mode. "
                + "The mod does what a player can do and nothing more; the actions only the game's own dev "
                + "tools offer (spawn, teleport, edit a pawn, delete a rectangle, change the weather, force "
                + "an incident) run only in a game with dev mode on, which is the throwaway game a debug "
                + "start opens. The dev routes themselves live under /api/v1/dev/.";
        }
    }
}
