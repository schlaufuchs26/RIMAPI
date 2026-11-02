using System;
using HarmonyLib;
using RIMAPI.Helpers;
using RIMAPI.Services;
using Verse;

namespace RimworldRestApi.Hooks
{
    /// <summary>
    /// Hooks global game state changes (Load, Save, Exit, New Game, Settings)
    /// and forwards them as SSE events.
    /// </summary>
    public static class GameStateHooks
    {
        private static void SendStateEvent(string eventName, object data = null)
        {
            try
            {
                var payload = new
                {
                    event_type = eventName,
                    ticks = Find.TickManager?.TicksGame ?? 0,
                    timestamp = DateTime.UtcNow,
                    data = data
                };

                EventPublisherAccess.Publish("game_state", payload);
            }
            catch (Exception ex)
            {
                var payload = new
                {
                    event_type = eventName,
                    data = ex.Message
                };

                EventPublisherAccess.Publish("error", payload);
            }
        }

        // --- 1. GAME LOADED ---
        // fox #3603: the game keeps no record of the save file a colony was
        // read from outside commitment mode, so this hook records the name and
        // the state read reports it. A release writes the colony back into
        // that file, which is what keeps a probe copy out of the live save.
        [HarmonyPatch(typeof(SavedGameLoaderNow), nameof(SavedGameLoaderNow.LoadGameFromSaveFileNow))]
        public static class LoadGameFromSaveFilePatch
        {
            static void Prefix(string fileName)
            {
                LoadedSave.Set(fileName);
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
        public static class GameLoadPatch
        {
            static void Postfix()
            {
                // The tick this colony starts from: the power figures are
                // written by the game's tick, and the loader's own tick still
                // reads 0 W produced (fox ticket #3781).
                LoadedTick.Record(Find.TickManager?.TicksGame ?? 0);
                // The dev-action menu is built from the loaded defs, so the
                // one the previous colony walked is not this colony's menu
                // (fox ticket #3946).
                DevActionService.ForgetActionMenu();
                SendStateEvent("game_loaded", new
                {
                    map_count = Find.Maps?.Count ?? 0,
                    permadeath = Current.Game.Info.permadeathMode,
                    storyteller = Find.Storyteller?.def?.defName
                });
            }
        }

        // --- 2. NEW GAME CREATED ---
        [HarmonyPatch(typeof(Game), nameof(Game.InitNewGame))]
        public static class GameNewPatch
        {
            static void Postfix()
            {
                // A new colony was read from no file, so it must not be saved
                // back into the one that was open before (#3603).
                LoadedSave.Clear();
                // A new colony starts at its own tick 0 (fox ticket #3781).
                LoadedTick.Record(Find.TickManager?.TicksGame ?? 0);
                // A new colony is a different game, so the menu the last one
                // walked is dropped (fox ticket #3946).
                DevActionService.ForgetActionMenu();
                // The batch a founding drew from belongs to the world the
                // setup built; the colony's own world replaces it, so its
                // candidates no longer name pawns any later founding could
                // reproduce (fox ticket #4020).
                PawnBatch.Reset();
                SendStateEvent("new_game_created", new
                {
                    scenario = Find.Scenario?.name,
                    storyteller = Find.Storyteller?.def?.defName,
                    difficulty = Find.Storyteller?.difficultyDef?.defName
                });
            }
        }

        // --- 3. EXIT TO MENU ---
        [HarmonyPatch(typeof(GenScene), nameof(GenScene.GoToMainMenu))]
        public static class ExitToMenuPatch
        {
            static void Prefix()
            {
                // Prefix is better here because once GoToMainMenu finishes, the game state is cleared.
                // The colony is gone, so the save it was read from is no
                // longer what a release would write (#3603), and the dev-action
                // menu it walked goes with it (fox ticket #3946).
                LoadedSave.Clear();
                DevActionService.ForgetActionMenu();
                SendStateEvent("exit_to_menu", new
                {
                    reason = "User requested main menu"
                });
            }
        }

        // --- 4. GAME SAVED ---
        [HarmonyPatch(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.SaveGame))]
        public static class GameSavePatch
        {
            static void Postfix(string fileName)
            {
                SendStateEvent("game_saved", new
                {
                    filename = fileName,
                    filesize_est = "unknown" // File info not available in this context easily
                });
            }
        }

        // --- 5. SETTINGS CHANGED ---
        // Prefs.Save() is called whenever standard settings or Mod settings are written to disk.
        [HarmonyPatch(typeof(Prefs), nameof(Prefs.Save))]
        public static class SettingsChangedPatch
        {
            static void Postfix()
            {
                SendStateEvent("settings_changed", new
                {
                    volume_master = Prefs.VolumeMaster,
                    ui_scale = Prefs.UIScale
                });
            }
        }
    }
}