using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface ICombatService
    {
        ApiResult<CombatStateDto> GetCombat(int mapId);
        ApiResult<List<DraftResultDto>> SetDrafted(DraftRequestDto request);
        ApiResult<OrderResultDto> Order(OrderRequestDto request);
    }
}
