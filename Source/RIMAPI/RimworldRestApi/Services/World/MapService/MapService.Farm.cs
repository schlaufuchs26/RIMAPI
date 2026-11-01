using System;
using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

// Growing zones, the plants in them and the food that spoils
// (split out of MapService.cs, fox #3659).

namespace RIMAPI.Services
{
    public partial class MapService
    {
        public ApiResult<MapFarmSummaryDto> GenerateFarmSummary(int mapId)
        {
            if (!TryMap(mapId, out Map map, out string error))
            {
                return ApiResult<MapFarmSummaryDto>.Fail(error);
            }

            var result = FarmHelper.GenerateFarmSummary(map);
            return ApiResult<MapFarmSummaryDto>.Ok(result);
        }

        public ApiResult<GrowingZoneDto> GetGrowingZoneById(int mapId, int zoneId)
        {
            if (!TryMap(mapId, out Map map, out string error))
            {
                return ApiResult<GrowingZoneDto>.Fail(error);
            }

            var result = FarmHelper.GetGrowingZoneById(map, zoneId);
            if (result == null)
            {
                return ApiResult<GrowingZoneDto>.Fail(
                    GrowingZoneLookup.ZoneNotFound(mapId, zoneId));
            }

            return ApiResult<GrowingZoneDto>.Ok(result);
        }

        public ApiResult<GrowingZoneDto> CreateGrowingZone(CreateGrowingZoneRequestDto request)
        {
            if (!TryMap(request.MapId, out Map map, out string mapError))
            {
                return ApiResult<GrowingZoneDto>.Fail(mapError);
            }

            var minX = Math.Min(request.PointA.X, request.PointB.X);
            var maxX = Math.Max(request.PointA.X, request.PointB.X);
            var minZ = Math.Min(request.PointA.Z, request.PointB.Z);
            var maxZ = Math.Max(request.PointA.Z, request.PointB.Z);
            var cells = new List<IntVec3>();
            for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                    cells.Add(new IntVec3(x, 0, z));

            var result = FarmHelper.CreateGrowingZone(map, request.PlantDef, cells, out var error);
            if (result == null)
            {
                return ApiResult<GrowingZoneDto>.Fail(
                    error ?? $"Invalid plant definition: {request.PlantDef}");
            }
            return ApiResult<GrowingZoneDto>.Ok(result);
        }

        public ApiResult<List<ThingDto>> GetMapPlants(int mapId)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<List<ThingDto>>.Fail(error);
            }

            var result = MapHelper.GetMapPlants(mapId);
            return ApiResult<List<ThingDto>>.Ok(result);
        }

        /// <summary>
        /// The plants standing on one rectangle (fox ticket #3277): the defName,
        /// growth and cell of each, which is what a field is diagnosed with.
        /// `map/plants` answers every plant on the map, a 6.5 MB answer no read
        /// wants, and `things` lists no plants at all.
        /// </summary>
        public ApiResult<List<PlantDto>> GetMapPlantsInRect(int mapId, int x1, int z1, int x2, int z2)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<List<PlantDto>>.Fail(error);
            }
            return ApiResult<List<PlantDto>>.Ok(MapHelper.GetMapPlantsInRect(mapId, x1, z1, x2, z2));
        }

        public ApiResult<UnwantedFoodDto> GetUnwantedFood(int mapId)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<UnwantedFoodDto>.Fail(error);
            }
            return ApiResult<UnwantedFoodDto>.Ok(MapHelper.GetUnwantedFood(mapId));
        }

        /// <summary>
        /// The perishable food in rooms above 0 C (fox ticket #3672). Colony
        /// 14 lost 59 horse meat overnight in a room held at 16 C by a
        /// PassiveCooler, and no read said a word before the game's own rot
        /// message: the rooms carry their temperature and the things their
        /// stack, but nothing joined the two.
        /// </summary>
        public ApiResult<MapRottingFoodDto> GetRottingFood(int mapId)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<MapRottingFoodDto>.Fail(error);
            }
            try
            {
                return ApiResult<MapRottingFoodDto>.Ok(RottingFood.ForMap(mapId));
            }
            catch (System.Exception ex)
            {
                return ApiResult<MapRottingFoodDto>.Fail(ex.Message);
            }
        }
    }
}
