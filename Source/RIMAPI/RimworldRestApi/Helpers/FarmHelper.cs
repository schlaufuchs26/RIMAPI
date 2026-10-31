using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Models;
using RimWorld;
using UnityEngine;
using Verse;

namespace RIMAPI.Helpers
{
    public static class FarmHelper
    {
        public static MapFarmSummaryDto GenerateFarmSummary(Map map)
        {
            var summary = new MapFarmSummaryDto();
            var growingZones = map.zoneManager.AllZones.OfType<Zone_Growing>().ToList();

            summary.TotalGrowingZones = growingZones.Count;

            var cropTypes = new Dictionary<string, CropTypeDto>();
            var allPlants = new List<Plant>();
            // The averages are accumulated here and not in the DTO: a DTO field
            // that carries a running sum is the shape that made the old
            // growth_progress_average a sum over every plant divided by the
            // number of crop types, so a half-grown field read 116 %.
            var growthSum = new Dictionary<string, float>();
            var maxGrowth = new Dictionary<string, float>();
            var daysUntilHarvest = new Dictionary<string, float>();

            foreach (Zone_Growing zone in growingZones)
            {
                var zonePlants = map
                    .listerThings.ThingsInGroup(ThingRequestGroup.Plant)
                    .OfType<Plant>()
                    .Where(p => zone.ContainsCell(p.Position))
                    .ToList();

                allPlants.AddRange(zonePlants);
                var plantDefToGrowName = zone.PlantDefToGrow.defName;
                if (!cropTypes.ContainsKey(plantDefToGrowName))
                {
                    cropTypes[plantDefToGrowName] = new CropTypeDto
                    {
                        PlantDefName = plantDefToGrowName,
                        PlantLabel = zone.PlantDefToGrow.label,
                        PlantCategory = GetPlantCategory(zone.PlantDefToGrow),
                        TotalPlants = 0,
                        ExpectedYield = 0,
                        InfectedCount = 0,
                        HarvestablePlants = 0,
                        GrowthProgressAverage = 0,
                        DaysUntilHarvest = 0,
                        IsFullyGrown = false,
                        IsHarvestable = false,
                        ZoneId = zone.ID,
                    };
                }

                foreach (Plant plant in zonePlants)
                {
                    var defName = plant.def.defName;

                    if (!cropTypes.ContainsKey(defName))
                    {
                        continue;
                    }

                    var crop = cropTypes[defName];
                    crop.TotalPlants++;
                    growthSum[defName] = (growthSum.ContainsKey(defName) ? growthSum[defName] : 0f) + plant.Growth;
                    maxGrowth[defName] = Math.Max(
                        maxGrowth.ContainsKey(defName) ? maxGrowth[defName] : 0f,
                        plant.Growth
                    );

                    if (plant.IsCrop && plant.HarvestableNow)
                    {
                        crop.HarvestablePlants++;
                        crop.ExpectedYield += plant.YieldNow();
                        crop.IsHarvestable = true;
                    }

                    if (plant.Blighted)
                    {
                        crop.InfectedCount++;
                        summary.TotalInfectedPlants++;
                    }

                    if (plant.Growth >= 1f)
                    {
                        crop.IsFullyGrown = true;
                    }

                    float days = CalculateDaysUntilHarvest(plant);
                    if (
                        !daysUntilHarvest.ContainsKey(defName)
                        || days < daysUntilHarvest[defName]
                    )
                    {
                        daysUntilHarvest[defName] = days;
                    }
                }
            }

            // Calculate averages and finalize crop types
            float totalGrowth = 0f;
            float nextHarvest = -1f;
            foreach (var crop in cropTypes.Values)
            {
                if (crop.TotalPlants > 0)
                {
                    crop.GrowthProgressAverage = growthSum[crop.PlantDefName] / crop.TotalPlants;
                    crop.MaxGrowthProgress = maxGrowth[crop.PlantDefName];
                    crop.DaysUntilHarvest = daysUntilHarvest[crop.PlantDefName];
                    totalGrowth += growthSum[crop.PlantDefName];
                    if (nextHarvest < 0f || crop.DaysUntilHarvest < nextHarvest)
                    {
                        nextHarvest = crop.DaysUntilHarvest;
                    }
                }
                summary.TotalPlants += crop.TotalPlants;
                summary.TotalExpectedYield += crop.ExpectedYield;
                summary.TotalHarvestablePlants += crop.HarvestablePlants;
            }

            // Averaged over the plants, not over the crop types: a zone of 200
            // rice plants and a zone of 2 corn plants must not weigh the same.
            if (summary.TotalPlants > 0)
            {
                summary.GrowthProgressAverage = totalGrowth / summary.TotalPlants;
                summary.MaxGrowthProgress = maxGrowth.Values.Count > 0 ? maxGrowth.Values.Max() : 0f;
                summary.DaysUntilNextHarvest = nextHarvest < 0f ? 0f : nextHarvest;
            }

            summary.CropTypes = cropTypes.Values.OrderByDescending(c => c.TotalPlants).ToList();
            return summary;
        }

        public static GrowingZoneDto GetGrowingZoneById(Map map, int zoneId)
        {
            var zone = map
                .zoneManager.AllZones.OfType<Zone_Growing>()
                .FirstOrDefault(z => z.ID == zoneId);

            if (zone == null)
                return null;

            var zonePlants = map
                .listerThings.ThingsInGroup(ThingRequestGroup.Plant)
                .OfType<Plant>()
                .Where(p => zone.ContainsCell(p.Position))
                .ToList();

            var zoneDto = new GrowingZoneDto
            {
                Zone = new ZoneDto
                {
                    Id = zone.ID,
                    CellsCount = zone.CellCount,
                    Label = zone.label ?? "Unnamed Zone",
                    BaseLabel = zone.BaseLabel,
                },
                PlantDefName = zone.GetPlantDefToGrow()?.defName ?? "None",
                PlantCount = zonePlants.Count,
                ExpectedYield = 0,
                InfectedCount = 0,
                GrowthProgress = 0,
                HasDying = false,
                HasDyingFromPollution = false,
                HasDyingFromNoPollution = false,
#if RIMWORLD_1_5
                IsSowing =
                    zone.allowSow
                    && PlantUtility.GrowthSeasonNow(
                        zone.Cells.FirstOrDefault(),
                        map,
                        forSowing: true
                    )
                    && zone.Cells.Any(c => c.GetPlant(map) == null),
#elif RIMWORLD_1_6
                IsSowing = zone.allowSow,
#endif
                SoilType = GetSoilType(zone.Cells.FirstOrDefault(), map),
                Fertility = GetZoneFertility(zone, map),
            };

            if (zonePlants.Count > 0)
            {
                zoneDto.GrowthProgress = zonePlants.Average(p => p.Growth);
                zoneDto.DefExpectedYield = Mathf.RoundToInt(
                    zonePlants.Sum(p => p.def.plant.harvestYield)
                );
                zoneDto.ExpectedYield = Mathf.RoundToInt(
                    zonePlants.Where(p => p.HarvestableNow).Sum(p => p.YieldNow())
                );
                zoneDto.InfectedCount = zonePlants.Count(p => p.Blighted);
                zoneDto.HasDying = zonePlants.Any(p => p.Dying);
                zoneDto.HasDyingFromPollution = zonePlants.Any(p => p.DyingFromPollution);
                zoneDto.HasDyingFromNoPollution = zonePlants.Any(p => p.DyingFromNoPollution);
            }

            return zoneDto;
        }

        /// <summary>
        /// Resolve a plant ThingDef from either its defName ("Plant_Rice") or a
        /// looser alias (label "rice", or the bare crop name "Rice"). The agents
        /// that drive this API only reliably know the defName, but accepting the
        /// label as a fallback makes the endpoint forgiving and matches operator
        /// expectations.
        /// </summary>
        public static ThingDef ResolvePlantDef(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
            if (def != null && def.plant != null)
                return def;

            return DefDatabase<ThingDef>.AllDefsListForReading.FirstOrDefault(d =>
                d.plant != null &&
                (string.Equals(d.label, name, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(d.defName, "Plant_" + name, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Create a growing zone. On failure, <paramref name="error"/> carries a
        /// SPECIFIC reason — historically this returned a bare null that the
        /// caller blamed on the plant def, even when the real cause was that
        /// every requested cell already belonged to another zone (the common
        /// case when an agent re-issues a growing zone over an existing one).
        /// </summary>
        public static GrowingZoneDto CreateGrowingZone(
            Map map, string plantDefName, List<IntVec3> cells, out string error)
        {
            error = null;

            var plantDef = ResolvePlantDef(plantDefName);
            if (plantDef == null || plantDef.plant == null)
            {
                error = $"Invalid plant definition: {plantDefName}";
                return null;
            }

            var zone = new Zone_Growing(map.zoneManager);
            map.zoneManager.RegisterZone(zone);

            int requested = cells.Count;
            int skippedOutOfBounds = 0;
            int skippedOccupied = 0;
            foreach (var cell in cells)
            {
                if (!cell.InBounds(map))
                {
                    skippedOutOfBounds++;
                    continue;
                }
                if (map.zoneManager.ZoneAt(cell) != null)
                {
                    skippedOccupied++;
                    continue;
                }
                zone.AddCell(cell);
            }

            // No valid cells added — remove the empty zone and report WHY.
            if (zone.CellCount == 0)
            {
                zone.Delete();
                error =
                    $"No free cells in the requested {requested}-cell rectangle " +
                    $"({skippedOccupied} already belong to another zone, " +
                    $"{skippedOutOfBounds} out of bounds). " +
                    $"The area likely overlaps an existing zone — a zone for " +
                    $"'{plantDef.defName}' may already exist here.";
                return null;
            }

            zone.SetPlantDefToGrow(plantDef);
            return GetGrowingZoneById(map, zone.ID);
        }

        // Helper methods
        public static string GetPlantCategory(ThingDef plantDef)
        {
            if (plantDef == null)
                return "Unknown";
            if (plantDef.plant == null)
                return "Unknown";

            if (plantDef.plant.IsTree)
                return "Tree";
            if (plantDef.plant.harvestedThingDef?.IsDrug ?? false)
                return "Drug";
            if (plantDef.plant.harvestedThingDef?.IsMedicine ?? false)
                return "Medicine";
            if (plantDef.plant.Sowable)
                return "Crop";

            return "Other";
        }

        /// <summary>
        /// Days until the plant can be harvested, assuming ideal light and
        /// fertility: growth advances 1/growDays per day at full speed, so the
        /// missing growth times growDays is the answer. The old code divided
        /// the missing growth by growDays, which is the reciprocal, and it
        /// summed the result over every plant before dividing by one crop's
        /// plant count.
        /// </summary>
        public static float CalculateDaysUntilHarvest(Plant plant)
        {
            float minGrowth = plant.def.plant.harvestMinGrowth;
            if (plant.Growth >= minGrowth)
            {
                return 0f; // Already harvestable
            }
            float growDays = plant.def.plant.growDays;
            if (growDays <= 0f)
            {
                return 0f;
            }
            return (minGrowth - plant.Growth) * growDays;
        }

        public static bool IsPlantResting(Plant plant)
        {
            float dayPercent = GenLocalDate.DayPercent(plant);
            return dayPercent < 0.25f || dayPercent > 0.8f;
        }

        public static string GetSoilType(IntVec3 cell, Map map)
        {
            if (!cell.InBounds(map))
                return "Unknown";
            var terrain = map.terrainGrid.TerrainAt(cell);
            return terrain?.defName ?? "Unknown";
        }

        public static float GetZoneFertility(Zone_Growing zone, Map map)
        {
            if (zone.Cells.Count == 0)
                return 0f;

            var fertilitySum = 0f;
            var sampleCells = zone.Cells.Take(10); // Sample first 10 cells for performance

            foreach (var cell in sampleCells)
            {
                if (cell.InBounds(map))
                {
                    fertilitySum += map.fertilityGrid.FertilityAt(cell);
                }
            }

            return sampleCells.Any() ? fertilitySum / sampleCells.Count() : 0f;
        }
    }
}
