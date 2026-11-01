using System.Collections.Generic;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Reads a building's thermostat and the rooms beside it (fox ticket
    /// #3744). A cooler fits into a wall with a room on each side: it chills
    /// one and dumps the heat into the other, so both temperatures matter and
    /// the colder one is the room a run has to watch. A heater stands inside
    /// its own room and touches only that one.
    /// </summary>
    public static class Thermostat
    {
        /// <summary>
        /// Describe answers what the building is set to and what the rooms
        /// around it read. The target is whatever the game holds, so a call
        /// without a new target is a plain read of the machine's own tab.
        /// </summary>
        public static TemperatureTargetDto Describe(Building building)
        {
            CompTempControl comp = building.TryGetComp<CompTempControl>();
            var dto = new TemperatureTargetDto
            {
                Id = building.thingIDNumber,
                Label = building.Label,
                DefName = building.def?.defName,
                HasThermostat = comp != null,
                Position = new PositionDto { X = building.Position.x, Z = building.Position.z },
                Rooms = RoomsBeside(building),
            };
            if (comp != null)
            {
                dto.Target = comp.targetTemperature;
                dto.PreviousTarget = comp.targetTemperature;
            }
            CompPowerTrader power = building.TryGetComp<CompPowerTrader>();
            if (power != null)
            {
                dto.HasPower = true;
                dto.Powered = power.PowerOn;
            }
            return dto;
        }

        /// <summary>
        /// The distinct rooms the building's own cell and its four cardinal
        /// neighbours lie in, with the cell each was found from. A wall cell
        /// belongs to no room, so a cooler's own cell contributes nothing and
        /// its two sides do the work.
        /// </summary>
        private static List<RoomTemperatureDto> RoomsBeside(Building building)
        {
            var rooms = new List<RoomTemperatureDto>();
            if (!building.Spawned || building.Map == null) return rooms;

            Map map = building.Map;
            var seen = new HashSet<int>();
            var cells = new List<IntVec3> { building.Position };
            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(building))
            {
                cells.Add(cell);
            }
            foreach (IntVec3 cell in cells)
            {
                Room room = cell.GetRoom(map);
                if (room == null || room.ID < 0 || !seen.Add(room.ID)) continue;
                rooms.Add(
                    new RoomTemperatureDto
                    {
                        Id = room.ID,
                        Label = room.GetRoomRoleLabel(),
                        Temperature = room.Temperature,
                        Cells = room.CellCount,
                        Position = new PositionDto { X = cell.x, Z = cell.z },
                    }
                );
            }
            return rooms;
        }
    }
}
