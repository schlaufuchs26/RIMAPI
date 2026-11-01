namespace RIMAPI.Helpers
{
    // The game tick the running colony was loaded at (fox ticket #3781). Every
    // figure the power read sums is written by the game's tick, and the tick
    // the loader itself runs still reads 0 W produced of 1700 W installed: the
    // map's sky and weather are not up yet at that moment. One `wait` later the
    // same colony read 1700 W. So the load's own tick is what a read compares
    // against, and a colony that has run almost nothing since it is not
    // measured.
    //
    // This file stays free of game types so the test project can compile it.
    public static class LoadedTick
    {
        private static readonly object sync = new object();
        private static int tick = -1;

        public static void Record(int gameTick)
        {
            lock (sync)
            {
                tick = gameTick;
            }
        }

        // Since is how many ticks the colony has run since it loaded, -1 when
        // no load was recorded or the clock reads before it.
        public static int Since(int gameTick)
        {
            lock (sync)
            {
                if (tick < 0 || gameTick < tick)
                    return -1;
                return gameTick - tick;
            }
        }

        public static void Clear()
        {
            lock (sync)
            {
                tick = -1;
            }
        }
    }
}
