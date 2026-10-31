using System.Collections.Generic;

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
        public string Area { get; set; }

        public PositionDto PointA { get; set; }
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
        public string Pawn { get; set; }
    }

    // FirefightSkipDto is a burning cell no colonist went to, with the reason,
    // so a run can tell "nobody free" from "nobody can reach it".
    public class FirefightSkipDto
    {
        public int X { get; set; }
        public int Z { get; set; }
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
        public int Cells { get; set; }
        public int Fires { get; set; }
        public bool Home { get; set; }
        public List<FirefightAssignmentDto> Assignments { get; set; }
        public List<FirefightSkipDto> Skipped { get; set; }
        public string Refusal { get; set; }
    }
}
