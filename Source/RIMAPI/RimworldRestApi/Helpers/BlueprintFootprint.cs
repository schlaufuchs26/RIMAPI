using RIMAPI.Models;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The footprint of a planned build (ticket #3608). A blueprint used to
    /// report only the cell the game calls its position, so a 2x1 bench's plan
    /// covered one cell and a map art drew a multi-cell plan as its corner.
    /// The size is the def's own, unrotated; the caller swaps it for the east
    /// and west rotations, the same rule it uses for a building's size.
    /// </summary>
    public static class BlueprintFootprint
    {
        public static PositionDto Of(BuildableDef buildDef)
        {
            // A blueprint's EntityToBuild is typed as BuildableDef, but only a
            // ThingDef carries a size; anything else reads as one cell.
            var def = buildDef as ThingDef;
            if (def == null) return null;
            return new PositionDto
            {
                X = def.size.x,
                Y = 0,
                Z = def.size.z,
            };
        }
    }
}
