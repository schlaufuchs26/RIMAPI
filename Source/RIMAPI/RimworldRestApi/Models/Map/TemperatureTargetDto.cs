using System.Collections.Generic;

namespace RIMAPI.Models
{
    /// <summary>
    /// One building's thermostat and the rooms beside it (fox ticket #3744).
    /// A cooler's target temperature is a per-building setting the player
    /// changes on the building's own tab; the build action placed the machine
    /// and the things read named it, but nothing read or set the target, so a
    /// colony could build a working cooler that never freezes anything.
    /// </summary>
    public class TemperatureTargetDto
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public string DefName { get; set; }

        /// <summary>True for a building that carries a CompTempControl: a cooler or a heater.</summary>
        public bool HasThermostat { get; set; }

        /// <summary>The target the machine holds now, in C. Only meaningful with HasThermostat.</summary>
        public float Target { get; set; }

        /// <summary>The target before this call changed it; equal to Target on a read.</summary>
        public float PreviousTarget { get; set; }

        /// <summary>True when the building draws power at all, so a caller can tell a cold room from an unpowered cooler.</summary>
        public bool HasPower { get; set; }

        /// <summary>The power switch's state, as the power component reports it.</summary>
        public bool Powered { get; set; }

        public PositionDto Position { get; set; }

        /// <summary>The rooms the building touches: a wall-mounted cooler has one on each side.</summary>
        public List<RoomTemperatureDto> Rooms { get; set; } = new List<RoomTemperatureDto>();
    }

    /// <summary>
    /// One room beside the building, with the cell it was found from, so a run
    /// can tell the room a cooler chills from the one it dumps its heat into.
    /// </summary>
    public class RoomTemperatureDto
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public float Temperature { get; set; }
        public int Cells { get; set; }
        public PositionDto Position { get; set; }
    }
}
