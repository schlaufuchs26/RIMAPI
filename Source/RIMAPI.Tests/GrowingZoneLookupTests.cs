using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // A growing-zone read that finds nothing says why (fox ticket #3613). Both
    // failures used to reach the caller as success with no data, so a stockpile's
    // id and a map that does not exist read the same.
    public class GrowingZoneLookupTests
    {
        [Fact]
        public void AWrongMapIdNamesTheMap()
        {
            Assert.Equal("Map not found: 7", GrowingZoneLookup.MapNotFound(7));
        }

        [Fact]
        public void AZoneIdThatIsNotGrowingNamesBothIds()
        {
            Assert.Equal(
                "No growing zone with id 42 on map 7",
                GrowingZoneLookup.ZoneNotFound(7, 42));
        }
    }
}
