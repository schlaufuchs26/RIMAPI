using System.Collections.Generic;
using RIMAPI.Services;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// How a growth letter's answer finds the choice a caller named (ticket
    /// #3714). A growth letter offers a handful of skills and traits, and a
    /// wrong pick is permanent, so an ambiguous word is refused instead of
    /// guessed.
    /// </summary>
    public class GrowthChoiceRulesTests
    {
        private static List<string> Offered()
        {
            return new List<string> { "Shooting", "Melee", "Cooking", "Medicine" };
        }

        [Fact]
        public void An_index_the_read_prints_picks_that_entry()
        {
            string problem;

            Assert.Equal(2, GrowthChoiceRules.Resolve(Offered(), "2", out problem));
            Assert.Null(problem);
        }

        [Fact]
        public void An_exact_label_wins_over_a_containing_one()
        {
            // "Medicine" is offered and "Medicine" also contains nothing else;
            // the point is that an exact hit is taken before the scan for a
            // word inside a label.
            var labels = new List<string> { "Cooking (minor)", "Cooking" };
            string problem;

            Assert.Equal(1, GrowthChoiceRules.Resolve(labels, "Cooking", out problem));
            Assert.Null(problem);
        }

        [Fact]
        public void A_word_inside_one_label_finds_it()
        {
            string problem;

            Assert.Equal(3, GrowthChoiceRules.Resolve(Offered(), "med", out problem));
            Assert.Null(problem);
        }

        [Fact]
        public void A_word_that_matches_two_labels_is_refused_not_guessed()
        {
            // The child's growth is permanent, so "Cook" over "Cooking" and
            // "Cooking (minor)" has to come back as a question.
            var labels = new List<string> { "Cooking", "Cooking (minor)" };
            string problem;

            var index = GrowthChoiceRules.Resolve(labels, "Cook", out problem);

            Assert.Equal(-1, index);
            Assert.Contains("more than one", problem);
            Assert.Contains("0 Cooking", problem);
        }

        [Fact]
        public void An_index_past_the_end_names_the_offered_choices()
        {
            string problem;

            var index = GrowthChoiceRules.Resolve(Offered(), "9", out problem);

            Assert.Equal(-1, index);
            Assert.Contains("there is no choice 9", problem);
            Assert.Contains("3 Medicine", problem);
        }

        [Fact]
        public void A_letter_without_choices_says_so()
        {
            string problem;

            Assert.Equal(-1, GrowthChoiceRules.Resolve(new List<string>(), "Shooting", out problem));
            Assert.Contains("no choice", problem);
        }

        [Fact]
        public void A_caller_may_name_several_choices_in_one_word()
        {
            // A letter that grants two passions takes two words, comma
            // separated; empty pieces from a trailing comma are dropped.
            Assert.Equal(
                new List<string> { "Shooting", "Cooking" },
                GrowthChoiceRules.Words(" Shooting , Cooking, "));
            Assert.Empty(GrowthChoiceRules.Words("  "));
            Assert.Empty(GrowthChoiceRules.Words(null));
        }

        [Fact]
        public void The_words_none_and_no_trait_both_mean_no_trait()
        {
            Assert.True(GrowthChoiceRules.IsNoTrait("none"));
            Assert.True(GrowthChoiceRules.IsNoTrait(" No trait "));
            Assert.False(GrowthChoiceRules.IsNoTrait("Tough"));
            Assert.False(GrowthChoiceRules.IsNoTrait(""));
        }

        [Fact]
        public void The_offer_list_prints_the_index_a_caller_passes_back()
        {
            Assert.Equal("0 Shooting, 1 Melee", GrowthChoiceRules.OfferList(Offered().GetRange(0, 2)));
            Assert.Equal("none", GrowthChoiceRules.OfferList(new List<string>()));
        }
    }
}
