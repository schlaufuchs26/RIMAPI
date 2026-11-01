using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface IBuildingService
    {
        ApiResult<BuildingDto> GetBuildingInfo(int buildingId);
        ApiResult<PoweredBuildingDto> SetBuildingPower(int buildingId, bool powerOn);
        ApiResult<BedDto> SetBedForPrisoners(int buildingId, bool forPrisoners);
        ApiResult<BedOwnerDto> SetBedOwner(int buildingId, int pawnId);
        ApiResult<TemperatureTargetDto> SetTemperatureTarget(int buildingId, float? target);
    }
}
