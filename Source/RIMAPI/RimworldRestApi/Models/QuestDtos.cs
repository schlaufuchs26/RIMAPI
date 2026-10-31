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
