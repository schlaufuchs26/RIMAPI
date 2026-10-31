using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    // WalkParms is the one place the mod builds the parms for the game's own
    // reachability check. The pawn has to be in them (ticket #3236): the
    // map-level CanReach overload builds TraverseParms without a pawn, and
    // Region.Allows answers every closed door with canBashDoors (false) when
    // tp.pawn is null. A pawn standing in a room was therefore told there is no
    // path through its own door, and a drafted colonist could not be ordered
    // out to fight. Never go back to an overload that leaves the pawn out.
    internal static class WalkParms
    {
        internal static TraverseParms For(Pawn pawn, Danger maxDanger)
        {
            return TraverseParms.For(pawn, maxDanger, TraverseMode.ByPawn, false);
        }
    }
}
