using System.Collections.Generic;
using RIMAPI.Helpers;
using Verse;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The incapability list a colonist read carries (ticket #3360): a pawn
    /// whose backstory disables violence shows no trait saying so, so the work
    /// tags are the only place the fact can come from.
    /// </summary>
    public class IncapabilityTagsTests
    {
        [Fact]
        public void No_disabled_tags_read_as_no_incapability()
        {
            Assert.Empty(IncapabilityTags.From(WorkTags.None));
        }

        [Fact]
        public void Violence_is_named_the_way_the_game_names_it()
        {
            List<string> labels = IncapabilityTags.From(WorkTags.Violent);

            Assert.Equal(new List<string> { "violence" }, labels);
        }

        [Fact]
        public void Several_tags_come_in_the_game_s_own_order()
        {
            // A backstory commonly disables violence and skilled labor
            // together; the order is the game's own list, not the mask's bits.
            List<string> labels = IncapabilityTags.From(
                WorkTags.Artistic | WorkTags.Violent | WorkTags.ManualSkilled);

            Assert.Equal(new List<string> { "violence", "skilled labor", "artistic" }, labels);
        }

        [Fact]
        public void The_tags_a_fighter_needs_are_named_too()
        {
            // Hunting and Shooting are what a violence-incapable pawn loses
            // besides Violent itself, and they must not read as an empty list.
            List<string> labels = IncapabilityTags.From(WorkTags.Hunting | WorkTags.Shooting);

            Assert.Equal(new List<string> { "hunting", "shooting" }, labels);
        }

        [Fact]
        public void Can_fight_follows_the_violence_tag_alone()
        {
            Assert.True(IncapabilityTags.CanFight(WorkTags.None));
            Assert.False(IncapabilityTags.CanFight(WorkTags.Violent));
            // Artistic and hauling are not violence: such a pawn still fights.
            Assert.True(IncapabilityTags.CanFight(WorkTags.Artistic | WorkTags.Hauling));
        }
    }
}
