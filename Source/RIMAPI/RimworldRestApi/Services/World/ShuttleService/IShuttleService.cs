using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    /// <summary>
    /// The shuttles on a map and the game's own boarding order (fox ticket
    /// #3862).
    /// </summary>
    public interface IShuttleService
    {
        ApiResult<List<ShuttleDto>> Shuttles(int mapId);

        ApiResult<ShuttleEnterResultDto> Enter(int pawnId, int shuttleId);
    }
}
