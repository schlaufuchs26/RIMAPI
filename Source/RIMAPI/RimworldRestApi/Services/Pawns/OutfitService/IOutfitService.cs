using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    /// <summary>
    /// The Assign tab's apparel policy (fox ticket #4212, gap 2): create, edit
    /// the filter, delete and assign, the four writes the Manage outfits dialog
    /// and the Assign tab offer.
    /// </summary>
    public interface IOutfitService
    {
        ApiResult<ApparelPolicyResultDto> Create(ApparelPolicyRequest request);

        ApiResult<ApparelPolicyResultDto> Update(ApparelPolicyRequest request);

        ApiResult<ApparelPolicyResultDto> Delete(int policyId);

        ApiResult<ApparelPolicyResultDto> Assign(PawnApparelPolicyRequest request);
    }
}
