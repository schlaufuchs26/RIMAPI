using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface IPawnInfoService
    {
        // Get all pawns (enemies, animals, colonists) on a map
        ApiResult<List<PawnDto>> GetPawnsOnMap(int mapId);

        // The humanlike people who are not free colonists: wild people,
        // prisoners, slaves, guests and downed raiders (ticket #3137).
        ApiResult<List<PersonDto>> GetPeople(int mapId);

        // Get aggregated details
        ApiResult<PawnDetailedDto> GetPawnDetails(int pawnId);

        // Specific data endpoints
        ApiResult<PawnInventoryDto> GetPawnInventory(int pawnId);
    }
}