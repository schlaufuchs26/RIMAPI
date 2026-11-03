using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // One letter on the game's letter stack (top right). Letters are how the
    // game tells a player that visitors arrived or a quest is on offer; the
    // alerts read does not show them, and a letter that stays unread sits on
    // top of the map forever (ticket #3147).
    public class LetterDto
    {
        [ParameterDescription("The letter's numeric id.")]
        public int Id { get; set; }
        // The title the game draws, e.g. "Visitors".
        public string Label { get; set; }
        // The first line of the letter's own body, so a caller can tell a raid
        // from a trade caravan without opening it.
        [ParameterDescription("The first line of the letter's own body, so a caller can tell a raid from a trade caravan without opening it.")]
        public string Text { get; set; }
        // The whole body the game shows when the letter is opened: a
        // paragraph or two. A wait that ends on a letter hands the run the
        // offer itself, so deciding on it costs no second read (ticket #3491).
        [ParameterDescription("The whole body the game shows when the letter is opened: a paragraph or two.")]
        public string Body { get; set; }
        // The labels the letter's dialog draws, in the order it draws them: a
        // joiner offers Accept and Reject. Empty for a letter without a
        // choice, so a caller reads the list, not the letter's type
        // (ticket #3491).
        [ParameterDescription("The labels the letter's dialog draws, in the order it draws them: a joiner offers Accept and Reject.")]
        public List<string> Options { get; set; } = new List<string>();
        // LetterDef defName: ThreatBig, ThreatSmall, PositiveEvent, NegativeEvent,
        // Quest, Event, ... A caller groups by this.
        [ParameterDescription("LetterDef defName: ThreatBig, ThreatSmall, PositiveEvent, NegativeEvent, Quest, Event, ...")]
        public string Def { get; set; }
        // The game tick the letter arrived, the same clock the game state reads.
        [ParameterDescription("The game tick the letter arrived, the same clock the game state reads.")]
        public int Tick { get; set; }
        // Whether the game's own right-click dismiss applies. A letter that
        // needs an answer (a quest offer, a choice) refuses it and shows a
        // popup instead, which holds the clock.
        [ParameterDescription("Whether the game's own right-click dismiss applies.")]
        public bool CanDismiss { get; set; }
        // Whether this read is the first to report the letter. The game keeps
        // no read flag on a letter, so the bridge remembers what it has shown
        // and what the player opened, and marks the stack seen as it reports it
        // (ticket #3156).
        [ParameterDescription("Whether this read is the first to report the letter.")]
        public bool New { get; set; }
        // The quest this letter announces, for a NewQuestLetter; null for every
        // other letter. A quest offer's letter offers no accept, so the id is
        // the link to the quests read and to quest/accept (ticket #3216).
        [ParameterDescription("The quest this letter announces, for a NewQuestLetter; null for every other letter.")]
        public int? QuestId { get; set; }
        // Whether the game's own auto-pause rule pauses the clock when this
        // letter is received: the player's pause setting is at or above the
        // letter's own mode (LetterStack.ReceiveLetter). A pause with no dialog
        // and no letter on the stack is a letter that paused the game and then
        // left the stack, and this flag is what names it (ticket #3357).
        [ParameterDescription("Whether the game's own auto-pause rule pauses the clock when this letter is received: the player's pause setting is at or above the letter's own mode (LetterStack.ReceiveLetter).")]
        public bool Pauses { get; set; }
        // PassionChoices are the skills a growth letter offers for the child's
        // new passion, in the order its dialog draws them: the index is what
        // the growth call takes (ticket #3714). Empty for every other letter.
        [ParameterDescription("PassionChoices are the skills a growth letter offers for the child's new passion, in the order its dialog draws them: the index is what the growth call takes (ticket #3714).")]
        public List<string> PassionChoices { get; set; } = new List<string>();
        // TraitChoices are the traits a growth letter offers.
        [ParameterDescription("TraitChoices are the traits a growth letter offers.")]
        public List<string> TraitChoices { get; set; } = new List<string>();
        // NoTraitOption says a growth letter also offers "no trait".
        [ParameterDescription("NoTraitOption says a growth letter also offers \"no trait\".")]
        public bool NoTraitOption { get; set; }
        // PassionGains is how many of the offered skills the growth letter
        // asks for: one, in practice.
        [ParameterDescription("PassionGains is how many of the offered skills the growth letter asks for: one, in practice.")]
        public int PassionGains { get; set; }
    }

    // Dismiss one letter by the id the letters read prints.
    public class LetterDismissRequestDto
    {
        [ParameterDescription("The letter to dismiss, by id.")]
        public int Id { get; set; }
    }

    public class LetterDismissResultDto
    {
        [ParameterDescription("Whether the letter was dismissed.")]
        public bool Dismissed { get; set; }
        public string Label { get; set; }
        // Why a letter could not be dismissed, empty on success.
        [ParameterDescription("Why a letter could not be dismissed, empty on success.")]
        public string Reason { get; set; }
        // How many letters stand afterwards.
        [ParameterDescription("How many letters stand afterwards.")]
        public int Remaining { get; set; }
    }

    // Answer a choice letter (a joiner, a quest offer, a ritual choice): the id
    // the letters read prints, and the option to press, matched against the
    // label the dialog draws. A word is enough ("accept", "reject"), and the
    // answer names the options when it matches none (ticket #3188).
    public class LetterAnswerRequestDto
    {
        [ParameterDescription("The letter to answer, by id.")]
        public int Id { get; set; }
        [ParameterDescription("The option to choose, by index.")]
        public string Option { get; set; }
    }

    public class LetterAnswerResultDto
    {
        // True when the option was pressed; a letter that offers no dialog, or
        // an option that matches none, answers false with a reason instead.
        [ParameterDescription("True when the option was pressed; a letter that offers no dialog, or an option that matches none, answers false with a reason instead.")]
        public bool Answered { get; set; }
        public string Label { get; set; }
        // Pressed is the label of the option that was run, the game's own text.
        [ParameterDescription("Pressed is the label of the option that was run, the game's own text.")]
        public string Pressed { get; set; }
        // Options are the choices the dialog drew, so a caller that matched
        // nothing learns what it can say.
        [ParameterDescription("Options are the choices the dialog drew, so a caller that matched nothing learns what it can say.")]
        public List<string> Options { get; set; } = new List<string>();
        // Why nothing was pressed, empty on success.
        [ParameterDescription("Why nothing was pressed, empty on success.")]
        public string Reason { get; set; }
        // How many letters stand afterwards.
        [ParameterDescription("How many letters stand afterwards.")]
        public int Remaining { get; set; }
        // StillOpen says a dialog holds the clock after the press: a choice
        // that leads to a second node is answered again with the options it
        // now offers.
        [ParameterDescription("StillOpen says a dialog holds the clock after the press: a choice that leads to a second node is answered again with the options it now offers.")]
        public bool StillOpen { get; set; }
        [ParameterDescription("Whether answering opened a window.")]
        public string OpenWindow { get; set; }
    }

    // Take the choice a growth letter asks for (ticket #3714): the skill that
    // gains a passion and, when the letter offers traits, one trait or "none".
    // Both halves are matched against the labels the letter's own dialog
    // draws, and either a label or the 0-based index works.
    public class LetterGrowthRequestDto
    {
        [ParameterDescription("The letter to answer, by id.")]
        public int Id { get; set; }
        // Passion names the skill that gains a passion, or the index the
        // letters read prints. Empty asks only what the letter offers.
        [ParameterDescription("Passion names the skill that gains a passion, or the index the letters read prints.")]
        public string Passion { get; set; }
        // Trait names the trait, or "none" for the letter's own no-trait
        // option. Empty when the letter offers no trait.
        [ParameterDescription("Trait names the trait, or \"none\" for the letter's own no-trait option.")]
        public string Trait { get; set; }
    }

    public class LetterGrowthResultDto
    {
        // Applied is true when the game made the choice: the skill gained its
        // passion, the trait was gained and the letter left the stack.
        [ParameterDescription("Applied is true when the game made the choice: the skill gained its passion, the trait was gained and the letter left the stack.")]
        public bool Applied { get; set; }
        public string Label { get; set; }
        // Pawn is the child whose growth moment this is.
        [ParameterDescription("Pawn is the child whose growth moment this is.")]
        public string Pawn { get; set; }
        // PassionChoices are the skills the letter offers, one per index.
        [ParameterDescription("PassionChoices are the skills the letter offers, one per index.")]
        public List<string> PassionChoices { get; set; } = new List<string>();
        // TraitChoices are the traits the letter offers.
        [ParameterDescription("TraitChoices are the traits the letter offers.")]
        public List<string> TraitChoices { get; set; } = new List<string>();
        // NoTraitOption says the letter offers "no trait" as well.
        [ParameterDescription("NoTraitOption says the letter offers \"no trait\" as well.")]
        public bool NoTraitOption { get; set; }
        // PassionGains is how many of the offered skills the letter asks for.
        [ParameterDescription("PassionGains is how many of the offered skills the letter asks for.")]
        public int PassionGains { get; set; }
        // ChosenPassions names what was gained, e.g. "Shooting (Major)".
        [ParameterDescription("ChosenPassions names what was gained, e.g.")]
        public List<string> ChosenPassions { get; set; } = new List<string>();
        // ChosenTrait is the trait gained, empty when the child gained none.
        [ParameterDescription("ChosenTrait is the trait gained, empty when the child gained none.")]
        public string ChosenTrait { get; set; }
        // Why nothing was applied, empty on success.
        [ParameterDescription("Why nothing was applied, empty on success.")]
        public string Reason { get; set; }
        // How many letters stand afterwards.
        [ParameterDescription("How many letters stand afterwards.")]
        public int Remaining { get; set; }
    }
}
