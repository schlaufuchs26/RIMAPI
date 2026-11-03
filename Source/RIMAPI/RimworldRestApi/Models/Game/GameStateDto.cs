using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class GameStateDto
    {
        [ParameterDescription("The game tick the answer was built on.")]
        public int GameTick { get; set; }
        [ParameterDescription("The colony's wealth in silver.")]
        public float ColonyWealth { get; set; }
        [ParameterDescription("How many free colonists the colony has.")]
        public int ColonistCount { get; set; }
        [ParameterDescription("The storyteller's def name.")]
        public string Storyteller { get; set; }
        [ParameterDescription("Whether the game is paused.")]
        public bool IsPaused { get; set; }
        [ParameterDescription("Where the game stands: menu, loading or playing.")]
        public string ProgramState { get; set; }
        [ParameterDescription("The screen the player looks at.")]
        public string CurrentView { get; set; }
        [ParameterDescription("Whether the options dialog is open.")]
        public bool IsSettingsOpen { get; set; }
        [ParameterDescription("Whether the mod settings dialog is open.")]
        public bool IsModSettingsOpen { get; set; }
        [ParameterDescription("How many maps the game holds.")]
        public int MapCount { get; set; }

        // What is loaded (#3098). A caller that counts colonists cannot tell
        // the title screen from a colony whose colonists all died: both report
        // zero. These are the game's own flags.
        [ParameterDescription("What is loaded (#3098).")]
        public bool HasGame { get; set; }   // Current.Game != null
        [ParameterDescription("Whether a map is loaded.")]
        public bool HasMap { get; set; }    // Find.CurrentMap != null
        [ParameterDescription("Whether the game is loading right now.")]
        public bool IsLoading { get; set; } // a long event is running: reading a save, generating a world
        [ParameterDescription("The name the player gave the colony.")]
        public string ColonyName { get; set; } // the player faction's name, the one the game shows

        // Which file a save writes (fox #3603). LoadedSave is the save the
        // running colony was read from; SaveName is the name the game's next
        // save writes, which in commitment mode is the colony's own unique
        // name whatever the caller asks for (Helpers/SaveNameRules.cs). A
        // caller that saves the colony must write it back into LoadedSave,
        // otherwise a probe copy of the live colony overwrites the live save.
        [ParameterDescription("Which file a save writes (fox #3603).")]
        public string LoadedSave { get; set; }
        [ParameterDescription("The save the colony was loaded from.")]
        public string SaveName { get; set; }

        // Why the clock stands still (fox ticket #3490). Paused is one boolean
        // over several causes: TimeSpeed is the game's own speed switch
        // ("Paused", "Normal", "Fast", ...), and PauseSource names the hold
        // that forced the pause while the switch is not Paused
        // (Helpers/PauseSource.cs). Both are empty when nothing holds it, so a
        // caller can tell "a save is running" from "a letter paused the game".
        [ParameterDescription("Why the clock stands still (fox ticket #3490).")]
        public string TimeSpeed { get; set; }
        [ParameterDescription("What paused the game, e.g. a letter or the player.")]
        public string PauseSource { get; set; }

        // The current map's clock offset in ticks (fox ticket #3412): the game
        // adds the tile's whole-hour time zone to its own start tick, so a
        // second settlement on another longitude reads hours off from the
        // founding map. Null when the read could not name a map.
        [ParameterDescription("The current map's clock offset in ticks (fox ticket #3412): the game adds the tile's whole-hour time zone to its own start tick, so a second settlement on another longitude reads hours off from...")]
        public int? ClockOffsetTicks { get; set; }

        // The scenario the colony was founded under (ticket #3930), so a run
        // can tell Crashlanded from Lost Tribe without remembering the call.
        [ParameterDescription("The scenario the colony was founded under (ticket #3930), so a run can tell Crashlanded from Lost Tribe without remembering the call.")]
        public string Scenario { get; set; }
        [ParameterDescription("The scenario's name, as the game shows it.")]
        public string ScenarioLabel { get; set; }

        // What a founding installed from a candidate batch (ticket #3924):
        // the picked candidates as "#7 Mia (34), #11 Tam (31)", or why the
        // pick was refused and the scenario's own random colonists stayed.
        // Empty on a founding that named no picks.
        [ParameterDescription("What a founding installed from a candidate batch (ticket #3924): the picked candidates as \"#7 Mia (34), #11 Tam (31)\", or why the pick was refused and the scenario's own random colonists stayed.")]
        public string PickedPawns { get; set; }
        [ParameterDescription("Why the game cannot start, when it cannot.")]
        public string PickProblem { get; set; }
    }

    public class NewGameStartRequestDto
    {
        [ParameterDescription("The storyteller to start with, by def name.")]
        public string StorytellerName { get; set; }
        [ParameterDescription("The difficulty to start on, by def name.")]
        public string DifficultyName { get; set; }
        [ParameterDescription("How large the starting map is, in cells.")]
        public int MapSize { get; set; }
        [ParameterDescription("Whether the colony runs in permadeath.")]
        public bool Permadeath { get; set; }
        [ParameterDescription("How much of the planet the world covers, 0 to 1.")]
        public float PlanetCoverage { get; set; }
        [ParameterDescription("The seed the world is generated from.")]
        public string WorldSeed { get; set; }
        [ParameterDescription("The world tile to land on.")]
        public string StartingTile { get; set; }
        [ParameterDescription("The season to start in.")]
        public string StartingSeason { get; set; }
        [ParameterDescription("How much rain the planet gets overall.")]
        public int OverallRainfall { get; set; }
        [ParameterDescription("How warm the planet is overall.")]
        public int OverallTemperature { get; set; }
        [ParameterDescription("How populated the planet is.")]
        public int OverallPopulation { get; set; }
        [ParameterDescription("How many landmarks the planet holds.")]
        public int LandmarkDensity { get; set; }

        // IdeoName picks a saved ideoligion by its name; empty builds the
        // fox default (Helpers/IdeoSetup.cs, ticket #3612).
        [ParameterDescription("IdeoName picks a saved ideoligion by its name; empty builds the fox default (Helpers/IdeoSetup.cs, ticket #3612).")]
        public string IdeoName { get; set; }

        // NewIdeoName names the ideoligion this call builds (fox ticket
        // #3929). Empty leaves the game's generated name in place, and it is
        // refused together with IdeoName: a saved ideoligion keeps its own
        // name. GET /api/v1/ideo reads back what the colony ended up with.
        [ParameterDescription("NewIdeoName names the ideoligion this call builds (fox ticket #3929).")]
        public string NewIdeoName { get; set; }

        // IdeoMeme is the one normal meme a freshly built ideo carries
        // (ticket #3927); empty keeps Individualist, which forces no precept.
        // The structure meme stays Ideological, because a fluid ideo needs one.
        [ParameterDescription("IdeoMeme is the one normal meme a freshly built ideo carries (ticket #3927); empty keeps Individualist, which forces no precept.")]
        public string IdeoMeme { get; set; }

        // IdeoPrecepts are the precepts a freshly built ideo carries, by
        // defName or by the "Issue: label" the ideoligion window shows
        // (ticket #3927). Empty keeps the fox default set.
        [ParameterDescription("IdeoPrecepts are the precepts a freshly built ideo carries, by defName or by the \"Issue: label\" the ideoligion window shows (ticket #3927).")]
        public List<string> IdeoPrecepts { get; set; }

        // Scenario names the starting scenario by defName or label (ticket
        // #3930); empty keeps the game's Crashlanded default. The list the
        // installed game offers is GET /api/v1/game/scenarios.
        [ParameterDescription("Scenario names the starting scenario by defName or label (ticket #3930); empty keeps the game's Crashlanded default.")]
        public string Scenario { get; set; }

        // The site wish (ticket #3930): what the starting tile should look
        // like. Every field is a preference, not a filter; the picker takes
        // the closest tile and names what it gave up. With every field empty
        // the landing site stays random, as before.
        [ParameterDescription("The site wish (ticket #3930): what the starting tile should look like.")]
        public List<string> SiteBiomes { get; set; }
        [ParameterDescription("How hilly the landing site must be.")]
        public List<string> SiteHilliness { get; set; }
        [ParameterDescription("The coldest the landing site may be, in Celsius.")]
        public float? SiteTemperatureMin { get; set; }
        [ParameterDescription("The warmest it may be, in Celsius.")]
        public float? SiteTemperatureMax { get; set; }
        [ParameterDescription("The fewest growing days the site needs.")]
        public int SiteGrowingDaysMin { get; set; }
        [ParameterDescription("Whether the site needs a road.")]
        public bool SiteRoad { get; set; }
        [ParameterDescription("Whether the site needs a river.")]
        public bool SiteRiver { get; set; }
        // SiteSettlementsMin other factions' settlements within
        // SiteSettlementRadius tiles: neighbours worth trading with or raiding.
        [ParameterDescription("SiteSettlementsMin other factions' settlements within SiteSettlementRadius tiles: neighbours worth trading with or raiding.")]
        public int SiteSettlementsMin { get; set; }
        [ParameterDescription("How far settlements must stay from the site.")]
        public int SiteSettlementRadius { get; set; }
        // SiteVary takes the Nth best tile instead of the best, so a run can
        // found a deliberately different colony on purpose.
        [ParameterDescription("SiteVary takes the Nth best tile instead of the best, so a run can found a deliberately different colony on purpose.")]
        public int SiteVary { get; set; }

        // CandidateCount is how many candidates a batch call draws (ticket
        // #3924); StartingPawns is the pick a founding installs, 1-based ids
        // from the batch answer. The candidates are rebuilt from the world
        // seed, so the ids stay valid across the two calls.
        [ParameterDescription("CandidateCount is how many candidates a batch call draws (ticket #3924); StartingPawns is the pick a founding installs, 1-based ids from the batch answer.")]
        public int CandidateCount { get; set; }
        [ParameterDescription("The pawns the colony starts with.")]
        public List<int> StartingPawns { get; set; }
    }

    public class GameSettingsDto
    {
        // --- General / System ---
        [ParameterDescription("The language the game's text shows in.")]
        public string Language { get; set; }
        [ParameterDescription("Whether the game keeps running unfocused.")]
        public bool RunInBackground { get; set; }
        [ParameterDescription("Whether dev mode is on.")]
        public bool DevelopmentMode { get; set; }
        [ParameterDescription("Whether the log records everything.")]
        public bool LogVerbose { get; set; }        // If verbose logging is enabled
        [ParameterDescription("Whether temperatures show in Celsius or Fahrenheit.")]
        public string TemperatureMode { get; set; } // "Celsius", "Fahrenheit", "Kelvin"
        [ParameterDescription("How many days between autosaves.")]
        public bool AutosaveInterval { get; set; }  // Usually in days (float or int representation)

        // --- Graphics / Display ---
        [ParameterDescription("The screen resolution the game runs at.")]
        public string Resolution { get; set; }      // Format: "1920x1080"
        [ParameterDescription("Whether the game runs fullscreen.")]
        public bool Fullscreen { get; set; }
        [ParameterDescription("How large the UI is drawn.")]
        public float UserInterfaceScale { get; set; } // Changed to float (e.g., 1.0, 1.25)
        [ParameterDescription("Whether the custom cursor is on.")]
        public bool CustomCursorEnabled { get; set; }
        [ParameterDescription("Whether pawns wear hats only on the map.")]
        public bool HatsOnlyOnMap { get; set; }     // "Show hats only on map" setting
        [ParameterDescription("Whether plants sway in the wind.")]
        public bool PlantWindSway { get; set; }
        [ParameterDescription("How many settlements the player may found.")]
        public int MaxNumberOfPlayerSettlements { get; set; } // Default is 1, up to 5

        // --- Audio (Volumes 0.0 to 1.0) ---
        [ParameterDescription("--- Audio (Volumes 0.0 to 1.0) ---")]
        public float VolumeMaster { get; set; }
        [ParameterDescription("The game sound volume, 0 to 1.")]
        public float VolumeGame { get; set; }
        [ParameterDescription("The music volume, 0 to 1.")]
        public float VolumeMusic { get; set; }
        [ParameterDescription("The ambient sound volume, 0 to 1.")]
        public float VolumeAmbient { get; set; }

        // --- Gameplay / Controls ---
        [ParameterDescription("Whether the game pauses once a save has loaded.")]
        public bool PauseOnLoad { get; set; }
        [ParameterDescription("Whether an urgent letter pauses the game.")]
        public string PauseOnUrgentLetter { get; set; }
        [ParameterDescription("Whether any letter pauses the game.")]
        public bool AutomaticPauseOnLetter { get; set; } // Any letter? (Check nuances)
        // Manual work priorities: while off, the game treats every active work
        // type as 3 and a pawn's stored level orders nothing.
        [ParameterDescription("Manual work priorities: while off, the game treats every active work type as 3 and a pawn's stored level orders nothing.")]
        public bool ManualPriorities { get; set; }
        [ParameterDescription("Whether the camera scrolls at the screen edge.")]
        public bool EdgeScreenScroll { get; set; }
        [ParameterDescription("How fast dragging moves the map.")]
        public float MapDragSensitivity { get; set; }
        [ParameterDescription("Whether zooming follows the mouse.")]
        public bool ZoomToMouse { get; set; }

        // --- Interface / HUD ---
        [ParameterDescription("Whether the clock shows the real time of day.")]
        public bool ShowRealtimeClock { get; set; }
        [ParameterDescription("Whether the resource readout groups by category.")]
        public bool ResourceReadoutCategorized { get; set; } // The tree view on the left
        [ParameterDescription("Whether animals show their names.")]
        public bool ShowAnimalNames { get; set; }

        // --- Modding (Optional) ---
        [ParameterDescription("Whether the mod list resets after a crash.")]
        public bool ResetModsConfigOnCrash { get; set; }
        [ParameterDescription("Whether textures are compressed.")]
        public int TextureCompression { get; set; } // Often hidden but accessible
    }
}
