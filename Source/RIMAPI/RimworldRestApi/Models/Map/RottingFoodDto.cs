using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// The perishable food on the map that sits in a room above 0 C (fox
    /// ticket #3672). The room read names every room's temperature and the
    /// things read every stack, but neither says which food is in which room:
    /// colony 14 lost 59 horse meat to rot in a room a PassiveCooler held at
    /// 16 C, and the only warning was the game's own message after the loss.
    /// </summary>
    public class MapRottingFoodDto
    {
        [ParameterDescription("The food stacks that will rot.")]
        public List<RottingFoodDto> Items { get; set; } = new List<RottingFoodDto>();
        [ParameterDescription("How many stacks will rot.")]
        public int TotalStacks { get; set; }
        [ParameterDescription("How many units those stacks hold.")]
        public int TotalUnits { get; set; }
        /// <summary>The warmest room in the answer, so a caller can name the worst one without re-reading the rooms.</summary>
        [ParameterDescription("The warmest room in the answer, so a caller can name the worst one without re-reading the rooms.")]
        public float WarmestTemperature { get; set; }

        /// <summary>
        /// The coolers and heaters that touch a room in this answer (fox ticket
        /// #3745). A room temperature alone reads like a room without a
        /// machine: colony 15's "freezer" stood at 16.4 C because the cooler in
        /// its wall still held the default 21 C, so the machine's target is the
        /// lever a run needs.
        /// </summary>
        [ParameterDescription("The coolers and heaters that touch a room in this answer (fox ticket #3745).")]
        public List<RoomThermostatDto> Thermostats { get; set; } =
            new List<RoomThermostatDto>();
    }

    /// <summary>
    /// One cooler or heater beside a room in the answer. A cooler fits into a
    /// wall, so the same machine appears under both rooms it works on; Side
    /// says which room it chills and which one it dumps its heat into.
    /// </summary>
    public class RoomThermostatDto
    {
        [ParameterDescription("The room's numeric id.")]
        public int RoomId { get; set; }

        /// <summary>
        /// Which side of the machine this room is on: "cold" (it chills it),
        /// "vent" (it dumps its heat into it) or "room" (a heater's own room).
        /// A cooler that vents into a warm room heats it, so its target is not
        /// the lever for that room (fox ticket #3834).
        /// </summary>
        [ParameterDescription("Which side of the machine this room is on: \"cold\" (it chills it), \"vent\" (it dumps its heat into it) or \"room\" (a heater's own room).")]
        public string Side { get; set; }

        public int BuildingId { get; set; }
        public string Label { get; set; }
        public string DefName { get; set; }
        /// <summary>The target the machine holds, in C. Above 0 C a cooler never chills below it.</summary>
        [ParameterDescription("The target the machine holds, in C.")]
        public float Target { get; set; }
        /// <summary>False for a machine the game gives no power comp at all.</summary>
        [ParameterDescription("False for a machine the game gives no power comp at all.")]
        public bool HasPower { get; set; }
        /// <summary>The power switch, so an unpowered cooler is not mistaken for one that cannot keep up.</summary>
        [ParameterDescription("The power switch, so an unpowered cooler is not mistaken for one that cannot keep up.")]
        public bool Powered { get; set; }
    }

    /// <summary>
    /// One stack of perishable food in a warm room. HoursUntilRot is the
    /// game's own estimate from the stack's accumulated rot progress, so a
    /// stack that has sat warm for days reads a small number even at a
    /// temperature where fresh food would last weeks. The value is clamped at
    /// 100000 hours: an item that would outlast any colony decision reads as
    /// "not soon" instead of an infinite JSON number.
    /// </summary>
    public class RottingFoodDto
    {
        public int ThingId { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How many units the stack holds.")]
        public int StackCount { get; set; }
        [ParameterDescription("How much nutrition the stack holds.")]
        public float Nutrition { get; set; }
        [ParameterDescription("How many hours until the stack rots away.")]
        public float HoursUntilRot { get; set; }
        [ParameterDescription("The room's numeric id.")]
        public int RoomId { get; set; }
        [ParameterDescription("The room's name, as the game shows it.")]
        public string RoomLabel { get; set; }
        [ParameterDescription("The room's temperature, in Celsius.")]
        public float RoomTemperature { get; set; }
        public PositionDto Position { get; set; }
    }
}
