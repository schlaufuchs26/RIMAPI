using System.Collections.Generic;

namespace RIMAPI.Models
{
    /// <summary>
    /// One building's thermostat and the spaces it works on (fox ticket
    /// #3744). A cooler's target temperature is a per-building setting the
    /// player changes on the building's own tab; the build action placed the
    /// machine and the things read named it, but nothing read or set the
    /// target, so a colony could build a working cooler that never freezes
    /// anything.
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

        /// <summary>
        /// The spaces the machine works on, one entry per side (fox ticket
        /// #3834). A cooler chills one side of the wall and dumps its heat into
        /// the other, so a read that named only "the colder room beside it"
        /// could name the outdoors and make a working freezer look broken. The
        /// sides come from the machine's rotation, which is the cell the game
        /// itself cools (Building_Cooler.TickRare); a heater has one side.
        /// </summary>
        public List<RoomSideDto> Sides { get; set; } = new List<RoomSideDto>();
    }

    /// <summary>
    /// One side of a thermostat: the cell, whether that cell is a wall, and the
    /// room behind it. A cooler whose side cell is impassable moves no heat at
    /// all, which is what the game checks before it runs, so the empty Room is
    /// the answer to "why is the freezer warm" rather than a missing value.
    /// </summary>
    public class RoomSideDto
    {
        /// <summary>"cold" (the side a cooler chills), "vent" (where its heat goes) or "room" (a heater's own room).</summary>
        public string Side { get; set; }

        /// <summary>The cell this side is read at, in map coordinates.</summary>
        public PositionDto Cell { get; set; }

        /// <summary>True when the cell is a wall, a closed door or anything else the game cannot pass.</summary>
        public bool Impassable { get; set; }

        /// <summary>The room the cell lies in, or null when the side reaches no room.</summary>
        public RoomTemperatureDto Room { get; set; }
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

        /// <summary>
        /// True when the room follows the outdoor temperature instead of one it
        /// holds itself (unroofed, or open to the map). The game's own
        /// temperature control does nothing to such a room, so a cooler with an
        /// outdoor side cannot chill it however cold its target is.
        /// </summary>
        public bool Outdoor { get; set; }
    }
}
