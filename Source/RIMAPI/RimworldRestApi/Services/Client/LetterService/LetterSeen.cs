using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Verse;

namespace RIMAPI.Services
{
    // Which letters the bridge has already reported, and which the player has
    // opened. The game keeps no per-letter read flag: Verse.Letter declares
    // neither EverSeenByPlayer nor an opened field (probed on 1.6.4871; the
    // property the assembly has belongs to Quest), so a read that answered
    // "unread" would invent it (ticket #3156).
    //
    // The set holds the standing letters only: Report rebuilds it from the
    // stack it just returned, so a dismissed letter leaves nothing behind that
    // a fresh letter id could match.
    //
    // It survives a game restart on disk (ticket #3184): a restart happens on
    // every mod update, and an in-memory set called the whole standing stack
    // new each time. The file is keyed by the world's persistentRandomValue, so
    // a colony in another world (a new game, a save from elsewhere) starts with
    // an empty set instead of matching ids that belong to someone else.
    public static class LetterSeen
    {
        private static readonly object Gate = new object();
        private static readonly HashSet<int> Seen = new HashSet<int>();
        // NoWorld is "no colony is loaded", where nothing can be stored: the
        // key is a random int the world itself holds.
        private const int NoWorld = int.MinValue;
        // loadedWorld names the world Seen belongs to; NoWorld until a read or
        // a mark loads it.
        private static int loadedWorld = NoWorld;

        // Report names the letters this read has not reported before and marks
        // every standing letter seen. It returns the fresh ids, so the caller
        // answers "new" for exactly those.
        public static HashSet<int> Report(IEnumerable<int> standing)
        {
            var fresh = new HashSet<int>();
            lock (Gate)
            {
                EnsureLoaded();
                var now = new HashSet<int>();
                foreach (var id in standing)
                {
                    now.Add(id);
                    if (!Seen.Contains(id))
                        fresh.Add(id);
                }
                if (!now.SetEquals(Seen))
                {
                    Seen.Clear();
                    Seen.UnionWith(now);
                    Save();
                }
            }
            return fresh;
        }

        // Peek names the letters a read would report as new without marking
        // them seen. The digest and the wait look at the stack on every turn
        // and every poll, so a look that consumed the flags left the `letters`
        // read with nothing to mark and a run learned a count but never which
        // letters arrived (ticket #3214).
        public static HashSet<int> Peek(IEnumerable<int> standing)
        {
            var fresh = new HashSet<int>();
            lock (Gate)
            {
                EnsureLoaded();
                foreach (var id in standing)
                {
                    if (!Seen.Contains(id))
                        fresh.Add(id);
                }
            }
            return fresh;
        }

        // Mark records one letter as seen without a read: the game's own
        // opening counts, so a letter the player read in game is not new to the
        // next read.
        public static void Mark(int id)
        {
            lock (Gate)
            {
                EnsureLoaded();
                if (Seen.Add(id))
                    Save();
            }
        }

        // EnsureLoaded switches the set over to the world that is loaded now. A
        // different world starts empty even when the file holds ids: letter ids
        // come from the save's own counter and would otherwise mark a fresh
        // letter as already reported.
        private static void EnsureLoaded()
        {
            var world = WorldKey();
            if (world == loadedWorld)
                return;
            Seen.Clear();
            loadedWorld = world;
            if (world == NoWorld)
                return;
            foreach (var id in Read(world))
                Seen.Add(id);
        }

        // WorldKey is the loaded world's own random value: it is scribed with
        // the save, so it is the same after a restart and differs for a world
        // that was generated again.
        private static int WorldKey()
        {
            try
            {
                var world = Find.World;
                if (world == null || world.info == null)
                    return NoWorld;
                return world.info.persistentRandomValue;
            }
            catch (Exception)
            {
                return NoWorld;
            }
        }

        // Path of the store, under the game's save data folder
        // (RimWorld/RIMAPI on macOS) rather than inside the mod: the mod folder
        // is the installed bundle, and a build replaces it.
        private static string FilePath()
        {
            try
            {
                var folder = Path.Combine(GenFilePaths.SaveDataFolderPath, "RIMAPI");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "letter-seen.txt");
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static List<int> Read(int world)
        {
            var ids = new List<int>();
            var path = FilePath();
            if (path == null || !File.Exists(path))
                return ids;
            try
            {
                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.Trim();
                    if (line.Length == 0)
                        continue;
                    var parts = line.Split('=');
                    if (parts.Length != 2)
                        continue;
                    var name = parts[0].Trim();
                    var value = parts[1].Trim();
                    if (name == "world")
                    {
                        int key;
                        if (!int.TryParse(value, out key) || key != world)
                            return new List<int>();
                    }
                    else if (name == "seen")
                    {
                        foreach (var piece in value.Split(','))
                        {
                            int id;
                            if (int.TryParse(piece.Trim(), out id))
                                ids.Add(id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    $"[RimworldRestApi] could not read {path}: {ex.Message}; the standing letters read as new once");
                return new List<int>();
            }
            return ids;
        }

        // Save writes the set for the loaded world. A failed write costs one
        // "new" stack after the next restart, so it warns and carries on
        // instead of failing the read that triggered it.
        private static void Save()
        {
            var path = FilePath();
            if (path == null || loadedWorld == NoWorld)
                return;
            try
            {
                var text = "world=" + loadedWorld + "\nseen=" +
                           string.Join(",", Seen.OrderBy(i => i)) + "\n";
                var temp = path + ".tmp";
                // No BOM: the file is a two-line key/value store, and a leading
                // byte-order mark only confuses a reader that opens it by hand.
                File.WriteAllText(temp, text, new UTF8Encoding(false));
                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimworldRestApi] could not write {path}: {ex.Message}");
            }
        }
    }
}
