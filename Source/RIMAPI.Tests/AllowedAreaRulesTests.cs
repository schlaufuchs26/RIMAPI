using System;
using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #3645: `allowed_area` names an area the way the game's dropdown does
    // while the map holds Area objects whose label is translated and whose type
    // name is not. The matching decides which area a restriction lands on, so
    // it is tested here rather than in the game.
    public class AllowedAreaRulesTests
    {
        private static readonly List<string> Labels = new List<string> { "Home", "Zuhause", "Garden" };
        private static readonly List<string> Types = new List<string>
        {
            "Area_Home",
            "Area_Allowed",
            "Area_Allowed",
        };

        [Fact]
        public void TheLabelsWordReachesTheArea()
        {
            Assert.Equal(0, AllowedAreaRules.IndexOf("Home", Labels, Types));
            Assert.Equal(2, AllowedAreaRules.IndexOf("Garden", Labels, Types));
        }

        // The game translates a label, so a German install answers "Zuhause"
        // for the home area; the type name does not move, so it counts too.
        [Fact]
        public void TheTypeNameCountsAndSoDoesTheGamesOwnForm()
        {
            Assert.Equal(0, AllowedAreaRules.IndexOf("Area_Home", Labels, Types));
            Assert.Equal(1, AllowedAreaRules.IndexOf("area_allowed", Labels, Types));
            Assert.Equal(0, AllowedAreaRules.IndexOf("home", Labels, Types));
            Assert.Equal(0, AllowedAreaRules.IndexOf("  HOME  ", Labels, Types));
        }

        [Fact]
        public void AnUnknownWordFindsNothing()
        {
            Assert.Equal(-1, AllowedAreaRules.IndexOf("Paddock", Labels, Types));
            Assert.Equal(-1, AllowedAreaRules.IndexOf("", Labels, Types));
        }

        // Lifting the restriction is a word of its own: an empty area must
        // never unrestrict a pawn by accident.
        [Fact]
        public void UnrestrictedIsItsOwnWordAndEmptyIsNot()
        {
            Assert.True(AllowedAreaRules.IsUnrestricted("Unrestricted"));
            Assert.True(AllowedAreaRules.IsUnrestricted(" none "));
            Assert.False(AllowedAreaRules.IsUnrestricted(""));
            Assert.False(AllowedAreaRules.IsUnrestricted("Home"));
            Assert.False(AllowedAreaRules.IsUnrestricted("Garden"));
        }

        [Fact]
        public void TheOptionsNameEveryAreaOrSayNone()
        {
            Assert.Equal("Home, Zuhause, Garden", AllowedAreaRules.OptionsText(Labels));
            Assert.Equal("none", AllowedAreaRules.OptionsText(new List<string>()));
            Assert.Equal("none", AllowedAreaRules.OptionsText(null));
        }

        // A list of labels and a list of type names must pair up; a caller
        // that passes mismatched lists hears about it instead of matching a
        // label against another area's type name.
        [Fact]
        public void MismatchedListsAreRefused()
        {
            Assert.Throws<ArgumentException>(() =>
                AllowedAreaRules.IndexOf("Home", Labels, new List<string> { "Area_Home" }));
        }
    }
}
