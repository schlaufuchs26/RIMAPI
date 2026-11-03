using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// The Assign tab's drug policy (fox ticket #4212, gap 3): the per-drug rules
    /// a pawn follows on its own, the ones that keep a colony off addiction.
    ///
    /// The read answers one line per drug with the game's own field names, and the
    /// write takes the same shape back, so a caller can read a line, change one
    /// field and send it.
    /// </summary>
    public class DrugPolicyDto
    {
        [ParameterDescription("The policy's numeric id.")]
        public int Id { get; set; }
        public string Label { get; set; }

        /// <summary>The policy a new pawn gets, the dialog's star.</summary>
        [ParameterDescription("Whether this is the colony's default drug policy.")]
        public bool IsDefault { get; set; }

        /// <summary>How many pawns follow this policy right now.</summary>
        [ParameterDescription("The pawns that follow this policy.")]
        public int PawnsUsing { get; set; }

        [ParameterDescription("One entry per drug the policy names.")]
        public List<DrugPolicyEntryWriteDto> Entries { get; set; }
    }

    /// <summary>
    /// One drug inside a policy. A null field on a write leaves the game's value
    /// alone, so changing "allowed for joy" does not reset the mood limit.
    /// </summary>
    public class DrugPolicyEntryWriteDto
    {
        [ParameterDescription("The drug's def name, e.g. \"Beer\".")]
        public string Drug { get; set; }
        [ParameterDescription("Whether the policy allows the drug for addiction.")]
        public bool? AllowedForAddiction { get; set; }
        [ParameterDescription("Whether the policy allows the drug for recreation.")]
        public bool? AllowedForJoy { get; set; }
        [ParameterDescription("Whether the policy allows it on a schedule.")]
        public bool? AllowScheduled { get; set; }

        /// <summary>Days between scheduled doses, 0.1 to 25.</summary>
        [ParameterDescription("How many days apart the scheduled dose is taken.")]
        public float? DaysFrequency { get; set; }

        /// <summary>Mood must be below this (0.01..1) for a mood-driven dose.</summary>
        [ParameterDescription("Only take it when mood is below this percentage.")]
        public float? OnlyIfMoodBelow { get; set; }

        /// <summary>Joy must be below this (0.01..1) for a joy-driven dose.</summary>
        [ParameterDescription("Only take it when joy is below this percentage.")]
        public float? OnlyIfJoyBelow { get; set; }

        /// <summary>How many a pawn carries, 0 to what it can pick up.</summary>
        [ParameterDescription("How many doses the pawn keeps in its inventory.")]
        public int? TakeToInventory { get; set; }
    }

    public class DrugPolicyWriteRequest
    {
        /// <summary>The policy to change; 0 creates a new one.</summary>
        [ParameterDescription("The policy's numeric id; omit to create a new one.")]
        public int Id { get; set; }

        /// <summary>The name the dialog shows, e.g. "No hard drugs".</summary>
        public string Label { get; set; }

        /// <summary>
        /// For a new policy: the policy to copy, the dialog's Duplicate button.
        /// Left out, the new policy starts with the game's defaults.
        /// </summary>
        [ParameterDescription("Copy the entries of this policy into the new one.")]
        public int CopyFromPolicyId { get; set; }

        [ParameterDescription("One entry per drug the policy names.")]
        public List<DrugPolicyEntryWriteDto> Entries { get; set; }
    }

    public class PawnDrugPolicyRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("The drug policy's numeric id to assign.")]
        public int PolicyId { get; set; }
    }
}
