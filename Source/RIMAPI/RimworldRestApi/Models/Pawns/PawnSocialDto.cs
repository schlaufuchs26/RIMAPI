using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class InteractionDefDto
    {
        [ParameterDescription("The interaction's def name, e.g. \"Chat\".")]
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
    }

    public class PawnInteractionStatusDto
    {
        [ParameterDescription("Whether the pawn can start an interaction now.")]
        public bool CanInteract { get; set; }
        [ParameterDescription("The ticks left before another interaction is possible.")]
        public int CooldownTicks { get; set; }
        [ParameterDescription("The same cooldown in in-game days.")]
        public float CooldownDays { get; set; }
        [ParameterDescription("The def name of the pawn's last interaction.")]
        public string LastInteractionDef { get; set; }
        [ParameterDescription("The tick the last interaction happened on.")]
        public int LastInteractionTicks { get; set; }
    }

    public class PawnInteractionLogDto
    {
        public int PawnId { get; set; }
        [ParameterDescription("One entry per interaction, newest first.")]
        public List<InteractionLogEntryDto> Interactions { get; set; }
        public int Count { get; set; }
    }

    public class InteractionLogEntryDto
    {
        [ParameterDescription("The numeric id of the pawn who started it.")]
        public int InitiatorId { get; set; }
        [ParameterDescription("The name of the pawn who started it.")]
        public string InitiatorName { get; set; }
        [ParameterDescription("The numeric id of the pawn it was aimed at.")]
        public int RecipientId { get; set; }
        [ParameterDescription("The name of the pawn it was aimed at.")]
        public string RecipientName { get; set; }
        [ParameterDescription("The interaction's def name, e.g. \"Chat\".")]
        public string InteractionDefName { get; set; }
        [ParameterDescription("The interaction's label, as the game shows it.")]
        public string InteractionLabel { get; set; }
        [ParameterDescription("The sentence the game wrote for the interaction.")]
        public string Text { get; set; }
        [ParameterDescription("The tick the interaction happened on.")]
        public int Ticks { get; set; }
        [ParameterDescription("How long ago it happened, in words.")]
        public string TimeAgo { get; set; }
    }

    public class PawnOpinionDto
    {
        [ParameterDescription("The numeric id of the pawn the opinion is about.")]
        public int TargetPawnId { get; set; }
        [ParameterDescription("The name of the pawn the opinion is about.")]
        public string TargetPawnName { get; set; }
        [ParameterDescription("The pawn's opinion of the other, from -100 to 100.")]
        public int Opinion { get; set; }
        [ParameterDescription("The thoughts that make up the opinion.")]
        public List<OpinionBreakdownDto> Breakdown { get; set; }
    }

    public class OpinionBreakdownDto
    {
        [ParameterDescription("The thought's def name.")]
        public string ThoughtDefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How much this thought adds to the opinion.")]
        public float Score { get; set; }
    }

    public class ForceInteractionRequestDto
    {
        [ParameterDescription("The numeric id of the pawn who starts it.")]
        public int InitiatorId { get; set; }
        [ParameterDescription("The numeric id of the pawn it is aimed at.")]
        public int RecipientId { get; set; }
        [ParameterDescription("The interaction's def name, e.g. \"Chat\".")]
        public string InteractionDefName { get; set; }
    }

    public class AddRelationRequestDto
    {
        [ParameterDescription("The numeric id of the first pawn.")]
        public int Pawn1Id { get; set; }
        [ParameterDescription("The numeric id of the second pawn.")]
        public int Pawn2Id { get; set; }
        [ParameterDescription("The relation's def name, e.g. \"Spouse\".")]
        public string RelationDefName { get; set; }
    }

    public class RemoveRelationRequestDto
    {
        [ParameterDescription("The numeric id of the first pawn.")]
        public int Pawn1Id { get; set; }
        [ParameterDescription("The numeric id of the second pawn.")]
        public int Pawn2Id { get; set; }
        [ParameterDescription("The relation's def name, e.g. \"Spouse\".")]
        public string RelationDefName { get; set; }
    }

    public class PawnRelationsDto
    {
        public int PawnId { get; set; }
        [ParameterDescription("One entry per pawn this pawn has a relation with.")]
        public List<PawnRelationEntryDto> Relations { get; set; }
    }

    public class PawnRelationEntryDto
    {
        [ParameterDescription("The numeric id of the related pawn.")]
        public int OtherPawnId { get; set; }
        [ParameterDescription("The name of the related pawn.")]
        public string OtherPawnName { get; set; }
        [ParameterDescription("The relation's def name, e.g. \"Spouse\".")]
        public string RelationDefName { get; set; }
        [ParameterDescription("The relation's label, as the game shows it.")]
        public string RelationLabel { get; set; }
    }
}
