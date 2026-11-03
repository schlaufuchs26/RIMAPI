using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// The Animals tab's training card (fox ticket #4212, gap 5): which tricks an
    /// animal is being taught, who it follows, and where it may go.
    ///
    /// Training progress itself is not readable: the game keeps the step counter
    /// in an internal field. The read answers whether a trick is learned and
    /// whether it is wanted, which is what the card's checkboxes show.
    /// </summary>
    public class AnimalTrainingDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The animal's race def name, e.g. \"Muffalo\".")]
        public string Race { get; set; }

        /// <summary>The colonist the animal follows, or null.</summary>
        [ParameterDescription("The name of the pawn the animal follows.")]
        public string Master { get; set; }

        [ParameterDescription("The numeric id of the pawn the animal follows.")]
        public int? MasterId { get; set; }

        /// <summary>The area the animal is restricted to, or null for anywhere.</summary>
        [ParameterDescription("The allowed area the animal is kept in.")]
        public string Area { get; set; }

        [ParameterDescription("Whether the animal follows its master when drafted.")]
        public bool FollowDrafted { get; set; }
        [ParameterDescription("Whether the animal follows its master during fieldwork.")]
        public bool FollowFieldwork { get; set; }

        [ParameterDescription("One entry per trainable the game offers for this animal.")]
        public List<TrainableDto> Trainables { get; set; }
    }

    public class TrainableDto
    {
        [ParameterDescription("The trainable's def name, e.g. \"Tameness\".")]
        public string Trainable { get; set; }
        public string Label { get; set; }

        /// <summary>Whether the training card shows this trick for this animal.</summary>
        [ParameterDescription("Whether the training card shows this trainable.")]
        public bool Visible { get; set; }

        /// <summary>Whether the animal can be assigned the trick now, and why not.</summary>
        [ParameterDescription("Whether the colony can still train this, or the game refuses.")]
        public bool CanAssign { get; set; }
        [ParameterDescription("Why the game will not train this, when it refuses.")]
        public string Refusal { get; set; }

        [ParameterDescription("Whether the animal has learned it.")]
        public bool Learned { get; set; }
        [ParameterDescription("Whether the colony has asked for it to be trained.")]
        public bool Wanted { get; set; }
    }

    public class SetTrainingRequest
    {
        public int PawnId { get; set; }

        /// <summary>The trick's def name, e.g. "Hauling".</summary>
        [ParameterDescription("The trainable's def name, e.g. \"Tameness\".")]
        public string Trainable { get; set; }

        /// <summary>Whether the animal should be taught it.</summary>
        [ParameterDescription("Whether the colony wants it trained.")]
        public bool Wanted { get; set; }
    }

    public class AnimalMasterRequest
    {
        public int PawnId { get; set; }

        /// <summary>The colonist to follow; 0 or absent means no master.</summary>
        [ParameterDescription("The numeric id of the pawn the animal should follow.")]
        public int MasterId { get; set; }
    }

    public class AnimalAreaRequest
    {
        public int PawnId { get; set; }

        /// <summary>An area's label, or null/empty for anywhere on the map.</summary>
        [ParameterDescription("The allowed area's name, or empty to let the animal roam.")]
        public string Area { get; set; }
    }

    public class AnimalFollowRequest
    {
        public int PawnId { get; set; }
        [ParameterDescription("Whether the animal follows its master when drafted.")]
        public bool? FollowDrafted { get; set; }
        [ParameterDescription("Whether the animal follows its master during fieldwork.")]
        public bool? FollowFieldwork { get; set; }
    }
}
