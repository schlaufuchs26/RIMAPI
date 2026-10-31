using System.Globalization;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Whether a bed is a fit destination for a carried patient (fox ticket
    /// #3383). The game's own rescue lookup takes the first free bed it finds,
    /// and in colony 10 that was the one inside a room at 111 C: the patient was
    /// put back into the burning room and got heatstroke while a free bed stood
    /// 13 cells away on open ground. The rule is one number and one flag, kept
    /// free of game types so a test can state it.
    /// </summary>
    public static class BedSafety
    {
        /// <summary>
        /// The room temperature at which a bed is no destination for a carried
        /// patient; the same threshold the fox's digest uses for a hot room
        /// (core/rimworld/digest.go, HotRoomC).
        /// </summary>
        public const float TooHotC = 50f;

        /// <summary>
        /// Why the bed is no destination, or null when it is one. A fire in the
        /// room counts whatever the thermometer reads: it spreads, and a patient
        /// who cannot walk cannot leave it.
        /// </summary>
        public static string UnsafeReason(float temperatureC, bool roomHasFire)
        {
            if (roomHasFire) return "a fire burns in its room";
            if (temperatureC >= TooHotC)
            {
                // Invariant, because the number reaches a Go answer that prints
                // its own decimals; a German locale would send "111,9 C".
                return "its room reads "
                    + temperatureC.ToString("F1", CultureInfo.InvariantCulture)
                    + " C, above the " + TooHotC.ToString("F0", CultureInfo.InvariantCulture)
                    + " C a carried patient cannot take";
            }
            return null;
        }

        public static bool IsUnsafe(float temperatureC, bool roomHasFire)
        {
            return UnsafeReason(temperatureC, roomHasFire) != null;
        }
    }
}
