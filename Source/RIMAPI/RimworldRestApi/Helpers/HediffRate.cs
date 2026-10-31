namespace RIMAPI.Helpers
{
    /// <summary>
    /// Which daily severity rate a hediff def states (fox, #3449). The caller
    /// hands in the two rates the def may carry, so this file touches no game
    /// type and the unit tests cover it against Source/RIMAPI.Tests.
    /// </summary>
    public static class HediffRate
    {
        /// <summary>
        /// The immunizable infection's severityPerDayNotImmune wins over a
        /// plain SeverityPerDay rate: an infection climbs on the not-immune
        /// rate until immunity overtakes it, and that is the clock a caller
        /// projecting a death needs. A non-positive immunizable figure means
        /// the def states none, so the plain rate applies. 0 means neither
        /// comp gives a rate, and the caller must not invent one.
        /// </summary>
        public static float Pick(float immunizableNotImmune, float severityPerDay)
        {
            if (immunizableNotImmune > 0f) return immunizableNotImmune;
            return severityPerDay > 0f ? severityPerDay : 0f;
        }
    }
}
