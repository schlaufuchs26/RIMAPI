using System.Collections.Generic;
using RIMAPI.Core;

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
        [ParameterDescription("The policy's numeric id; omit to create a new one.")]
        public int Id { get; set; }

        /// <summary>The name the dialog shows, e.g. "Worker".</summary>
        public string Label { get; set; }

        /// <summary>
        /// Start from "nothing allowed", the dialog's Clear all. The lists below
        /// then say what comes back in.
        /// </summary>
        [ParameterDescription("When true, the policy allows nothing, whatever else it lists.")]
        public bool DisallowAll { get; set; }

        /// <summary>
        /// Start from every apparel allowed, the dialog's Allow all. Ignored when
        /// DisallowAll is set; a new policy already starts that way.
        /// </summary>
        [ParameterDescription("When true, every apparel item passes the policy.")]
        public bool AllowAllApparel { get; set; }

        [ParameterDescription("Apparel def names the policy allows, on top of its filters.")]
        public List<string> AllowThingDefNames { get; set; }
        [ParameterDescription("Apparel def names the policy refuses.")]
        public List<string> DisallowThingDefNames { get; set; }

        /// <summary>Special filters to switch on, e.g. AllowDeadmansApparel.</summary>
        [ParameterDescription("Special filter names the policy allows, e.g. \"Apparel\".")]
        public List<string> AllowSpecialFilterDefNames { get; set; }

        /// <summary>Special filters to switch off, e.g. AllowNonDeadmansApparel.</summary>
        [ParameterDescription("Special filter names the policy refuses.")]
        public List<string> DisallowSpecialFilterDefNames { get; set; }

        /// <summary>Hit points the apparel may have, 0..1 like the read.</summary>
        [ParameterDescription("The lowest hit points an allowed item may have.")]
        public float? HitPointsMin { get; set; }
        [ParameterDescription("The highest hit points an allowed item may have.")]
        public float? HitPointsMax { get; set; }

        /// <summary>Quality the apparel may have, as the game names it.</summary>
        [ParameterDescription("The lowest quality an allowed item may have.")]
        public string QualityMin { get; set; }
        [ParameterDescription("The highest quality an allowed item may have.")]
        public string QualityMax { get; set; }
    }

    /// <summary>Assign a policy to a pawn, the Assign tab's dropdown.</summary>
    public class PawnApparelPolicyRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("The apparel policy's numeric id to assign.")]
        public int PolicyId { get; set; }
    }

    /// <summary>The policy read back after a write: what the game holds now.</summary>
    public class ApparelPolicyResultDto
    {
        [ParameterDescription("The policy's numeric id.")]
        public int Id { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How many apparel defs the policy currently allows.")]
        public int AllowedDefCount { get; set; }
        [ParameterDescription("The lowest hit points the policy allows.")]
        public float AllowedHitPointsMin { get; set; }
        [ParameterDescription("The highest hit points the policy allows.")]
        public float AllowedHitPointsMax { get; set; }
        [ParameterDescription("The lowest quality the policy allows.")]
        public string AllowedQualityMin { get; set; }
        [ParameterDescription("The highest quality the policy allows.")]
        public string AllowedQualityMax { get; set; }
        [ParameterDescription("The apparel def names the policy allows.")]
        public List<string> AllowedThingDefNames { get; set; }
        [ParameterDescription("The special filter names the policy refuses.")]
        public List<string> DisallowedSpecialFilterDefNames { get; set; }
        /// <summary>How many pawns wear this policy right now.</summary>
        [ParameterDescription("The pawns that wear this policy.")]
        public int PawnsUsing { get; set; }
    }
}
