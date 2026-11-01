using System.Collections.Generic;

namespace RIMAPI.Models
{
    public class GameStateDto
    {
        public int GameTick { get; set; }
        public float ColonyWealth { get; set; }
        public int ColonistCount { get; set; }
        public string Storyteller { get; set; }
        public bool IsPaused { get; set; }
        public string ProgramState { get; set; }
        public string CurrentView { get; set; }
        public bool IsSettingsOpen { get; set; }
        public bool IsModSettingsOpen { get; set; }
        public int MapCount { get; set; }

        // What is loaded (#3098). A caller that counts colonists cannot tell
        // the title screen from a colony whose colonists all died: both report
        // zero. These are the game's own flags.
        public bool HasGame { get; set; }   // Current.Game != null
        public bool HasMap { get; set; }    // Find.CurrentMap != null
        public bool IsLoading { get; set; } // a long event is running: reading a save, generating a world
        public string ColonyName { get; set; } // the player faction's name, the one the game shows

        // Which file a save writes (fox #3603). LoadedSave is the save the
        // running colony was read from; SaveName is the name the game's next
        // save writes, which in commitment mode is the colony's own unique
        // name whatever the caller asks for (Helpers/SaveNameRules.cs). A
        // caller that saves the colony must write it back into LoadedSave,
        // otherwise a probe copy of the live colony overwrites the live save.
        public string LoadedSave { get; set; }
        public string SaveName { get; set; }

        // Why the clock stands still (fox ticket #3490). Paused is one boolean
        // over several causes: TimeSpeed is the game's own speed switch
        // ("Paused", "Normal", "Fast", ...), and PauseSource names the hold
        // that forced the pause while the switch is not Paused
        // (Helpers/PauseSource.cs). Both are empty when nothing holds it, so a
        // caller can tell "a save is running" from "a letter paused the game".
        public string TimeSpeed { get; set; }
        public string PauseSource { get; set; }

        // The current map's clock offset in ticks (fox ticket #3412): the game
        // adds the tile's whole-hour time zone to its own start tick, so a
        // second settlement on another longitude reads hours off from the
        // founding map. Null when the read could not name a map.
        public int? ClockOffsetTicks { get; set; }

        // The scenario the colony was founded under (ticket #3930), so a run
        // can tell Crashlanded from Lost Tribe without remembering the call.
        public string Scenario { get; set; }
        public string ScenarioLabel { get; set; }
    }

    public class NewGameStartRequestDto
    {
        public string StorytellerName { get; set; }
        public string DifficultyName { get; set; }
        public int MapSize { get; set; }
        public bool Permadeath { get; set; }
        public float PlanetCoverage { get; set; }
        public string WorldSeed { get; set; }
        public string StartingTile { get; set; }
        public string StartingSeason { get; set; }
        public int OverallRainfall { get; set; }
        public int OverallTemperature { get; set; }
        public int OverallPopulation { get; set; }
        public int LandmarkDensity { get; set; }

        // IdeoName picks a saved ideoligion by its name; empty builds the
        // fox default (Helpers/IdeoSetup.cs, ticket #3612).
        public string IdeoName { get; set; }

        // Scenario names the starting scenario by defName or label (ticket
        // #3930); empty keeps the game's Crashlanded default. The list the
        // installed game offers is GET /api/v1/game/scenarios.
        public string Scenario { get; set; }

        // The site wish (ticket #3930): what the starting tile should look
        // like. Every field is a preference, not a filter; the picker takes
        // the closest tile and names what it gave up. With every field empty
        // the landing site stays random, as before.
        public List<string> SiteBiomes { get; set; }
        public List<string> SiteHilliness { get; set; }
        public float? SiteTemperatureMin { get; set; }
        public float? SiteTemperatureMax { get; set; }
        public int SiteGrowingDaysMin { get; set; }
        public bool SiteRoad { get; set; }
        public bool SiteRiver { get; set; }
        // SiteSettlementsMin other factions' settlements within
        // SiteSettlementRadius tiles: neighbours worth trading with or raiding.
        public int SiteSettlementsMin { get; set; }
        public int SiteSettlementRadius { get; set; }
        // SiteVary takes the Nth best tile instead of the best, so a run can
        // found a deliberately different colony on purpose.
        public int SiteVary { get; set; }
    }

    public class GameSettingsDto
    {
        // --- General / System ---
        public string Language { get; set; }
        public bool RunInBackground { get; set; }
        public bool DevelopmentMode { get; set; }
        public bool LogVerbose { get; set; }        // If verbose logging is enabled
        public string TemperatureMode { get; set; } // "Celsius", "Fahrenheit", "Kelvin"
        public bool AutosaveInterval { get; set; }  // Usually in days (float or int representation)

        // --- Graphics / Display ---
        public string Resolution { get; set; }      // Format: "1920x1080"
        public bool Fullscreen { get; set; }
        public float UserInterfaceScale { get; set; } // Changed to float (e.g., 1.0, 1.25)
        public bool CustomCursorEnabled { get; set; }
        public bool HatsOnlyOnMap { get; set; }     // "Show hats only on map" setting
        public bool PlantWindSway { get; set; }
        public int MaxNumberOfPlayerSettlements { get; set; } // Default is 1, up to 5

        // --- Audio (Volumes 0.0 to 1.0) ---
        public float VolumeMaster { get; set; }
        public float VolumeGame { get; set; }
        public float VolumeMusic { get; set; }
        public float VolumeAmbient { get; set; }

        // --- Gameplay / Controls ---
        public bool PauseOnLoad { get; set; }
        public string PauseOnUrgentLetter { get; set; }
        public bool AutomaticPauseOnLetter { get; set; } // Any letter? (Check nuances)
        // Manual work priorities: while off, the game treats every active work
        // type as 3 and a pawn's stored level orders nothing.
        public bool ManualPriorities { get; set; }
        public bool EdgeScreenScroll { get; set; }
        public float MapDragSensitivity { get; set; }
        public bool ZoomToMouse { get; set; }

        // --- Interface / HUD ---
        public bool ShowRealtimeClock { get; set; }
        public bool ResourceReadoutCategorized { get; set; } // The tree view on the left
        public bool ShowAnimalNames { get; set; }

        // --- Modding (Optional) ---
        public bool ResetModsConfigOnCrash { get; set; }
        public int TextureCompression { get; set; } // Often hidden but accessible
    }
}
