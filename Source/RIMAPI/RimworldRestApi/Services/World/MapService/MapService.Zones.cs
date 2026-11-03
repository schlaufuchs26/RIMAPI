using System;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

// Zones, areas and stockpiles: what is painted where and what it
// allows (split out of MapService.cs, fox #3659).

namespace RIMAPI.Services
{
    public partial class MapService
    {
        public ApiResult<MapZonesDto> GetMapZones(int mapId)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<MapZonesDto>.Fail(error);
            }

            MapZonesDto mapZones = new MapZonesDto();
            mapZones.Zones = MapHelper.GetMapZones(mapId);
            mapZones.Areas = MapHelper.GetMapAreas(mapId);
            return ApiResult<MapZonesDto>.Ok(mapZones);
        }

        public ApiResult<AreaCellsDto> GetMapArea(int mapId, string label)
        {
            if (!TryMap(mapId, out Map map, out string mapError))
            {
                return ApiResult<AreaCellsDto>.Fail(mapError);
            }

            Area area = MapHelper.FindArea(map, label, out string error);
            if (area == null)
            {
                return ApiResult<AreaCellsDto>.Fail(error);
            }
            return ApiResult<AreaCellsDto>.Ok(MapHelper.GetAreaCells(map, area));
        }

        /// <summary>
        /// Extend one area over a rectangle, the game's own expand designator
        /// applied at once (fox ticket #3095). The answer is the area after the
        /// change plus how many cells were added, already inside, or outside the
        /// map, so a caller never has to guess what a rectangle did.
        /// </summary>
        public ApiResult<AreaCellsDto> ExpandMapArea(AreaExpandRequestDto request)
        {
            if (!TryMap(request.MapId, out Map map, out string mapError))
            {
                return ApiResult<AreaCellsDto>.Fail(mapError);
            }
            if (request.PointA == null || request.PointB == null)
            {
                return ApiResult<AreaCellsDto>.Fail("point_a and point_b are required: the rectangle the area grows over.");
            }

            Area area = MapHelper.FindArea(map, request.Label, out string error);
            if (area == null)
            {
                return ApiResult<AreaCellsDto>.Fail(error);
            }

            int x1 = Math.Min(request.PointA.X, request.PointB.X);
            int x2 = Math.Max(request.PointA.X, request.PointB.X);
            int z1 = Math.Min(request.PointA.Z, request.PointB.Z);
            int z2 = Math.Max(request.PointA.Z, request.PointB.Z);
            CellRect rect = CellRect.FromLimits(x1, z1, x2, z2);
            return ApiResult<AreaCellsDto>.Ok(MapHelper.ExpandArea(map, area, rect));
        }

        /// <summary>
        /// One area and its cells (fox ticket #3095). The Home area is the one
        /// that decides where colonists haul, so a build order outside it never
        /// happens; this read is how a caller checks before it places one.
        /// </summary>
        public ApiResult<StorageCapacityDto> GetStorageCapacity(int mapId, string defName)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<StorageCapacityDto>.Fail(error);
            }
            try
            {
                return ApiResult<StorageCapacityDto>.Ok(StorageCapacity.ForDef(mapId, defName));
            }
            catch (System.Exception ex)
            {
                return ApiResult<StorageCapacityDto>.Fail(ex.Message);
            }
        }

        public ApiResult<StockpileResponseDto> CreateStockpile(CreateStockpileRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return ApiResult<StockpileResponseDto>.Fail("Request cannot be null.");
                }

                var result = MapHelper.CreateStockpile(request);

                if (result.Success)
                {
                    return ApiResult<StockpileResponseDto>.Ok(result);
                }
                else
                {
                    return ApiResult<StockpileResponseDto>.Fail(result.Message);
                }
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error creating stockpile: {ex}");
                return ApiResult<StockpileResponseDto>.Fail($"Failed to create stockpile: {ex.Message}");
            }
        }

        public ApiResult DeleteStockpile(int zoneId) => MapHelper.DeleteStockpile(zoneId);

        public ApiResult DeleteZone(int zoneId) => MapHelper.DeleteZone(zoneId);

        public ApiResult<StockpileResponseDto> UpdateStockpile(UpdateStockpileRequestDto request) => MapHelper.UpdateStockpile(request);

        // The zone and area writes of parity wave 6 (fox ticket #4316). The map
        // is resolved here, so the helper stays free of the lookup every route
        // would otherwise repeat.
        public ApiResult<GrowingZonePlantsDto> GetGrowingZonePlants(int mapId, int zoneId)
        {
            if (!TryMap(mapId, out Map map, out string error))
            {
                return ApiResult<GrowingZonePlantsDto>.Fail(error);
            }
            return ZoneEdit.GetSelectablePlants(map, zoneId);
        }

        public ApiResult<GrowingZoneDto> UpdateGrowingZone(UpdateGrowingZoneRequestDto request)
        {
            if (!TryMap(request?.MapId ?? 0, out Map map, out string error))
            {
                return ApiResult<GrowingZoneDto>.Fail(error);
            }
            return ZoneEdit.UpdateGrowingZone(map, request);
        }

        public ApiResult<ZoneDto> RenameZone(RenameZoneRequestDto request)
        {
            if (!TryMap(request?.MapId ?? 0, out Map map, out string error))
            {
                return ApiResult<ZoneDto>.Fail(error);
            }
            return ZoneEdit.RenameZone(map, request);
        }

        public ApiResult<AreaCellsDto> CreateArea(CreateAreaRequestDto request)
        {
            if (!TryMap(request?.MapId ?? 0, out Map map, out string error))
            {
                return ApiResult<AreaCellsDto>.Fail(error);
            }
            return ZoneEdit.CreateArea(map, request);
        }

        public ApiResult<AreaCellsDto> RenameArea(RenameAreaRequestDto request)
        {
            if (!TryMap(request?.MapId ?? 0, out Map map, out string error))
            {
                return ApiResult<AreaCellsDto>.Fail(error);
            }
            return ZoneEdit.RenameArea(map, request);
        }

        public ApiResult<AreaCellsDto> InvertArea(AreaChangeRequestDto request)
        {
            if (!TryMap(request?.MapId ?? 0, out Map map, out string error))
            {
                return ApiResult<AreaCellsDto>.Fail(error);
            }
            return ZoneEdit.InvertArea(map, request);
        }

        public ApiResult<AreaCellsDto> ShrinkArea(ShrinkAreaRequestDto request)
        {
            if (!TryMap(request?.MapId ?? 0, out Map map, out string error))
            {
                return ApiResult<AreaCellsDto>.Fail(error);
            }
            return ZoneEdit.ShrinkArea(map, request);
        }

        public ApiResult DeleteArea(AreaChangeRequestDto request)
        {
            if (!TryMap(request?.MapId ?? 0, out Map map, out string error))
            {
                return ApiResult.Fail(error);
            }
            return ZoneEdit.DeleteArea(map, request);
        }

        public ApiResult<StorageSettingsDto> UpdateThingStorage(UpdateStorageRequestDto request) => MapHelper.UpdateThingStorage(request);
    }
}
