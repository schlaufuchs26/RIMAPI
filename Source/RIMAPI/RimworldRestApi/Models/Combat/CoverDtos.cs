using System.Collections.Generic;

namespace RIMAPI.Models
{
    // Cover is what a fight decision needs after the combat read (ticket
    // #3174): the fight read names positions, but not which cell shields a
    // colonist and which cell still has a shot at the raider. These models
    // carry the game's own answer for each cell around a pawn: the block
    // chance CoverUtility gives it against one shooter, the neighbouring
    // things that provide it, and whether the cell can see the target.
    public class CoverCellDto
    {
        public int X { get; set; }
        public int Z { get; set; }
        // Cover is the game's own block chance for a pawn standing here
        // against the named target (0..1, 0.75 behind a wall or sandbag). It is
        // directional: the same cell reads 0 when the cover thing is not
        // between it and the shooter.
        public float Cover { get; set; }
        // Providers names the adjacent things that would give cover, as
        // "Sandbags (101,50)", so a cell that looks like cover but reads 0
        // tells its own story.
        public List<string> Providers { get; set; }
        public bool Standable { get; set; }
        // Reachable is the walk check for the pawn the read is around; false
        // when no pawn was named.
        public bool Reachable { get; set; }
        public bool LineOfSight { get; set; }
        // Distance is the cell's distance to the target, in cells.
        public float Distance { get; set; }
    }

    public class CoverStateDto
    {
        public int MapId { get; set; }
        // X/Z is the cell the read is around, PawnId/PawnName name the pawn
        // when it came from one (0/"" for a bare cell).
        public int X { get; set; }
        public int Z { get; set; }
        public int PawnId { get; set; }
        public string PawnName { get; set; }
        public int TargetId { get; set; }
        public string TargetName { get; set; }
        public int TargetX { get; set; }
        public int TargetZ { get; set; }
        public int Radius { get; set; }
        // The counts say what the radius holds: standable cells, cells with a
        // cover neighbour, cells that really give cover against this target,
        // and cells with a line to it.
        public int StandableCount { get; set; }
        public int CoverNeighbourCount { get; set; }
        public int CoverCount { get; set; }
        public int LineOfSightCount { get; set; }
        // Cells holds the standable cells with a cover neighbour, best first,
        // capped; CoverNeighbourCount says how many there were.
        public List<CoverCellDto> Cells { get; set; }
    }
}
