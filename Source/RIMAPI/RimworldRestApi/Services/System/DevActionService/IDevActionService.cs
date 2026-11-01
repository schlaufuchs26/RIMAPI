using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    /// <summary>
    /// The `dev` action of the fox's rimworld tool (fox ticket #3757): build a
    /// test situation in the debug instance. The route is refused unless the
    /// game itself runs in dev mode.
    /// </summary>
    public interface IDevActionService
    {
        ApiResult<DevActionResultDto> Run(DevActionRequest request);
    }
}
