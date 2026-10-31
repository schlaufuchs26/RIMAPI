using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface IOrderService
    {
        ApiResult DesignateArea(DesignateRequestDto request);

        // Roof a rectangle through the game's own build-roof job (fox ticket
        // #3418): a sealed room stays cold until a constructor builds the roof.
        ApiResult<RoofResultDto> BuildRoof(RoofRequestDto request);
    }
}
