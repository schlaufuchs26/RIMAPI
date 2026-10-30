using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface IBuilderService
    {
        ApiResult<BlueprintDto> CopyArea(CopyAreaRequestDto request);
        ApiResult PasteArea(PasteAreaRequestDto request);
        ApiResult<PlaceResultDto> PlaceBlueprints(PasteAreaRequestDto request);
        ApiResult<CancelResultDto> CancelBlueprints(CancelBlueprintsRequestDto request);
        ApiResult<CheckZoneResultDto> CheckZone(CheckZoneRequestDto request);
    }
}