using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // One job change of one colonist: the def the game gave it, the label a
    // reader knows ("harvest"), and the thing it works on. The tool folds a
    // list of these into "what each colonist did and for how long" (ticket
    // #3300), so the mod reports changes only, never spans.
    public class JobLogEntryDto
    {
        [ParameterDescription("The game tick the job change happened on.")]
        public int Tick { get; set; }
        public int PawnId { get; set; }
        [ParameterDescription("The pawn's name, as the pawn list shows it.")]
        public string Pawn { get; set; }
        // Job is the job def's defName, empty when the pawn has no job at all
        // (that is the state a run has to see as idle).
        [ParameterDescription("The job def name the pawn started.")]
        public string Job { get; set; }
        public string Label { get; set; }
        // Target is the label of the thing the job works on, empty when it
        // works on a cell or on nothing.
        [ParameterDescription("What the job was aimed at, as the game names it.")]
        public string Target { get; set; }
        // Idle is the game's own idle state for this entry: true when the pawn
        // has nothing to do (Pawn_MindState.IsIdle, the state the colonists-idle
        // alert shows). It replaces the tool's list of wander job names
        // (ticket #3392), so a wander job a game update adds is idle at once.
        [ParameterDescription("Whether the pawn had no job at that tick.")]
        public bool Idle { get; set; }
    }

    // The answer of GET /api/v1/colonists/job-log: the record from Since on,
    // plus the entry each pawn was in at Since, and the tick it was read at.
    public class JobLogDto
    {
        [ParameterDescription("The game tick the answer was built on.")]
        public int Now { get; set; }
        [ParameterDescription("The game tick the log starts at.")]
        public int Since { get; set; }
        [ParameterDescription("One entry per job change, newest first.")]
        public List<JobLogEntryDto> Entries { get; set; }
    }
}
