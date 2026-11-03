using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// The firefight order (fox ticket #3444). The tool could only raise the
    /// Firefighter work priority, which sends a colonist to whatever fire the
    /// game picks; colony 12 burned from 175 to 637 cells in a day while the
    /// barracks stood at 249 C. The order names the cells instead: a rectangle
    /// (point_a/point_b) or the whole Home area (area "home").
    /// </summary>
    public class FirefightRequestDto
    {
        public int MapId { get; set; }

        // "home" fights every fire inside the colony's Home area. Empty means
        // the rectangle in point_a/point_b; giving both is refused.
        [ParameterDescription("The area to fight fires in, as the game names it.")]
        public string Area { get; set; }

        [ParameterDescription("One corner of the area to cover.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the area.")]
        public PositionDto PointB { get; set; }

        // PawnId names the colonist who fights; 0 lets the mod pick, the
        // colony's firefighters first, then the nearest.
        public int PawnId { get; set; }

        // Limit caps how many cells one call puts a colonist on; 0 means one
        // per free colonist, at most MaxFirefightAssignments.
        public int Limit { get; set; }
    }

    // FirefightAssignmentDto is one queued BeatFire job: the cell and the
    // colonist who walks to it.
    public class FirefightAssignmentDto
    {
        public int X { get; set; }
        public int Z { get; set; }
        public int PawnId { get; set; }
        [ParameterDescription("The pawn assigned to fight the fire.")]
        public string Pawn { get; set; }
    }

    // FirefightSkipDto is a burning cell no colonist went to, with the reason,
    // so a run can tell "nobody free" from "nobody can reach it".
    public class FirefightSkipDto
    {
        public int X { get; set; }
        public int Z { get; set; }
        [ParameterDescription("Why the pawn was skipped.")]
        public string Reason { get; set; }
    }

    /// <summary>
    /// What a firefight call did. Cells counts the distinct burning cells in
    /// scope (two Fire things can share a cell), Fires the Fire things behind
    /// them; Home says the scope was the Home area. Refusal means the whole
    /// call queued nothing.
    /// </summary>
    public class FirefightResultDto
    {
        [ParameterDescription("How many cells the plan covers.")]
        public int Cells { get; set; }
        [ParameterDescription("How many fires the plan found.")]
        public int Fires { get; set; }
        [ParameterDescription("How many fires burn inside the home area.")]
        public bool Home { get; set; }
        [ParameterDescription("The pawns the plan assigned to the fires.")]
        public List<FirefightAssignmentDto> Assignments { get; set; }
        [ParameterDescription("The pawns the plan skipped and why.")]
        public List<FirefightSkipDto> Skipped { get; set; }
        [ParameterDescription("Why the plan was refused, when it was.")]
        public string Refusal { get; set; }
    }
}
