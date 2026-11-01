using System.Collections.Generic;
using System.Linq;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    // The buildings behind the power figures, one row each (fox ticket #3762).
    // The totals in GetMapPowerInfo sum the nets, and a building no net holds is
    // in no total: an electric stove four cells from the nearest conduit read
    // exactly like a working one for four game hours while its bill said
    // "running". This walks the built buildings instead, so the unconnected ones
    // are visible with their own switch, their net's figures and the distance to
    // the nearest conduit cell.
    public static class PowerGrid
    {
        // Buildings answers every built building that has a power comp, sorted
        // by id so two reads of the same colony agree.
        public static List<PoweredBuildingDto> Buildings(Map map)
        {
            var rows = new List<PoweredBuildingDto>();
            if (map == null)
                return rows;

            List<IntVec3> conduits = ConduitCells(map);
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                CompPower power = building.TryGetComp<CompPower>();
                if (power == null)
                    continue;

                rows.Add(Row(building, power, conduits));
            }

            return rows.OrderBy(r => r.Id).ToList();
        }

        // NearestConduit is how far the nearest conduit cell stands from a
        // building, for a caller that holds only the building (the bill state
        // asks for one bench). Distance -1 means the map holds no conduit.
        public static int NearestConduit(Building building, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (building == null || !building.Spawned || building.Map == null)
                return -1;

            int best = -1;
            foreach (IntVec3 conduit in ConduitCells(building.Map))
            {
                int distance = Distance(building, conduit);
                if (best < 0 || distance < best)
                {
                    best = distance;
                    cell = conduit;
                }
            }
            return best;
        }

        // Describe answers one building's row in the same shape the list holds
        // (fox ticket #3898). The power switch write answers with it, so a run
        // reads the state it just wrote: a switch that flipped and a net that
        // still gives nothing are two facts, and only the first is the call's.
        public static PoweredBuildingDto Describe(Building building)
        {
            if (building == null)
                return null;
            CompPower power = building.TryGetComp<CompPower>();
            return Row(building, power, ConduitCells(building.Map));
        }

        private static PoweredBuildingDto Row(Building building, CompPower power, List<IntVec3> conduits)
        {
            CompPowerTrader trader = building.TryGetComp<CompPowerTrader>();
            CompPowerPlant plant = building.TryGetComp<CompPowerPlant>();

            // A plant's own PowerConsumption is its nominal output, negative in
            // the game's sign, so only a positive one is a machine that draws.
            float consumes = trader != null && trader.Props.PowerConsumption > 0f
                ? trader.Props.PowerConsumption
                : 0f;

            int distance = -1;
            IntVec3 nearest = IntVec3.Invalid;
            foreach (IntVec3 conduit in conduits)
            {
                int candidate = Distance(building, conduit);
                if (distance < 0 || candidate < distance)
                {
                    distance = candidate;
                    nearest = conduit;
                }
            }

            var dto = new PoweredBuildingDto
            {
                Id = building.thingIDNumber,
                DefName = building.def?.defName,
                Label = building.Label,
                Position = new PositionDto { X = building.Position.x, Z = building.Position.z },
                Consumes = consumes,
                Produces = plant != null ? plant.PowerOutput : 0f,
                Nominal = plant != null ? System.Math.Abs(plant.Props.PowerConsumption) : 0f,
                Connected = power != null && power.PowerNet != null,
                // A building without a trader has no "power on" of its own (a
                // conduit): the net reaching it is the whole question. A
                // flickable building without a power comp (a switch, a vent)
                // has no net either and is still on or off by its switch.
                Powered = trader != null ? trader.PowerOn : power != null && power.PowerNet != null,
                SwitchedOn = FlickUtility.WantsToBeOn(building),
                ConduitDistance = distance,
            };
            if (power != null && power.PowerNet != null)
            {
                dto.NetProduction = power.PowerNet.CurrentEnergyGainRate();
                dto.NetStored = power.PowerNet.CurrentStoredEnergy();
            }
            if (nearest.IsValid)
            {
                dto.ConduitPosition = new PositionDto { X = nearest.x, Z = nearest.z };
            }
            return dto;
        }

        // ConduitCells collects every cell a built conduit stands on. Conduits
        // are the one thing a run builds to reach a machine, so their cells are
        // what the distance is measured to.
        private static List<IntVec3> ConduitCells(Map map)
        {
            var cells = new List<IntVec3>();
            foreach (Building building in map.listerBuildings.allBuildingsColonist)
            {
                if (building.def?.building?.isPowerConduit != true)
                    continue;
                cells.AddRange(building.OccupiedRect().Cells);
            }
            return cells;
        }

        // Distance is how many cells stand between a building and one cell, by
        // the longer of the two axes: a conduit under the building is 0, one
        // beside it is 1. A footprint of several cells measures from its
        // nearest edge, so a 2x2 cooler one cell from a conduit is 1.
        private static int Distance(Building building, IntVec3 cell)
        {
            CellRect rect = building.OccupiedRect();
            int dx = 0;
            if (cell.x < rect.minX)
                dx = rect.minX - cell.x;
            else if (cell.x > rect.maxX)
                dx = cell.x - rect.maxX;

            int dz = 0;
            if (cell.z < rect.minZ)
                dz = rect.minZ - cell.z;
            else if (cell.z > rect.maxZ)
                dz = cell.z - rect.maxZ;

            return System.Math.Max(dx, dz);
        }
    }
}
