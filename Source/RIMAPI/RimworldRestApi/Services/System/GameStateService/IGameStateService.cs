using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface IGameStateService
    {
        ApiResult<GameStateDto> GetGameState();
        ApiResult SetGameSpeed(int speed);
        ApiResult GameSave(GameSaveRequestDto body);
        ApiResult GameLoad(GameLoadRequestDto body);
        ApiResult GameDevQuickStart();
        ApiResult GoToMainMenu();
        ApiResult QuitGame();
        ApiResult GameStart(NewGameStartRequestDto body);

        // The starting-colonist batch a founding picks from (ticket #3924).
        ApiResult StartCandidateBatch(NewGameStartRequestDto body);
        CandidateBatchDto GetCandidateBatch();
        ApiResult<GameSettingsDto> GetCurrentSettings();
        ApiResult<bool> ToggleRunInBackground();
        ApiResult<bool> GetRunInBackground();
        ApiResult<bool> SetManualPriorities(bool enabled);
    }
}