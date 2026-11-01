using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The bed a rescue carries a patient to (fox ticket #3383): a bed in a
    /// burning room or above 50 C is no destination, whatever the game's own
    /// lookup picked first.
    /// </summary>
    public class BedSafetyTests
    {
        [Fact]
        public void A_cool_room_is_a_destination()
        {
            Assert.Null(BedSafety.UnsafeReason(21.3f, false));
            Assert.False(BedSafety.IsUnsafe(21.3f, false));
        }

        [Fact]
        public void A_room_above_the_threshold_names_its_reading()
        {
            Assert.Equal(
                "its room reads 111.9 C, above the 50 C a carried patient cannot take",
                BedSafety.UnsafeReason(111.9f, false));
        }

        [Fact]
        public void The_threshold_itself_is_already_too_hot()
        {
            Assert.True(BedSafety.IsUnsafe(BedSafety.TooHotC, false));
            Assert.False(BedSafety.IsUnsafe(BedSafety.TooHotC - 0.1f, false));
        }

        [Fact]
        public void A_fire_counts_even_in_a_cool_room()
        {
            Assert.Equal("a fire burns in its room", BedSafety.UnsafeReason(18f, true));
        }

        // The escape rule of fox ticket #3621: a cool prisoner bed on open
        // ground is no destination at all, because the game counts only a bed
        // inside an enclosed room as a prison cell and the prisoner walks off
        // the moment he is put into it.
        [Fact]
        public void A_bed_in_a_prison_cell_is_a_cell()
        {
            Assert.Null(BedSafety.NoCellReason(true, false, true));
        }

        [Fact]
        public void A_bed_outdoors_is_no_cell_however_cool_it_reads()
        {
            Assert.Equal("it stands outdoors", BedSafety.NoCellReason(false, true, false));
            // A roleless room the roof grid still calls outdoors.
            Assert.Equal("it stands outdoors", BedSafety.NoCellReason(true, true, false));
        }

        [Fact]
        public void A_roofed_room_the_game_does_not_count_is_no_cell_either()
        {
            Assert.Equal("its room is no prison cell", BedSafety.NoCellReason(true, false, false));
        }

        [Fact]
        public void A_proper_room_of_sane_size_can_hold_a_prison_cell()
        {
            Assert.Null(BedSafety.NotPrisonCellReason(true, false, false, true));
        }

        [Fact]
        public void A_room_the_game_refuses_says_which_condition_it_fails()
        {
            // A transport pod's captive needs a prison cell, and a run that
            // reads only "no prisoner bed" cannot tell a missing bed from a
            // room the game refuses (fox ticket #3742).
            Assert.Equal("it stands in no room",
                BedSafety.NotPrisonCellReason(false, false, false, false));
            Assert.Equal("its room touches the map edge, which the game never counts as a prison cell",
                BedSafety.NotPrisonCellReason(true, true, false, true));
            Assert.Equal("its room is too large to be a prison cell",
                BedSafety.NotPrisonCellReason(true, false, true, true));
            Assert.Equal("its room is not enclosed",
                BedSafety.NotPrisonCellReason(true, false, false, false));
        }

        // The bed list prints the label instead of the sentence (fox ticket
        // #3750): 80 characters per refused bed made the list unreadable.
        [Fact]
        public void Every_refusal_carries_a_short_label_beside_its_sentence()
        {
            Assert.Null(BedSafety.NotPrisonCell(true, false, false, true));
            Assert.Equal("no room", BedSafety.NotPrisonCell(false, false, false, false).Cause);
            Assert.Equal("map edge", BedSafety.NotPrisonCell(true, true, false, true).Cause);
            Assert.Equal("too large", BedSafety.NotPrisonCell(true, false, true, true).Cause);
            Assert.Equal("not enclosed", BedSafety.NotPrisonCell(true, false, false, false).Cause);
        }
    }
}
