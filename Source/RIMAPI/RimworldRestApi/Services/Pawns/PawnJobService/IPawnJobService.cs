using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface IPawnJobService
    {
        ApiResult AssignJob(PawnJobRequestDto request);
        ApiResult AssignTendJob(MedicalTendRequestDto request);

        // The Strip order (fox ticket #4316, parity wave 7).
        ApiResult Strip(StripRequestDto request);
        ApiResult AssignBedRest(MedicalBedRestRequestDto request);
    }
}
