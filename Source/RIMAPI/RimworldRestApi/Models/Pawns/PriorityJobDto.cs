using System.Collections.Generic;

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
        public string Pawn { get; set; }
        public string Job { get; set; }
        public string WorkType { get; set; }
        public bool Queued { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public List<string> Offered { get; set; }
        public string Refusal { get; set; }
        // Note says which step of the work the job is when the answer would
        // otherwise mislead: a delivery to a planned build is the step before
        // the build, not the build itself (fox #3777).
        public string Note { get; set; }
    }
}
