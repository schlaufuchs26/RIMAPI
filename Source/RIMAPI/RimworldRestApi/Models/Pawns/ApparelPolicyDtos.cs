using System.Collections.Generic;

namespace RIMAPI.Models
{
    /// <summary>
    /// An apparel policy (fox ticket #4212, gap 2): the Assign tab's outfit. The
    /// read already existed as OutfitDto; these are the writes the Manage
    /// outfits dialog offers, so a colony can stop wearing tattered gear instead
    /// of only reading that it does.
    ///
    /// Hit points are on the read's own scale, 0..1, and quality is the game's
    /// name (Awful, Poor, Normal, Good, Excellent, Masterwork, Legendary), so a
    /// caller can take the read's answer and send it back.
    /// </summary>
    public class ApparelPolicyRequest
    {
        /// <summary>The policy to change; 0 creates a new one.</summary>
        public int Id { get; set; }

        /// <summary>The name the dialog shows, e.g. "Worker".</summary>
        public string Label { get; set; }

        /// <summary>
        /// Start from "nothing allowed", the dialog's Clear all. The lists below
        /// then say what comes back in.
        /// </summary>
        public bool DisallowAll { get; set; }

        /// <summary>
        /// Start from every apparel allowed, the dialog's Allow all. Ignored when
        /// DisallowAll is set; a new policy already starts that way.
        /// </summary>
        public bool AllowAllApparel { get; set; }

        public List<string> AllowThingDefNames { get; set; }
        public List<string> DisallowThingDefNames { get; set; }

        /// <summary>Special filters to switch on, e.g. AllowDeadmansApparel.</summary>
        public List<string> AllowSpecialFilterDefNames { get; set; }

        /// <summary>Special filters to switch off, e.g. AllowNonDeadmansApparel.</summary>
        public List<string> DisallowSpecialFilterDefNames { get; set; }

        /// <summary>Hit points the apparel may have, 0..1 like the read.</summary>
        public float? HitPointsMin { get; set; }
        public float? HitPointsMax { get; set; }

        /// <summary>Quality the apparel may have, as the game names it.</summary>
        public string QualityMin { get; set; }
        public string QualityMax { get; set; }
    }

    /// <summary>Assign a policy to a pawn, the Assign tab's dropdown.</summary>
    public class PawnApparelPolicyRequest
    {
        public int PawnId { get; set; }
        public int PolicyId { get; set; }
    }

    /// <summary>The policy read back after a write: what the game holds now.</summary>
    public class ApparelPolicyResultDto
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public int AllowedDefCount { get; set; }
        public float AllowedHitPointsMin { get; set; }
        public float AllowedHitPointsMax { get; set; }
        public string AllowedQualityMin { get; set; }
        public string AllowedQualityMax { get; set; }
        public List<string> AllowedThingDefNames { get; set; }
        public List<string> DisallowedSpecialFilterDefNames { get; set; }
        /// <summary>How many pawns wear this policy right now.</summary>
        public int PawnsUsing { get; set; }
    }
}
