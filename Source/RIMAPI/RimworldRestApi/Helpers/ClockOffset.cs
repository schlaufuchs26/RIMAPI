namespace RIMAPI.Helpers
{
    /// <summary>
    /// The offset between the raw game tick count and the clock the HUD shows
    /// on one map (fox ticket #3412). The game does not read ticks/2500: it
    /// adds TickManager.gameStartAbsTick, which puts the founding morning at
    /// 06:00, and then GenDate adds the map tile's whole-hour time zone. The
    /// second half is per map, so a second settlement on another longitude
    /// reads hours off from the founding map's constant.
    ///
    /// The arithmetic is kept free of game types so a test can state it; the
    /// caller passes the two game values it reads.
    /// </summary>
    public static class ClockOffset
    {
        /// <summary>One in-game hour: GenDate.TicksPerHour.</summary>
        public const int TicksPerHour = 2500;

        /// <summary>One in-game day: GenDate.TicksPerDay.</summary>
        public const int TicksPerDay = 60000;

        /// <summary>
        /// The clock's shift in ticks, 0..TicksPerDay-1. gameStartAbsTick is
        /// TickManager.gameStartAbsTick; timeZoneHours is
        /// GenDate.TimeZoneAt of the map tile's longitude, the same whole-hour
        /// value GenDate.LocalTicksOffsetFromLongitude multiplies by an hour.
        /// A map west of the founding one has a smaller offset, and the day
        /// turns over there that much earlier.
        /// </summary>
        public static int From(int gameStartAbsTick, int timeZoneHours)
        {
            long offset = (long)gameStartAbsTick + (long)timeZoneHours * TicksPerHour;
            offset %= TicksPerDay;
            if (offset < 0)
            {
                offset += TicksPerDay;
            }
            return (int)offset;
        }
    }
}
