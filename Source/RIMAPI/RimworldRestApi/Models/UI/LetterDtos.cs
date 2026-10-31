using System.Collections.Generic;

namespace RIMAPI.Models
{
    // One letter on the game's letter stack (top right). Letters are how the
    // game tells a player that visitors arrived or a quest is on offer; the
    // alerts read does not show them, and a letter that stays unread sits on
    // top of the map forever (ticket #3147).
    public class LetterDto
    {
        public int Id { get; set; }
        // The title the game draws, e.g. "Visitors".
        public string Label { get; set; }
        // The first line of the letter's own body, so a caller can tell a raid
        // from a trade caravan without opening it.
        public string Text { get; set; }
        // LetterDef defName: ThreatBig, ThreatSmall, PositiveEvent, NegativeEvent,
        // Quest, Event, ... A caller groups by this.
        public string Def { get; set; }
        // The game tick the letter arrived, the same clock the game state reads.
        public int Tick { get; set; }
        // Whether the game's own right-click dismiss applies. A letter that
        // needs an answer (a quest offer, a choice) refuses it and shows a
        // popup instead, which holds the clock.
        public bool CanDismiss { get; set; }
        // Whether this read is the first to report the letter. The game keeps
        // no read flag on a letter, so the bridge remembers what it has shown
        // and what the player opened, and marks the stack seen as it reports it
        // (ticket #3156).
        public bool New { get; set; }
        // The quest this letter announces, for a NewQuestLetter; null for every
        // other letter. A quest offer's letter offers no accept, so the id is
        // the link to the quests read and to quest/accept (ticket #3216).
        public int? QuestId { get; set; }
        // Whether the game's own auto-pause rule pauses the clock when this
        // letter is received: the player's pause setting is at or above the
        // letter's own mode (LetterStack.ReceiveLetter). A pause with no dialog
        // and no letter on the stack is a letter that paused the game and then
        // left the stack, and this flag is what names it (ticket #3357).
        public bool Pauses { get; set; }
    }

    // Dismiss one letter by the id the letters read prints.
    public class LetterDismissRequestDto
    {
        public int Id { get; set; }
    }

    public class LetterDismissResultDto
    {
        public bool Dismissed { get; set; }
        public string Label { get; set; }
        // Why a letter could not be dismissed, empty on success.
        public string Reason { get; set; }
        // How many letters stand afterwards.
        public int Remaining { get; set; }
    }

    // Answer a choice letter (a joiner, a quest offer, a ritual choice): the id
    // the letters read prints, and the option to press, matched against the
    // label the dialog draws. A word is enough ("accept", "reject"), and the
    // answer names the options when it matches none (ticket #3188).
    public class LetterAnswerRequestDto
    {
        public int Id { get; set; }
        public string Option { get; set; }
    }

    public class LetterAnswerResultDto
    {
        // True when the option was pressed; a letter that offers no dialog, or
        // an option that matches none, answers false with a reason instead.
        public bool Answered { get; set; }
        public string Label { get; set; }
        // Pressed is the label of the option that was run, the game's own text.
        public string Pressed { get; set; }
        // Options are the choices the dialog drew, so a caller that matched
        // nothing learns what it can say.
        public List<string> Options { get; set; } = new List<string>();
        // Why nothing was pressed, empty on success.
        public string Reason { get; set; }
        // How many letters stand afterwards.
        public int Remaining { get; set; }
        // StillOpen says a dialog holds the clock after the press: a choice
        // that leads to a second node is answered again with the options it
        // now offers.
        public bool StillOpen { get; set; }
        public string OpenWindow { get; set; }
    }
}
