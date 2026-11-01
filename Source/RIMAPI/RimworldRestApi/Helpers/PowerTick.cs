using System.Collections.Generic;

namespace RIMAPI.Helpers
{
    // When a map's power grid last ticked (fox ticket #3781). Every figure the
    // power read sums describes the last tick: CurrentPower comes from the
    // plants' PowerOutput and ConsumptionPowerOn from the traders' PowerOn, both
    // written while the nets tick. A colony read straight from a save has not
    // ticked at all, so 1700 W installed read as 0 W produced and every machine
    // read as dead; one `wait` later the same colony read 1700 W.
    //
    // The Harmony hook records the game tick of each power update; the read
    // answers -1 until then, which is what tells a stale grid from a dark one.
    //
    // This file stays free of game types so the test project can compile it.
    public static class PowerTick
    {
        private static readonly object sync = new object();
        private static readonly Dictionary<int, int> lastTickByMap = new Dictionary<int, int>();

        public static void Record(int mapId, int tick)
        {
            lock (sync)
            {
                lastTickByMap[mapId] = tick;
            }
        }

        // Last is the game tick of the map's last power update, -1 while the
        // grid has not updated since the map loaded.
        public static int Last(int mapId)
        {
            lock (sync)
            {
                int tick;
                return lastTickByMap.TryGetValue(mapId, out tick) ? tick : -1;
            }
        }

        // Clear forgets every map. A loaded colony keeps its map ids (they come
        // from the save), so a record from the game that ran before would claim
        // a tick this load never had.
        public static void Clear()
        {
            lock (sync)
            {
                lastTickByMap.Clear();
            }
        }
    }
}
