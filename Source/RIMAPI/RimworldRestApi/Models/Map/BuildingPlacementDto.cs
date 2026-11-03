using RIMAPI.Core;

namespace RIMAPI.Models.Map
{
    /// <summary>
    /// Moving a building (fox ticket #4316, parity wave 7): the Designator
    /// Uninstall marks it, and the Install order puts the minified item back
    /// down as an install blueprint.
    /// </summary>
    public class UninstallRequestDto
    {
        public int MapId { get; set; }
        public int BuildingId { get; set; }
    }

    public class UninstallResultDto
    {
        public int BuildingId { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }

        /// <summary>"designated" when a constructor has to take it down, "uninstalled" when the game did it at once.</summary>
        [ParameterDescription("\"designated\" when a constructor has to take it down, \"uninstalled\" when the game did it at once.")]
        public string Mode { get; set; }

        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }

    public class InstallRequestDto
    {
        public int MapId { get; set; }

        /// <summary>The minified item's thing id: what an uninstall leaves behind.</summary>
        public int ThingId { get; set; }

        public PositionDto Position { get; set; }

        /// <summary>0 south, 1 east, 2 north, 3 west; ignored by a def that does not rotate.</summary>
        [ParameterDescription("0 south, 1 east, 2 north, 3 west; ignored by a def that does not rotate.")]
        public int? Rotation { get; set; }
    }

    public class InstallResultDto
    {
        [ParameterDescription("The id of the blueprint the install left on the map.")]
        public int BlueprintId { get; set; }
        public int ThingId { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        [ParameterDescription("The rotation the installed building takes: 0, 90, 180 or 270.")]
        public int Rotation { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }
}
