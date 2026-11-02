using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    /// <summary>
    /// The caravan writes (fox tickets #4212 and #4226, wave 2). The world map
    /// could be read and never written, so a colony could not leave its own tile;
    /// and once a caravan had left, nothing outside the game could steer it.
    /// </summary>
    public interface ICaravanService
    {
        ApiResult<FormCaravanResultDto> Form(FormCaravanRequest request);

        /// <summary>Change a formed caravan's destination, the way a right-click does.</summary>
        ApiResult<CaravanSteerResultDto> Steer(SteerCaravanRequest request);

        /// <summary>Trade with a settlement: send the caravan, or open the dialog when it stands there.</summary>
        ApiResult<CaravanTradeResultDto> Trade(CaravanTradeRequest request);
    }
}
