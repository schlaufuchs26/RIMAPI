using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// What a `prioritize` order did (fox ticket #3626): the job the game built
    /// for one named colonist, the work type that owns it, where he stands, and
    /// when the game would not offer that entry, its refusal plus the jobs it
    /// does offer on that thing or cell, which is what the float menu shows a
    /// player.
    /// </summary>
    public class PriorityJobResultDto
    {
        public int PawnId { get; set; }
        [ParameterDescription("The pawn's name, as the pawn list shows it.")]
        public string Pawn { get; set; }
        [ParameterDescription("The job def name that was ordered.")]
        public string Job { get; set; }
        [ParameterDescription("The work type the job belongs to.")]
        public string WorkType { get; set; }
        [ParameterDescription("Whether the job was queued.")]
        public bool Queued { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        [ParameterDescription("Whether the game offered the job to the pawn.")]
        public List<string> Offered { get; set; }
        [ParameterDescription("Why the pawn did not take the job.")]
        public string Refusal { get; set; }
        // Note says which step of the work the job is when the answer would
        // otherwise mislead: a delivery to a planned build is the step before
        // the build, not the build itself (fox #3777).
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }
}
