using System.Collections.Generic;

namespace RIMAPI.Models
{
    // One job change of one colonist: the def the game gave it, the label a
    // reader knows ("harvest"), and the thing it works on. The tool folds a
    // list of these into "what each colonist did and for how long" (ticket
    // #3300), so the mod reports changes only, never spans.
    public class JobLogEntryDto
    {
        public int Tick { get; set; }
        public int PawnId { get; set; }
        public string Pawn { get; set; }
        // Job is the job def's defName, empty when the pawn has no job at all
        // (that is the state a run has to see as idle).
        public string Job { get; set; }
        public string Label { get; set; }
        // Target is the label of the thing the job works on, empty when it
        // works on a cell or on nothing.
        public string Target { get; set; }
        // Idle is the game's own idle state for this entry: true when the pawn
        // has nothing to do (Pawn_MindState.IsIdle, the state the colonists-idle
        // alert shows). It replaces the tool's list of wander job names
        // (ticket #3392), so a wander job a game update adds is idle at once.
        public bool Idle { get; set; }
    }

    // The answer of GET /api/v1/colonists/job-log: the record from Since on,
    // plus the entry each pawn was in at Since, and the tick it was read at.
    public class JobLogDto
    {
        public int Now { get; set; }
        public int Since { get; set; }
        public List<JobLogEntryDto> Entries { get; set; }
    }
}
