using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The reason a building route gives for an id the stored read lists but
    /// the map has not built (ticket #3295): every route shares one wording,
    /// and a stored building always carries the step that is missing.
    /// </summary>
    public class UnbuiltBuildingReasonTests
    {
        private const string OnlyBuilt = "only a built building on the map can be read";

        [Fact]
        public void A_held_thing_is_named_with_the_route_clause()
        {
            string reason = UnbuiltBuildingReason.Describe(
                "Thing 12 is inside Grave #7 (grave), not on the map",
                false,
                OnlyBuilt);

            Assert.Equal(
                "Thing 12 is inside Grave #7 (grave), not on the map; " + OnlyBuilt,
                reason);
        }

        [Fact]
        public void A_stored_building_says_it_has_to_be_installed_first()
        {
            string reason = UnbuiltBuildingReason.Describe(
                "Thing 12 is a Bed packed up as MinifiedThing #9 at (3, 4), not built on the map",
                true,
                OnlyBuilt);

            Assert.Equal(
                "Thing 12 is a Bed packed up as MinifiedThing #9 at (3, 4), not built on the map; "
                    + "install it first, " + OnlyBuilt,
                reason);
        }

        [Fact]
        public void An_id_that_names_nothing_gets_no_reason()
        {
            // Null means the caller answers its own "not found".
            Assert.Null(UnbuiltBuildingReason.Describe(null, true, OnlyBuilt));
        }

        [Fact]
        public void A_route_keeps_the_stored_reason()
        {
            const string Reason = "Thing 12 is inside Grave #7 (grave), not on the map; " + OnlyBuilt;

            Assert.Equal(Reason, UnbuiltBuildingReason.OrNotFound(Reason, "Building 3 not found or is not a work table"));
        }

        [Fact]
        public void A_route_without_a_stored_reason_answers_its_own_not_found()
        {
            // An id that names nothing, or a built building that is not the
            // thing the route needs, has no stored reason to report.
            Assert.Equal(
                "Building 5 not found or is not a work table",
                UnbuiltBuildingReason.OrNotFound(
                    null,
                    "Building 5 not found or is not a work table"));
        }
    }
}
