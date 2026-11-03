using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// A bed's own medical setting (fox ticket #4235), the bed tab's
    /// "Medical" gizmo. It is what makes a bed a hospital bed; the medicine
    /// policy beside it belongs to the pawn, not to the bed. A call without a
    /// value reads the flag, a call with one sets it and answers what the game
    /// holds afterwards.
    /// </summary>
    public class BedMedicalDto
    {
        [ParameterDescription("The bed's numeric id.")]
        public int Id { get; set; }
        public string Label { get; set; }
        public string DefName { get; set; }

        /// <summary>True when the def has the toggle at all: false for an animal bed or a sleeping spot.</summary>
        [ParameterDescription("True when the def has the toggle at all: false for an animal bed or a sleeping spot.")]
        public bool CanBeMedical { get; set; }

        /// <summary>The flag as the bed holds it now.</summary>
        [ParameterDescription("The flag as the bed holds it now.")]
        public bool Medical { get; set; }

        /// <summary>The flag before this call wrote it; equal to Medical on a read.</summary>
        [ParameterDescription("The flag before this call wrote it; equal to Medical on a read.")]
        public bool PreviousMedical { get; set; }

        /// <summary>True when this call changed the flag.</summary>
        [ParameterDescription("True when this call changed the flag.")]
        public bool Changed { get; set; }
    }
}
