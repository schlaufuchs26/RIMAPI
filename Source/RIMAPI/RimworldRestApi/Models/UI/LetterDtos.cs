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
}
