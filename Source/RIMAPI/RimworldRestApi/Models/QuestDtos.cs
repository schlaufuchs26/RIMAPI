using System.Collections.Generic;

namespace RIMAPI.Models
{
    // One reward choice of a quest: the Quests tab draws an "Accept quest for"
    // button per choice, and the game drops the other choices' quest parts when
    // one is taken (ticket #3216).
    public class QuestChoiceDto
    {
        public int Index { get; set; }
        // The choice's total market value, the game's own number.
        public float TotalMarketValue { get; set; }
        // The reward kinds in the choice (Reward_Items, Reward_Goodwill, ...),
        // enough to tell "some silver" from "a new colonist".
        public List<string> Rewards { get; set; } = new List<string>();
        // What the choice's item rewards hold: a Reward_Items names only its
        // kind, so without this a caller picks a choice blind (fox ticket
        // #3950). Empty for a choice with no item reward. A reward the game
        // already handed out lists what it gave, from the labels it remembers
        // (fox ticket #4009).
        public List<QuestChoiceItemDto> Items { get; set; } = new List<QuestChoiceItemDto>();
    }

    // One thing inside a reward choice's item reward, as the game would drop
    // it: the label the Quests tab shows, the stack size and the game's own
    // market value for the stack.
    public class QuestChoiceItemDto
    {
        public string Label { get; set; }
        public int Count { get; set; }
        public float MarketValue { get; set; }
        // The quality label when the thing has one (QualityUtility.GetLabel),
        // empty for a thing without quality.
        public string Quality { get; set; }
        // True when the game already handed the reward out, so these are the
        // labels it remembers rather than things still waiting to drop.
        public bool Remembered { get; set; }
    }

    // Accept one quest by the id the quests read prints. pawn_id names the
    // colonist who takes it, for a quest the game marks RequiresAccepter;
    // choice is the reward choice's index for a quest that offers several.
    public class QuestAcceptRequestDto
    {
        public int Id { get; set; }
        public int? PawnId { get; set; }
        public int? Choice { get; set; }
    }

    public class QuestAcceptResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        // Accepted is true when the game's own accept ran and the quest left
        // the offer state.
        public bool Accepted { get; set; }
        // The game's own CanAcceptQuest report, whether or not it was taken.
        public bool CanAccept { get; set; }
        public string AcceptReason { get; set; }
        // The quest's state after the call (Ongoing on success).
        public string State { get; set; }
        // The colonist the game recorded as accepter; empty when the quest was
        // taken for the colony.
        public string Accepter { get; set; }
        public int AccepterPawnId { get; set; }
        // The reward choice that was taken, -1 when the quest had none.
        public int Choice { get; set; } = -1;
        // Why the quest was not accepted, empty on success.
        public string Reason { get; set; }
        // The choices the quest offers, filled when a choice index is missing or
        // out of range so the caller can name one.
        public List<QuestChoiceDto> Choices { get; set; } = new List<QuestChoiceDto>();
    }
}
