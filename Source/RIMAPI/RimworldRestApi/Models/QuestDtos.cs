using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // One reward choice of a quest: the Quests tab draws an "Accept quest for"
    // button per choice, and the game drops the other choices' quest parts when
    // one is taken (ticket #3216).
    public class QuestChoiceDto
    {
        [ParameterDescription("The choice's position in the quest's list.")]
        public int Index { get; set; }
        // The choice's total market value, the game's own number.
        [ParameterDescription("What the choice's rewards are worth in total.")]
        public float TotalMarketValue { get; set; }
        // The reward kinds in the choice (Reward_Items, Reward_Goodwill, ...),
        // enough to tell "some silver" from "a new colonist".
        [ParameterDescription("The things the quest choice offers as reward.")]
        public List<string> Rewards { get; set; } = new List<string>();
        // What the choice's item rewards hold: a Reward_Items names only its
        // kind, so without this a caller picks a choice blind (fox ticket
        // #3950). Empty for a choice with no item reward. A reward the game
        // already handed out lists what it gave, from the labels it remembers
        // (fox ticket #4009).
        [ParameterDescription("The items one choice of the quest offers.")]
        public List<QuestChoiceItemDto> Items { get; set; } = new List<QuestChoiceItemDto>();
    }

    // One thing inside a reward choice's item reward, as the game would drop
    // it: the label the Quests tab shows, the stack size and the game's own
    // market value for the stack.
    public class QuestChoiceItemDto
    {
        public string Label { get; set; }
        public int Count { get; set; }
        [ParameterDescription("What the item is worth in silver.")]
        public float MarketValue { get; set; }
        // The quality label when the thing has one (QualityUtility.GetLabel),
        // empty for a thing without quality.
        public string Quality { get; set; }
        // True when the game already handed the reward out, so these are the
        // labels it remembers rather than things still waiting to drop.
        [ParameterDescription("Whether the item is remembered in the colony's history.")]
        public bool Remembered { get; set; }
    }

    // Accept one quest by the id the quests read prints. pawn_id names the
    // colonist who takes it, for a quest the game marks RequiresAccepter;
    // choice is the reward choice's index for a quest that offers several.
    public class QuestAcceptRequestDto
    {
        [ParameterDescription("The quest's numeric id.")]
        public int Id { get; set; }
        public int? PawnId { get; set; }
        [ParameterDescription("The choice to accept, by its position.")]
        public int? Choice { get; set; }
    }

    public class QuestAcceptResultDto
    {
        [ParameterDescription("The quest's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }
        // Accepted is true when the game's own accept ran and the quest left
        // the offer state.
        [ParameterDescription("Whether the quest was accepted.")]
        public bool Accepted { get; set; }
        // The game's own CanAcceptQuest report, whether or not it was taken.
        [ParameterDescription("Whether the game allows accepting it.")]
        public bool CanAccept { get; set; }
        [ParameterDescription("Why the game refused, when it did.")]
        public string AcceptReason { get; set; }
        // The quest's state after the call (Ongoing on success).
        [ParameterDescription("The quest's state after the call.")]
        public string State { get; set; }
        // The colonist the game recorded as accepter; empty when the quest was
        // taken for the colony.
        [ParameterDescription("The pawn who accepted it, when one did.")]
        public string Accepter { get; set; }
        [ParameterDescription("The numeric id of the accepting pawn.")]
        public int AccepterPawnId { get; set; }
        // The reward choice that was taken, -1 when the quest had none.
        [ParameterDescription("The choice that was taken.")]
        public int Choice { get; set; } = -1;
        // Why the quest was not accepted, empty on success.
        [ParameterDescription("Why the call failed, when it failed.")]
        public string Reason { get; set; }
        // The choices the quest offers, filled when a choice index is missing or
        // out of range so the caller can name one.
        [ParameterDescription("The choices the quest offers for its reward.")]
        public List<QuestChoiceDto> Choices { get; set; } = new List<QuestChoiceDto>();
    }
}
