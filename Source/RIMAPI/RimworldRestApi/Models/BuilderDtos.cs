using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // --- Request Objects ---
    public class CopyAreaRequestDto
    {
        public int MapId { get; set; }
        [ParameterDescription("One corner of the area to copy.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the area.")]
        public PositionDto PointB { get; set; }
    }

    public class PasteAreaRequestDto
    {
        public int MapId { get; set; }
        public PositionDto Position { get; set; } // This will be the bottom-left corner
        [ParameterDescription("The blueprint whose cells the call pastes.")]
        public BlueprintDto Blueprint { get; set; }
        [ParameterDescription("When true, blocking things in the way are removed.")]
        public bool ClearObstacles { get; set; } = true; // Destroy existing things before pasting
    }

    public class CheckZoneRequestDto
    {
        public int MapId { get; set; }
        [ParameterDescription("One corner of the area to check.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the area.")]
        public PositionDto PointB { get; set; }
    }

    // --- The Blueprint Data Structure ---
    public class BlueprintDto
    {
        [ParameterDescription("How many cells wide the blueprint is.")]
        public int Width { get; set; }
        [ParameterDescription("How many cells tall the blueprint is.")]
        public int Height { get; set; }
        [ParameterDescription("The terrain cells the blueprint lays.")]
        public List<SavedTerrainDto> Floors { get; set; } = new List<SavedTerrainDto>();
        [ParameterDescription("The buildings one paste of the blueprint places.")]
        public List<SavedBuildingDto> Buildings { get; set; } = new List<SavedBuildingDto>();
    }

    public class SavedTerrainDto
    {
        public string DefName { get; set; }
        [ParameterDescription("The cell's x offset from the blueprint's corner.")]
        public int RelX { get; set; }
        [ParameterDescription("The cell's z offset from the blueprint's corner.")]
        public int RelZ { get; set; }
    }

    public class SavedBuildingDto
    {
        public string DefName { get; set; }
        [ParameterDescription("The material's def name, e.g. \"Steel\".")]
        public string StuffDefName { get; set; } // Material (Wood, Steel, etc)
        [ParameterDescription("The building's x offset from the blueprint's corner.")]
        public int RelX { get; set; }
        [ParameterDescription("The building's z offset from the blueprint's corner.")]
        public int RelZ { get; set; }
        [ParameterDescription("The building's rotation: 0, 90, 180 or 270.")]
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
        [ParameterDescription("How many things the call placed.")]
        public int Placed { get; set; }

        /// <summary>
        /// How many of the placed things went out finished instead of as a
        /// blueprint, because their def takes no construction work (a sleeping
        /// spot). Placed counts them too; this field says so, so the answer
        /// does not promise a plan that will never be listed (fox ticket #3940).
        /// </summary>
        [ParameterDescription("How many were placed finished.")]
        public int Instantly { get; set; }

        [ParameterDescription("How many were dropped as items.")]
        public List<PlaceRefusalDto> Dropped { get; set; } = new List<PlaceRefusalDto>();

        /// <summary>
        /// What the game's own placement removed to make room, the way its
        /// build designator wipes a thing a plan may be built over (a cooler
        /// into a wall takes the wall). Named instead of silent: a run that
        /// ordered one cell must not lose a wall without reading it (fox
        /// ticket #4033).
        /// </summary>
        [ParameterDescription("How many things the call removed.")]
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
        [ParameterDescription("Why the game refused the placement.")]
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
        [ParameterDescription("What asked for the placement.")]
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
        [ParameterDescription("One corner of the area to cancel.")]
        public PositionDto PointA { get; set; }
        [ParameterDescription("The opposite corner of the area.")]
        public PositionDto PointB { get; set; }
    }

    public class CancelResultDto
    {
        /// <summary>How many planned builds were removed (a multi-cell blueprint counts once).</summary>
        [ParameterDescription("How many blueprints the call cancelled.")]
        public int Cancelled { get; set; }

        [ParameterDescription("How many placed things the call removed.")]
        public List<CancelEntryDto> Removed { get; set; } = new List<CancelEntryDto>();

        /// <summary>Every cell of the rectangle that held nothing to cancel, with the reason.</summary>
        [ParameterDescription("How many entries the call skipped.")]
        public List<PlaceRefusalDto> Skipped { get; set; } = new List<PlaceRefusalDto>();
    }

    /// <summary>One planned build the cancel removed: the def, its material, the cell and the kind.</summary>
    public class CancelEntryDto
    {
        public string DefName { get; set; }

        /// <summary>The material's defName, null when the def takes none.</summary>
        [ParameterDescription("The material the entry was built from.")]
        public string Stuff { get; set; }

        /// <summary>"blueprint" (nothing built yet) or "frame" (started).</summary>
        [ParameterDescription("What the entry was: blueprint, frame or building.")]
        public string Kind { get; set; }

        public int X { get; set; }

        public int Z { get; set; }
    }

    public class CheckZoneResultDto
    {
        [ParameterDescription("Whether the zone can be built as planned.")]
        public bool CanBuild { get; set; }
        [ParameterDescription("The problems the check found with the plan.")]
        public CheckZoneIssuesDto Issues { get; set; }
    }

    public class CheckZoneIssuesDto
    {
        [ParameterDescription("The cells whose terrain blocks the plan.")]
        public List<CheckZoneIssueDto> Terrain { get; set; } = new List<CheckZoneIssueDto>();
        [ParameterDescription("The ore the plan would cover.")]
        public List<CheckZoneIssueDto> Ores { get; set; } = new List<CheckZoneIssueDto>();
        [ParameterDescription("The buildings that stand in the way.")]
        public List<CheckZoneIssueDto> Buildings { get; set; } = new List<CheckZoneIssueDto>();
        [ParameterDescription("The zones that lie in the way.")]
        public List<CheckZoneIssueDto> Zones { get; set; } = new List<CheckZoneIssueDto>();
    }

    public class CheckZoneIssueDto
    {
        public int X { get; set; }
        public int Z { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The zone's type, e.g. stockpile or growing.")]
        public string ZoneType { get; set; }
    }
}