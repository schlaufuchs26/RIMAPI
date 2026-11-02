using System.Collections.Generic;

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
        public string Race { get; set; }

        /// <summary>The colonist the animal follows, or null.</summary>
        public string Master { get; set; }

        public int? MasterId { get; set; }

        /// <summary>The area the animal is restricted to, or null for anywhere.</summary>
        public string Area { get; set; }

        public bool FollowDrafted { get; set; }
        public bool FollowFieldwork { get; set; }

        public List<TrainableDto> Trainables { get; set; }
    }

    public class TrainableDto
    {
        public string Trainable { get; set; }
        public string Label { get; set; }

        /// <summary>Whether the training card shows this trick for this animal.</summary>
        public bool Visible { get; set; }

        /// <summary>Whether the animal can be assigned the trick now, and why not.</summary>
        public bool CanAssign { get; set; }
        public string Refusal { get; set; }

        public bool Learned { get; set; }
        public bool Wanted { get; set; }
    }

    public class SetTrainingRequest
    {
        public int PawnId { get; set; }

        /// <summary>The trick's def name, e.g. "Hauling".</summary>
        public string Trainable { get; set; }

        /// <summary>Whether the animal should be taught it.</summary>
        public bool Wanted { get; set; }
    }

    public class AnimalMasterRequest
    {
        public int PawnId { get; set; }

        /// <summary>The colonist to follow; 0 or absent means no master.</summary>
        public int MasterId { get; set; }
    }

    public class AnimalAreaRequest
    {
        public int PawnId { get; set; }

        /// <summary>An area's label, or null/empty for anywhere on the map.</summary>
        public string Area { get; set; }
    }

    public class AnimalFollowRequest
    {
        public int PawnId { get; set; }
        public bool? FollowDrafted { get; set; }
        public bool? FollowFieldwork { get; set; }
    }
}
