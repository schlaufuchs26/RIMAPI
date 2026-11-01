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
        // The map's orbital trade beacons, with or without a ship in orbit
        // (fox ticket #3632).
        ApiResult<List<TradeBeaconDto>> GetTradeBeacons(int mapId);
        // One purchase or sale through the game's own TradeDeal.
        ApiResult<TradeResultDto> Trade(TradeRequestDto request);
    }
}
