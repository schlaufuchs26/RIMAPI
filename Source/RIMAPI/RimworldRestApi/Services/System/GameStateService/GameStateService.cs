using System;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    public class GameStateService : IGameStateService
    {
        public ApiResult<GameStateDto> GetGameState()
        {
            try
            {
                return ApiResult<GameStateDto>.Ok(GamePlayHelper.GetGameStateDto());
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error getting game state: {ex}");
                return ApiResult<GameStateDto>.Fail($"Failed to get game state: {ex.Message}");
            }
        }

        public ApiResult SetGameSpeed(int speed)
        {
            try
            {
                Find.TickManager.CurTimeSpeed = (TimeSpeed)speed;
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail($"Failed to set game speed: {ex.Message}");
            }
        }

        public ApiResult GoToMainMenu()
        {
            LongEventHandler.ExecuteWhenFinished(() => GenScene.GoToMainMenu());
            return ApiResult.Ok();
        }

        public ApiResult QuitGame()
        {
            LongEventHandler.ExecuteWhenFinished(() => Root.Shutdown());
            return ApiResult.Ok();
        }

        public ApiResult GameSave(GameSaveRequestDto body)
        {
            try
            {
                if (body == null) return ApiResult.Fail("Request body is missing.");
                if (Current.Game == null) return ApiResult.Fail("No active game is currently running.");
                if (GameDataSaveLoader.SavingIsTemporarilyDisabled) return ApiResult.Fail("Saving is temporarily disabled.");

                string saveName = Current.Game.Info.permadeathMode
                    ? Current.Game.Info.permadeathModeUniqueName
                    : GenFile.SanitizedFileName(body.FileName);

                if (string.IsNullOrWhiteSpace(saveName)) return ApiResult.Fail("File name is required.");

                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    LongEventHandler.QueueLongEvent(() => GameDataSaveLoader.SaveGame(saveName), "SavingLongEvent", false, null);
                    Messages.Message("Game saved as: " + saveName, MessageTypeDefOf.SilentInput);
                });

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail($"Failed to initialize game save: {ex.Message}");
            }
        }

        public ApiResult GameLoad(GameLoadRequestDto body)
        {
            try
            {
                if (body == null) return ApiResult.Fail("Request body is missing.");
                string filePath = GenFilePaths.FilePathForSavedGame(body.FileName);
                if (!System.IO.File.Exists(filePath)) return ApiResult.Fail($"Save file not found: {body.FileName}.rws");

                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    if (!body.CheckVersion)
                    {
                        GameDataSaveLoader.LoadGame(body.FileName);
                    }
                    else if (body.SkipModMismatch)
                    {
                        PreLoadUtility.CheckVersionAndLoad(filePath, ScribeMetaHeaderUtility.ScribeHeaderMode.Map,
                            () => GameDataSaveLoader.LoadGame(body.FileName), skipOnMismatch: true);
                    }
                    else
                    {
                        GameDataSaveLoader.CheckVersionAndLoadGame(body.FileName);
                    }
                });

                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail($"Failed to load game: {ex.Message}");
            }
        }

        public ApiResult GameDevQuickStart()
        {
            try
            {
                LongEventHandler.QueueLongEvent(() =>
                {
                    Root_Play.SetupForQuickTestPlay();
                    PageUtility.InitGameStart();
                }, "GeneratingMap", true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
                return ApiResult.Ok();
            }
            catch (Exception ex) { return ApiResult.Fail(ex.Message); }
        }

        public ApiResult GameStart(NewGameStartRequestDto request)
        {
            try
            {
                // The wish and the scenario are checked before the world is
                // generated: a long event cannot answer its caller, so an
                // unknown biome or scenario would otherwise fail into the log
                // and leave no colony (ticket #3930).
                string problem = LandingSitePick.Problem(request);
                if (problem != "")
                {
                    return ApiResult.Fail(problem);
                }
                LongEventHandler.QueueLongEvent(() =>
                {
                    GamePlayHelper.InitGameFromConfiguration(request);
                    PageUtility.InitGameStart();
                }, "GeneratingMap", true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
                return ApiResult.Ok();
            }
            catch (Exception ex) { return ApiResult.Fail(ex.Message); }
        }

        public ApiResult<GameSettingsDto> GetCurrentSettings()
        {
            return ApiResult<GameSettingsDto>.Ok(GamePlayHelper.GetCurrentSettings());
        }

        public ApiResult<bool> ToggleRunInBackground()
        {
            Prefs.RunInBackground = !Prefs.RunInBackground;
            Prefs.Save();
            return ApiResult<bool>.Ok(Prefs.RunInBackground);
        }

        public ApiResult<bool> GetRunInBackground()
        {
            return ApiResult<bool>.Ok(Prefs.RunInBackground);
        }

        // SetManualPriorities switches the colony's manual work priorities, the
        // option that decides whether a pawn's stored level orders its work.
        // The flag lives in PlaySettings (per game, saved with the colony), not
        // in Prefs, so it needs a loaded game and is not persisted on its own.
        public ApiResult<bool> SetManualPriorities(bool enabled)
        {
            try
            {
                if (Current.Game == null || Find.PlaySettings == null)
                {
                    return ApiResult<bool>.Fail("No colony is loaded, so there are no work priorities to switch.");
                }

                Find.PlaySettings.useWorkPriorities = enabled;
                return ApiResult<bool>.Ok(Find.PlaySettings.useWorkPriorities);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Fail(ex.Message);
            }
        }
    }
}