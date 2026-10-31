using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface IOrderService
    {
        ApiResult DesignateArea(DesignateRequestDto request);

        // Clear one designation kind over a rectangle (fox ticket #3425): a
        // wrong sweep of harvest or chop marks work nobody can cancel.
        ApiResult UndesignateArea(DesignateRequestDto request);

        // Roof a rectangle through the game's own build-roof job (fox ticket
        // #3418): a sealed room stays cold until a constructor builds the roof.
        ApiResult<RoofResultDto> BuildRoof(RoofRequestDto request);
    }
}
