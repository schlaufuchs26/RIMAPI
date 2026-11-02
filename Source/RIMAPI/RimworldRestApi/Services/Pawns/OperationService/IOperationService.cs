using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    /// <summary>
    /// The Health tab's operations (fox ticket #4212, gap 4): read the surgery
    /// bills and the medicine policy, list the operations a pawn could be given,
    /// add one, cancel one and set the medicine policy.
    /// </summary>
    public interface IOperationService
    {
        ApiResult<PawnOperationsDto> GetOperations(int pawnId);

        ApiResult<List<SurgeryRecipeDto>> GetSurgeryRecipes(int pawnId);

        ApiResult<PawnOperationsDto> Add(AddOperationRequest request);

        ApiResult<PawnOperationsDto> Cancel(int pawnId, string operationId);

        ApiResult<PawnOperationsDto> SetMedicine(MedicinePolicyRequest request);
    }
}
