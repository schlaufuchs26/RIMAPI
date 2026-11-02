using System.Collections.Generic;

namespace RIMAPI.Models
{
    // --- Request Objects ---
    public class CopyAreaRequestDto
    {
        public int MapId { get; set; }
        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }
    }

    public class PasteAreaRequestDto
    {
        public int MapId { get; set; }
        public PositionDto Position { get; set; } // This will be the bottom-left corner
        public BlueprintDto Blueprint { get; set; }
        public bool ClearObstacles { get; set; } = true; // Destroy existing things before pasting
    }

    public class CheckZoneRequestDto
    {
        public int MapId { get; set; }
        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }
    }

    // --- The Blueprint Data Structure ---
    public class BlueprintDto
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public List<SavedTerrainDto> Floors { get; set; } = new List<SavedTerrainDto>();
        public List<SavedBuildingDto> Buildings { get; set; } = new List<SavedBuildingDto>();
    }

    public class SavedTerrainDto
    {
        public string DefName { get; set; }
        public int RelX { get; set; }
        public int RelZ { get; set; }
    }

    public class SavedBuildingDto
    {
        public string DefName { get; set; }
        public string StuffDefName { get; set; } // Material (Wood, Steel, etc)
        public int RelX { get; set; }
        public int RelZ { get; set; }
        public int Rotation { get; set; } // 0=North, 1=East, 2=South, 3=West
    }

    // --- What a build call answers (fox ticket #3089) ---

    /// <summary>
    /// The result of placing a layout: how many blueprints went out, and every
    /// cell the game refused with its own reason. The handler used to answer a
    /// bare success, so a cell that could not take a blueprint (marsh, a wall,
    /// an unreachable spot) disappeared without a word.
    /// </summary>
    public class PlaceResultDto
    {
        public int Placed { get; set; }

        /// <summary>
        /// How many of the placed things went out finished instead of as a
        /// blueprint, because their def takes no construction work (a sleeping
        /// spot). Placed counts them too; this field says so, so the answer
        /// does not promise a plan that will never be listed (fox ticket #3940).
        /// </summary>
        public int Instantly { get; set; }

        public List<PlaceRefusalDto> Dropped { get; set; } = new List<PlaceRefusalDto>();

        /// <summary>
        /// What the game's own placement removed to make room, the way its
        /// build designator wipes a thing a plan may be built over (a cooler
        /// into a wall takes the wall). Named instead of silent: a run that
        /// ordered one cell must not lose a wall without reading it (fox
        /// ticket #4033).
        /// </summary>
        public List<WipedThingDto> Wiped { get; set; } = new List<WipedThingDto>();
    }

    /// <summary>One thing the game's placement rule removed for a blueprint.</summary>
    public class WipedThingDto
    {
        public string DefName { get; set; }

        public string Label { get; set; }

        public int X { get; set; }

        public int Z { get; set; }
    }

    /// <summary>One blueprint cell the game would not place, and why.</summary>
    public class PlaceRefusalDto
    {
        public string DefName { get; set; }

        public int X { get; set; }

        public int Z { get; set; }

        /// <summary>The game's own acceptance reason, e.g. "CannotPlaceOnTerrain".</summary>
        public string Reason { get; set; }

        /// <summary>
        /// Where the refusal came from, so a caller can tell the game's own
        /// placement verdict from a def-level refusal (fox ticket #3264):
        /// "placement" (GenConstruct.CanPlaceBlueprintAt refused the cell),
        /// "no_blueprint" (the def has no blueprint at all, e.g. a sculpture),
        /// "bounds" (the cell lies outside the map), "unknown_def" (the name
        /// resolves to nothing) or "exception" (a step threw). Empty in a cancel
        /// answer, where nothing was placed to begin with.
        /// </summary>
        public string Source { get; set; }
    }

    /// <summary>
    /// Cancel every blueprint and frame inside a rectangle: the game's own
    /// cancel designator, applied to the whole area at once (fox ticket #3133).
    /// A blueprint whose material never arrives used to be stuck: it could only
    /// be overwritten cell by cell, and a wrong cell could not be removed.
    /// </summary>
    public class CancelBlueprintsRequestDto
    {
        public int MapId { get; set; }
        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }
    }

    public class CancelResultDto
    {
        /// <summary>How many planned builds were removed (a multi-cell blueprint counts once).</summary>
        public int Cancelled { get; set; }

        public List<CancelEntryDto> Removed { get; set; } = new List<CancelEntryDto>();

        /// <summary>Every cell of the rectangle that held nothing to cancel, with the reason.</summary>
        public List<PlaceRefusalDto> Skipped { get; set; } = new List<PlaceRefusalDto>();
    }

    /// <summary>One planned build the cancel removed: the def, its material, the cell and the kind.</summary>
    public class CancelEntryDto
    {
        public string DefName { get; set; }

        /// <summary>The material's defName, null when the def takes none.</summary>
        public string Stuff { get; set; }

        /// <summary>"blueprint" (nothing built yet) or "frame" (started).</summary>
        public string Kind { get; set; }

        public int X { get; set; }

        public int Z { get; set; }
    }

    public class CheckZoneResultDto
    {
        public bool CanBuild { get; set; }
        public CheckZoneIssuesDto Issues { get; set; }
    }

    public class CheckZoneIssuesDto
    {
        public List<CheckZoneIssueDto> Terrain { get; set; } = new List<CheckZoneIssueDto>();
        public List<CheckZoneIssueDto> Ores { get; set; } = new List<CheckZoneIssueDto>();
        public List<CheckZoneIssueDto> Buildings { get; set; } = new List<CheckZoneIssueDto>();
        public List<CheckZoneIssueDto> Zones { get; set; } = new List<CheckZoneIssueDto>();
    }

    public class CheckZoneIssueDto
    {
        public int X { get; set; }
        public int Z { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        public string ZoneType { get; set; }
    }
}