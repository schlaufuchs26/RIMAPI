using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface IOrderService
    {
        ApiResult DesignateArea(DesignateRequestDto request);

        // The same order over an explicit list of cells (fox ticket #4095): a
        // zone that is not a rectangle is marked without the cells beside it.
        ApiResult DesignateCells(DesignateCellsRequestDto request);

        // Clear one designation kind over a rectangle (fox ticket #3425): a
        // wrong sweep of harvest or chop marks work nobody can cancel.
        ApiResult UndesignateArea(DesignateRequestDto request);

        // Roof a rectangle through the game's own build-roof job (fox ticket
        // #3418): a sealed room stays cold until a constructor builds the roof.
        ApiResult<RoofResultDto> BuildRoof(RoofRequestDto request);

        // Put one colonist on one burning cell, or on the fires inside the Home
        // area, through the game's own BeatFire job (fox ticket #3444): the
        // Firefighter work priority sends him to whatever fire the game picks.
        ApiResult<FirefightResultDto> Firefight(FirefightRequestDto request);
    }
}
