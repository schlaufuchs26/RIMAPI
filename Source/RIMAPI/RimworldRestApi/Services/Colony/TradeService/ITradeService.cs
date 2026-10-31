using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface ITradeService
    {
        ApiResult<List<TraderKindDto>> GetAllTraderDefs();
        // The live traders on the map (ticket #3170).
        ApiResult<List<TraderDto>> GetTraders(int mapId);
        // One purchase or sale through the game's own TradeDeal.
        ApiResult<TradeResultDto> Trade(TradeRequestDto request);
    }
}
