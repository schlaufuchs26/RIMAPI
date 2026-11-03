using System.Collections.Generic;

namespace RIMAPI.Models.Map
{
    // The writes the zone and area panels offer (fox ticket #4316, parity wave
    // 6): a growing zone's crop and its two toggles, a zone's name, and the
    // Manage areas dialog's New area, Rename, Invert, Shrink and Delete.

    /// <summary>
    /// The crops a growing zone's Plant button offers here, with the zone's
    /// current crop so a caller can tell a change from a re-selection.
    /// </summary>
    public class GrowingZonePlantsDto
    {
        public int ZoneId { get; set; }
        public string PlantDefName { get; set; }
        public List<SelectablePlantDto> Plants { get; set; }
    }

    public class SelectablePlantDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }

        /// <summary>
        /// The Plants skill a colonist needs before the game lets it sow this
        /// crop; 0 for a crop anyone can sow. The game's Plant menu prints it
        /// beside the name and warns when nobody in the colony reaches it.
        /// </summary>
        public int SowMinSkill { get; set; }

        /// <summary>The game's plant purpose (Food, Health, Beauty, ...).</summary>
        public string Purpose { get; set; }
    }

    /// <summary>
    /// One growing-zone write. Every field is optional except map_id and
    /// zone_id, and a body that names none of them is refused instead of
    /// answering "success" for a zone that did not change.
    /// </summary>
    public class UpdateGrowingZoneRequestDto
    {
        public int MapId { get; set; }
        public int ZoneId { get; set; }

        /// <summary>The crop to grow; must be one the zone's Plant button offers.</summary>
        public string PlantDef { get; set; }

        /// <summary>The Sow toggle: whether the zone's cells are sown at all.</summary>
        public bool? AllowSow { get; set; }

        /// <summary>The Cut toggle: whether plants in the zone may be cut.</summary>
        public bool? AllowCut { get; set; }
    }

    public class RenameZoneRequestDto
    {
        public int MapId { get; set; }
        public int ZoneId { get; set; }

        /// <summary>The new name, as the rename dialog takes it.</summary>
        public string Label { get; set; }
    }

    /// <summary>
    /// The Manage areas dialog's New area button: an empty allowed area with a
    /// name, at most 10 of them per map (the game's own limit).
    /// </summary>
    public class CreateAreaRequestDto
    {
        public int MapId { get; set; }

        /// <summary>The name to give the area; empty takes the game's own default ("Area 1").</summary>
        public string Label { get; set; }
    }

    /// <summary>
    /// Rename one area. `label` names the area the way the area reads do (its
    /// label or its type name), `new_label` is the name the dialog would take.
    /// </summary>
    public class RenameAreaRequestDto
    {
        public int MapId { get; set; }
        public string Label { get; set; }
        public string NewLabel { get; set; }
    }

    /// <summary>One area a change lands on: its label or type name.</summary>
    public class AreaChangeRequestDto
    {
        public int MapId { get; set; }
        public string Label { get; set; }
    }

    /// <summary>
    /// The Manage areas dialog's Shrink button: clear every cell of the
    /// rectangle from the area, the game's own shrink designator applied at
    /// once.
    /// </summary>
    public class ShrinkAreaRequestDto
    {
        public int MapId { get; set; }
        public string Label { get; set; }
        public PositionDto PointA { get; set; }
        public PositionDto PointB { get; set; }
    }
}
