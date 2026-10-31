

using System;
using RIMAPI.Models;
using RimWorld;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace RIMAPI.Helpers
{
    public static class GamePlayHelper
    {
        public static void InitGameFromConfiguration(NewGameStartRequestDto request)
        {
            StorytellerDef storytellerDef = DefDatabase<StorytellerDef>.GetNamed(request.StorytellerName);
            DifficultyDef difficultyDef = DefDatabase<DifficultyDef>.GetNamed(request.DifficultyName);

            string seed = string.IsNullOrEmpty(request.WorldSeed) ? GenText.RandomSeedString() : request.WorldSeed.ToLower();

            Current.ProgramState = ProgramState.Entry;
            Game.ClearCaches();
            Current.Game = new Game();
            Current.Game.InitData = new GameInitData();
            Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
            Find.Scenario.PreConfigure();
            Current.Game.storyteller = new Storyteller(storytellerDef, difficultyDef);
            GameInitData initData = Find.GameInitData;

            Current.Game.World = WorldGenerator.GenerateWorld(Mathf.Clamp(request.PlanetCoverage, 0, 1), seed, (OverallRainfall)request.OverallRainfall, (OverallTemperature)request.OverallTemperature, (OverallPopulation)request.OverallPopulation, (LandmarkDensity)request.LandmarkDensity);
            if (!string.IsNullOrEmpty(request.StartingTile) && request.StartingTile != "auto")
            {
                if (int.TryParse(request.StartingTile, out int tileId))
                {
                    if (tileId < 0 || tileId >= Find.WorldGrid.TilesCount)
                    {
                        Log.Error($"[RIMAPI] Requested tile {tileId} is out of bounds.");
                        throw new Exception("Tile ID value is out of boundaries");
                    }
                    initData.startingTile = Find.WorldGrid[tileId].tile;
                }
                else
                {
                    Log.Error($"[RIMAPI] Could not parse tile ID '{request.StartingTile}'.");
                    throw new Exception("Could not parse tile ID");
                }
            }
            else
            {
                initData.ChooseRandomStartingTile();
            }

            if (!string.IsNullOrEmpty(request.StartingTile) && request.StartingTile != "auto")
            {
                initData.startingSeason = (Season)int.Parse(request.StartingSeason);
            }
            else
            {
                initData.startingSeason = Season.Spring;
            }

            initData.mapSize = request.MapSize;
            initData.permadeath = request.Permadeath;
            Find.Scenario.PostIdeoChosen();
        }

        public static GameStateDto GetGameStateDto()
        {
            var state = new GameStateDto();

            try
            {
                // First check if we can access basic game state
                state.IsLoading = LongEventHandler.AnyEventNowOrWaiting;

                if (Current.Game == null || Find.Maps == null)
                {
                    // Game is not fully initialized
                    state.ProgramState = "NotInitialized";
                    state.GameTick = 0;
                    state.Storyteller = "Unknown";
                    state.MapCount = 0;
                    state.CurrentView = "None";
                    state.IsPaused = true;
                    state.HasGame = false;
                    state.HasMap = false;
                    state.ColonyName = "";
                    return state;
                }

                state.GameTick = Find.TickManager?.TicksGame ?? 0;
                state.Storyteller = Current.Game.storyteller?.def?.defName ?? "Unknown";
                state.MapCount = Find.Maps.Count;
                state.ProgramState = Current.ProgramState.ToString();
                // #3098: the flags that say what is loaded, instead of a
                // colonist count a caller would have to interpret.
                state.HasGame = true;
                state.HasMap = Find.CurrentMap != null;
                try
                {
                    state.ColonyName = Faction.OfPlayer?.Name ?? "";
                }
                catch
                {
                    state.ColonyName = "";
                }
                // #3412: the clock is the map's, not the colony's. The game
                // adds the tile's whole-hour time zone to its start tick, so
                // report the offset this map runs at instead of letting a
                // caller assume the founding map's 06:00.
                try
                {
                    Map map = Find.CurrentMap;
                    if (map != null && Find.WorldGrid != null && Find.TickManager != null)
                    {
                        var longLat = Find.WorldGrid.LongLatOf(map.Tile);
                        state.ClockOffsetTicks = ClockOffset.From(
                            Find.TickManager.gameStartAbsTick,
                            GenDate.TimeZoneAt(longLat.x));
                    }
                }
                catch
                {
                    state.ClockOffsetTicks = null;
                }
            }
            catch (Exception)
            {
                // If any of the above fail, we're in an unstable state
                state.ProgramState = "Error";
                state.GameTick = 0;
                state.Storyteller = "Unknown";
                state.MapCount = 0;
                state.CurrentView = "Error";
                state.IsPaused = true;
                // Consider logging the exception if you have a logger
                // Log.Error($"Error getting game state: {ex.Message}");
                return state;
            }

            try
            {
                state.ColonyWealth = Find.CurrentMap?.wealthWatcher?.WealthTotal ?? 0;
            }
            catch
            {
                state.ColonyWealth = 0;
            }

            try
            {
                state.ColonistCount = Find.CurrentMap?.mapPawns?.FreeColonistsCount ?? 0;
            }
            catch
            {
                state.ColonistCount = 0;
            }

            // Set view state
            if (Current.ProgramState == ProgramState.Playing)
            {
                try
                {
                    if (WorldRendererUtility.WorldRendered)
                    {
                        state.CurrentView = "World";
                    }
                    else if (Find.CurrentMap != null)
                    {
                        state.CurrentView = "Map";
                    }
                    else
                    {
                        state.CurrentView = "Unknown";
                    }
                    state.IsPaused = Find.TickManager?.Paused ?? true;
                }
                catch
                {
                    state.CurrentView = "Error";
                    state.IsPaused = true;
                }
            }
            else
            {
                state.CurrentView = "Menu";
                state.IsPaused = true;
            }

            // Why the clock stands still, from the game's own flags (fox ticket
            // #3490): the speed switch and the holds of TickManager.ForcePaused,
            // so a caller that sees a pause names it instead of guessing "an
            // alert, a modal or a save". The naming rule is PauseSource.Describe,
            // which carries the unit tests.
            try
            {
                state.TimeSpeed = Find.TickManager != null ? Find.TickManager.CurTimeSpeed.ToString() : "";
                bool longEvent = LongEventHandler.ForcePause;
                bool window = Find.WindowStack != null && Find.WindowStack.WindowsForcePause;
                bool tilePicker = Find.TilePicker != null && Find.TilePicker.Active;
                bool forcePaused = Find.TickManager != null && Find.TickManager.ForcePaused;
                state.PauseSource = PauseSource.Describe(longEvent, window, tilePicker, MapGenerator.debugMode, forcePaused);
            }
            catch (Exception)
            {
                state.TimeSpeed = "";
                state.PauseSource = "";
            }

            // Check for open windows
            if (Find.WindowStack != null)
            {
                try
                {
                    // Check for standard Options/Pause menu
                    state.IsSettingsOpen = Find.WindowStack.IsOpen(typeof(Dialog_Options));

                    // Check for Mod Settings
                    state.IsModSettingsOpen = Find.WindowStack.IsOpen(typeof(Dialog_ModSettings));
                }
                catch
                {
                    state.IsSettingsOpen = false;
                    state.IsModSettingsOpen = false;
                }
            }
            else
            {
                state.IsSettingsOpen = false;
                state.IsModSettingsOpen = false;
            }

            return state;
        }

        public static GameSettingsDto GetCurrentSettings()
        {
            return new GameSettingsDto
            {
                // --- General ---
                Language = Prefs.LangFolderName,
                RunInBackground = Prefs.RunInBackground,
                DevelopmentMode = Prefs.DevMode,
                LogVerbose = Prefs.LogVerbose,
                TemperatureMode = Prefs.TemperatureMode.ToString(),
                // Autosave is technically stored as a float of "days"
                AutosaveInterval = Prefs.AutosaveIntervalDays > 0,

                // --- Graphics ---
                Resolution = $"{Screen.width}x{Screen.height}",
                Fullscreen = Screen.fullScreen,
                UserInterfaceScale = Prefs.UIScale,
                CustomCursorEnabled = Prefs.CustomCursorEnabled,
                HatsOnlyOnMap = Prefs.HatsOnlyOnMap,
                PlantWindSway = Prefs.PlantWindSway,
                MaxNumberOfPlayerSettlements = Prefs.MaxNumberOfPlayerSettlements,

                // --- Audio ---
                VolumeMaster = Prefs.VolumeMaster,
                VolumeGame = Prefs.VolumeGame,
                VolumeMusic = Prefs.VolumeMusic,
                VolumeAmbient = Prefs.VolumeAmbient,

                // --- Gameplay ---
                PauseOnLoad = Prefs.PauseOnLoad,
                PauseOnUrgentLetter = Prefs.AutomaticPauseMode.ToString(),
                ManualPriorities = Find.PlaySettings?.useWorkPriorities ?? false,
                EdgeScreenScroll = Prefs.EdgeScreenScroll,
                MapDragSensitivity = Prefs.MapDragSensitivity,
                ZoomToMouse = Prefs.ZoomToMouse,

                // --- Interface ---
                ShowRealtimeClock = Prefs.ShowRealtimeClock,
                ResourceReadoutCategorized = Prefs.ResourceReadoutCategorized,
                ShowAnimalNames = Prefs.AnimalNameMode != AnimalNameDisplayMode.None,
            };
        }
    }

}