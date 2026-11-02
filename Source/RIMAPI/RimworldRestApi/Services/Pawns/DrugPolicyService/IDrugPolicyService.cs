using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    /// <summary>
    /// The Assign tab's drug policy (fox ticket #4212, gap 3): read the
    /// policies, create, edit, delete and default one, and give one to a pawn.
    /// </summary>
    public interface IDrugPolicyService
    {
        ApiResult<List<DrugPolicyDto>> GetAll();

        ApiResult<DrugPolicyDto> Create(DrugPolicyWriteRequest request);

        ApiResult<DrugPolicyDto> Update(DrugPolicyWriteRequest request);

        ApiResult<DrugPolicyDto> Delete(int policyId);

        ApiResult<DrugPolicyDto> SetDefault(int policyId);

        ApiResult<DrugPolicyDto> Assign(PawnDrugPolicyRequest request);
    }
}
