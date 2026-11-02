using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    /// <summary>
    /// The Animals tab's training card (fox ticket #4212, gap 5): read the card,
    /// switch a trick on or off, set the master, the area and the two follow
    /// checkboxes.
    /// </summary>
    public interface IAnimalTrainingService
    {
        ApiResult<AnimalTrainingDto> Get(int pawnId);

        ApiResult<AnimalTrainingDto> SetTraining(SetTrainingRequest request);

        ApiResult<AnimalTrainingDto> SetMaster(AnimalMasterRequest request);

        ApiResult<AnimalTrainingDto> SetArea(AnimalAreaRequest request);

        ApiResult<AnimalTrainingDto> SetFollow(AnimalFollowRequest request);
    }
}
