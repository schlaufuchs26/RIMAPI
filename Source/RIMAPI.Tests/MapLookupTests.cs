using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // A map route that was handed an id no map carries says so (fox ticket
    // #3658). The routes used to hand the null to a helper, which either threw
    // inside the mod (a 500 for a wrong map_id) or answered success with no
    // data, so a wrong id read the same as an empty map.
    public class MapLookupTests
    {
        [Fact]
        public void AMissingMapIdNamesTheMap()
        {
            var map = MapLookup.Resolve<object>(7, _ => null, out string error);

            Assert.Null(map);
            Assert.Equal("Map not found: 7", error);
        }

        [Fact]
        public void APresentMapIdYieldsTheMapAndNoError()
        {
            var found = new object();

            var map = MapLookup.Resolve(7, _ => found, out string error);

            Assert.Same(found, map);
            Assert.Null(error);
        }

        [Fact]
        public void TheFinderIsAskedForTheRoutesOwnId()
        {
            int asked = 0;

            MapLookup.Resolve<object>(7, id => { asked = id; return null; }, out _);

            Assert.Equal(7, asked);
        }

        [Fact]
        public void TheGrowingZoneReadNamesTheSameSentence()
        {
            Assert.Equal(MapLookup.MapNotFound(7), GrowingZoneLookup.MapNotFound(7));
        }
    }
}
