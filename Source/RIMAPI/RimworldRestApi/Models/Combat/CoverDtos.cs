using System.Collections.Generic;
using RIMAPI.Core;

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
        [ParameterDescription("How much cover the cell gives, 0 to 1.")]
        public float Cover { get; set; }
        // Providers names the adjacent things that would give cover, as
        // "Sandbags (101,50)", so a cell that looks like cover but reads 0
        // tells its own story.
        [ParameterDescription("What gives the cover in this cell.")]
        public List<string> Providers { get; set; }
        [ParameterDescription("Whether a pawn can stand in the cell.")]
        public bool Standable { get; set; }
        // Reachable is the walk check for the pawn the read is around; false
        // when no pawn was named.
        [ParameterDescription("Whether the pawn can reach the cell.")]
        public bool Reachable { get; set; }
        [ParameterDescription("Whether the pawn can see the target from the cell.")]
        public bool LineOfSight { get; set; }
        // Distance is the cell's distance to the target, in cells.
        [ParameterDescription("How far the cell is from the pawn, in map cells.")]
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
        [ParameterDescription("The name of the pawn the cover is for.")]
        public string PawnName { get; set; }
        [ParameterDescription("The numeric id of the pawn or thing it shoots at.")]
        public int TargetId { get; set; }
        [ParameterDescription("The name of the pawn or thing it shoots at.")]
        public string TargetName { get; set; }
        [ParameterDescription("The target cell's x coordinate, in map cells.")]
        public int TargetX { get; set; }
        [ParameterDescription("The target cell's z coordinate, in map cells.")]
        public int TargetZ { get; set; }
        public int Radius { get; set; }
        // The counts say what the radius holds: standable cells, cells with a
        // cover neighbour, cells that really give cover against this target,
        // and cells with a line to it.
        [ParameterDescription("How many cells around the point can be stood in.")]
        public int StandableCount { get; set; }
        [ParameterDescription("How many cells around the point give cover.")]
        public int CoverNeighbourCount { get; set; }
        [ParameterDescription("How many cells give cover.")]
        public int CoverCount { get; set; }
        [ParameterDescription("How many cells see the target.")]
        public int LineOfSightCount { get; set; }
        // CoverAndLineOfSightCount is the number worth acting on: cells that
        // shield the pawn and still have a shot at the target.
        [ParameterDescription("How many cells give cover and see the target.")]
        public int CoverAndLineOfSightCount { get; set; }
        // MatchingCount is how many cells pass the filter (a cover neighbour
        // that gives cover or a line to the target); Cells holds the best few.
        [ParameterDescription("How many cells pass every filter the call asked for.")]
        public int MatchingCount { get; set; }
        // Cells holds the standable cells with a cover neighbour, best first,
        // capped; CoverNeighbourCount says how many there were.
        [ParameterDescription("One entry per cell around the point.")]
        public List<CoverCellDto> Cells { get; set; }
    }
}
