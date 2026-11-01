namespace RIMAPI.Helpers
{
    /// <summary>
    /// Why a growing-zone read has no zone to report (fox ticket #3613). The
    /// helper answered null for a stockpile's id and for an id no zone carries,
    /// and the route wrapped that null in Ok, so a caller saw success with no
    /// data and could not tell a wrong map id from a non-growing zone. The two
    /// messages are the create route's, kept here so the read and the create
    /// name the same failure the same way.
    /// </summary>
    public static class GrowingZoneLookup
    {
        public static string MapNotFound(int mapId) => $"Map not found: {mapId}";

        public static string ZoneNotFound(int mapId, int zoneId) =>
            $"No growing zone with id {zoneId} on map {mapId}";
    }
}
