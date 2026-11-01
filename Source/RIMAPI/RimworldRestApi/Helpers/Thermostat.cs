using System.Collections.Generic;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Reads a building's thermostat and the spaces it works on (fox tickets
    /// #3744, #3834). A cooler fits into a wall with a room on each side: it
    /// chills one and dumps the heat into the other. Which one is which comes
    /// from the machine's rotation, exactly as the game decides it
    /// (Building_Cooler.TickRare), never from which side happens to be colder:
    /// a cooler whose cold side is a wall chills nothing, and a read that
    /// guessed from temperature named the outdoors instead and made a working
    /// freezer look broken. A heater stands inside its own room and has one
    /// side.
    /// </summary>
    public static class Thermostat
    {
        /// <summary>The side name a cooler chills.</summary>
        public const string ColdSide = "cold";

        /// <summary>The side name a cooler dumps its heat into.</summary>
        public const string VentSide = "vent";

        /// <summary>The side name of a machine that works on its own room (a heater).</summary>
        public const string OwnSide = "room";

        /// <summary>
        /// Describe answers what the building is set to and what the sides read.
        /// The target is whatever the game holds, so a call without a new
        /// target is a plain read of the machine's own tab.
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
                Sides = Sides(building),
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
            // The switch is not the power comp's PowerOn: a machine switched
            // off and one on a dead net both read PowerOn false, and only the
            // first is fixed by flipping the switch (fox ticket #3898).
            dto.HasSwitch = building.TryGetComp<CompFlickable>() != null;
            dto.SwitchedOn = FlickUtility.WantsToBeOn(building);
            return dto;
        }

        /// <summary>
        /// The spaces the machine works on. A cooler has two: the cell it chills
        /// and the cell it vents into, both taken from its rotation. Any other
        /// thermostat has one: its own cell's room. A side whose cell is a wall
        /// carries no room, because the game moves no heat through it.
        /// </summary>
        public static List<RoomSideDto> Sides(Building building)
        {
            var sides = new List<RoomSideDto>();
            if (!building.Spawned || building.Map == null) return sides;

            Map map = building.Map;
            if (building is Building_Cooler)
            {
                TemperatureTargetRules.CoolerCells cells = TemperatureTargetRules.CoolerSideCells(
                    building.Position.x,
                    building.Position.z,
                    building.Rotation.AsInt
                );
                sides.Add(Side(ColdSide, new IntVec3(cells.ColdX, 0, cells.ColdZ), map));
                sides.Add(Side(VentSide, new IntVec3(cells.VentX, 0, cells.VentZ), map));
                return sides;
            }
            sides.Add(Side(OwnSide, building.Position, map));
            return sides;
        }

        /// <summary>One side, read at its cell.</summary>
        private static RoomSideDto Side(string name, IntVec3 cell, Map map)
        {
            var dto = new RoomSideDto
            {
                Side = name,
                Cell = new PositionDto { X = cell.x, Z = cell.z },
                Impassable = !cell.InBounds(map) || cell.Impassable(map),
            };
            if (!dto.Impassable) dto.Room = RoomAt(cell, map);
            return dto;
        }

        /// <summary>
        /// The room a cell lies in, or null for a cell no room covers. Outdoor
        /// space is a room too, and it says so: the game leaves such a room at
        /// the outdoor temperature, so a cooler cannot chill it.
        /// </summary>
        private static RoomTemperatureDto RoomAt(IntVec3 cell, Map map)
        {
            Room room = cell.GetRoom(map);
            if (room == null) return null;
            return new RoomTemperatureDto
            {
                Id = room.ID,
                Label = room.GetRoomRoleLabel(),
                Temperature = room.Temperature,
                Cells = room.CellCount,
                Position = new PositionDto { X = cell.x, Z = cell.z },
                Outdoor = room.UsesOutdoorTemperature,
            };
        }
    }
}
