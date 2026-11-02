using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    /// <summary>
    /// Forming a caravan (fox ticket #4212, wave 2). The world map could be read
    /// and never written, so a colony could not leave its own tile.
    /// </summary>
    public interface ICaravanService
    {
        ApiResult<FormCaravanResultDto> Form(FormCaravanRequest request);
    }
}
