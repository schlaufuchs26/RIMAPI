using System.Collections.Generic;

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
        public int Id { get; set; }
        public string Label { get; set; }

        /// <summary>The policy a new pawn gets, the dialog's star.</summary>
        public bool IsDefault { get; set; }

        /// <summary>How many pawns follow this policy right now.</summary>
        public int PawnsUsing { get; set; }

        public List<DrugPolicyEntryWriteDto> Entries { get; set; }
    }

    /// <summary>
    /// One drug inside a policy. A null field on a write leaves the game's value
    /// alone, so changing "allowed for joy" does not reset the mood limit.
    /// </summary>
    public class DrugPolicyEntryWriteDto
    {
        public string Drug { get; set; }
        public bool? AllowedForAddiction { get; set; }
        public bool? AllowedForJoy { get; set; }
        public bool? AllowScheduled { get; set; }

        /// <summary>Days between scheduled doses, 0.1 to 25.</summary>
        public float? DaysFrequency { get; set; }

        /// <summary>Mood must be below this (0.01..1) for a mood-driven dose.</summary>
        public float? OnlyIfMoodBelow { get; set; }

        /// <summary>Joy must be below this (0.01..1) for a joy-driven dose.</summary>
        public float? OnlyIfJoyBelow { get; set; }

        /// <summary>How many a pawn carries, 0 to what it can pick up.</summary>
        public int? TakeToInventory { get; set; }
    }

    public class DrugPolicyWriteRequest
    {
        /// <summary>The policy to change; 0 creates a new one.</summary>
        public int Id { get; set; }

        /// <summary>The name the dialog shows, e.g. "No hard drugs".</summary>
        public string Label { get; set; }

        /// <summary>
        /// For a new policy: the policy to copy, the dialog's Duplicate button.
        /// Left out, the new policy starts with the game's defaults.
        /// </summary>
        public int CopyFromPolicyId { get; set; }

        public List<DrugPolicyEntryWriteDto> Entries { get; set; }
    }

    public class PawnDrugPolicyRequest
    {
        public int PawnId { get; set; }
        public int PolicyId { get; set; }
    }
}
