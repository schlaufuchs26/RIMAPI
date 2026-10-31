using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface ICoverService
    {
        // GetCover reads the cover cells around a cell or a pawn against one
        // target: the game's own block chance per cell, the neighbouring things
        // that provide it, and whether the cell can see the target.
        ApiResult<CoverStateDto> GetCover(
            int mapId,
            int pawnId,
            int x,
            int z,
            int radius,
            int targetId,
            int targetX,
            int targetZ
        );
    }
}
