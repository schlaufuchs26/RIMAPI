using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The rules the game's own rename dialogs and growing-zone Plant button
    // enforce (fox ticket #4316). A route that writes a name or a crop has to
    // refuse exactly what the dialog refuses, otherwise the API can do
    // something no player can do.
    public class ZoneEditRulesTests
    {
        private static readonly List<string> Taken = new List<string> { "Garden", "Growing zone 1" };

        [Fact]
        public void AFreeNamePasses()
        {
            Assert.Null(ZoneEditRules.LabelRefusal("Rice field", Taken));
        }

        [Fact]
        public void AnEmptyNameIsRefused()
        {
            Assert.NotNull(ZoneEditRules.LabelRefusal("", Taken));
            Assert.NotNull(ZoneEditRules.LabelRefusal(null, Taken));
        }

        [Fact]
        public void TheGamesFieldStopsAt28Characters()
        {
            // Dialog_Rename<T> accepts input while the name is shorter than 28,
            // so 28 is reachable and 29 is not.
            Assert.Null(ZoneEditRules.LabelRefusal(new string('a', 28), Taken));

            string refusal = ZoneEditRules.LabelRefusal(new string('a', 29), Taken);
            Assert.Contains("29", refusal);
            Assert.Contains("28", refusal);
        }

        [Fact]
        public void ANameInUseIsRefused()
        {
            string refusal = ZoneEditRules.LabelRefusal("Garden", Taken);
            Assert.Contains("Garden", refusal);
            Assert.Contains("already in use", refusal);
        }

        // The game compares the stored label to the typed name with ==, so the
        // case matters: a player can hold both "Garden" and "garden".
        [Fact]
        public void TheComparisonIsOrdinalLikeTheGames()
        {
            Assert.Null(ZoneEditRules.LabelRefusal("garden", Taken));
        }

        [Fact]
        public void AnUpdateThatNamesNothingIsRefused()
        {
            string refusal = ZoneEditRules.GrowingRefusal(null, null, null);
            Assert.Contains("plant_def", refusal);
            Assert.Contains("allow_sow", refusal);
            Assert.Contains("allow_cut", refusal);
        }

        [Fact]
        public void OneSettingIsEnough()
        {
            Assert.Null(ZoneEditRules.GrowingRefusal("Plant_Rice", null, null));
            Assert.Null(ZoneEditRules.GrowingRefusal(null, false, null));
            Assert.Null(ZoneEditRules.GrowingRefusal(null, null, true));
        }

        [Fact]
        public void ACropFromTheGamesListPasses()
        {
            Assert.Null(
                ZoneEditRules.PlantRefusal(
                    "Plant_Rice",
                    new List<string> { "Plant_Potato", "Plant_Rice" }
                )
            );
        }

        [Fact]
        public void ACropOutsideTheListIsRefusedAndTheReadIsNamed()
        {
            string refusal = ZoneEditRules.PlantRefusal(
                "Plant_TreeOak",
                new List<string> { "Plant_Potato", "Plant_Rice" }
            );

            Assert.Contains("Plant_TreeOak", refusal);
            Assert.Contains("/api/v1/map/zone/growing/plants", refusal);
        }

        [Fact]
        public void AMissingCropIsRefusedWithTheFieldName()
        {
            Assert.Contains(
                "plant_def",
                ZoneEditRules.PlantRefusal("", new List<string> { "Plant_Rice" })
            );
        }
    }
}
