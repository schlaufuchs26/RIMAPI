using RIMAPI.Core;
using RIMAPI.Models;
using RIMAPI.Models.Map;

namespace RIMAPI.Services
{
    public interface IBuildingService
    {
        ApiResult<BuildingDto> GetBuildingInfo(int buildingId);

        // The two halves of moving a building (fox ticket #4316, parity wave 7).
        ApiResult<UninstallResultDto> Uninstall(UninstallRequestDto request);
        ApiResult<InstallResultDto> Install(InstallRequestDto request);
        ApiResult<PoweredBuildingDto> SetBuildingPower(int buildingId, bool powerOn);
        ApiResult<BedDto> SetBedForPrisoners(int buildingId, bool forPrisoners);
        ApiResult<BedOwnerDto> SetBedOwner(int buildingId, int pawnId);
        ApiResult<BedMedicalDto> SetBedMedical(int buildingId, bool? medical);
        ApiResult<TemperatureTargetDto> SetTemperatureTarget(int buildingId, float? target);
    }
}
