using System.Collections.Generic;

namespace RIMAPI.Services
{
    // Which letters the bridge has already reported, and which the player has
    // opened. The game keeps no per-letter read flag: Verse.Letter declares
    // neither EverSeenByPlayer nor an opened field (probed on 1.6.4871; the
    // property the assembly has belongs to Quest), so a read that answered
    // "unread" would invent it (ticket #3156).
    //
    // The set holds the standing letters only: Report rebuilds it from the
    // stack it just returned, so a dismissed letter, a loaded save and a new
    // colony leave nothing behind that a fresh letter id could match.
    public static class LetterSeen
    {
        private static readonly object Gate = new object();
        private static readonly HashSet<int> Seen = new HashSet<int>();

        // Report names the letters this read has not reported before and marks
        // every standing letter seen. It returns the fresh ids, so the caller
        // answers "new" for exactly those.
        public static HashSet<int> Report(IEnumerable<int> standing)
        {
            var fresh = new HashSet<int>();
            var now = new HashSet<int>();
            lock (Gate)
            {
                foreach (var id in standing)
                {
                    now.Add(id);
                    if (!Seen.Contains(id))
                        fresh.Add(id);
                }
                Seen.Clear();
                Seen.UnionWith(now);
            }
            return fresh;
        }

        // Mark records one letter as seen without a read: the game's own
        // opening counts, so a letter the player read in game is not new to the
        // next read.
        public static void Mark(int id)
        {
            lock (Gate)
                Seen.Add(id);
        }
    }
}
