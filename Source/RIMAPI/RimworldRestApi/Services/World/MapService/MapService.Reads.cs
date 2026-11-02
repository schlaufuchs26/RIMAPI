using System;
using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

// The map's own reads: the map list, what stands on it, its weather and
// its fog (split out of MapService.cs, fox #3659).

namespace RIMAPI.Services
{
    public partial class MapService
    {
        public ApiResult<List<MapDto>> GetMaps()
        {
            var result = MapHelper.GetMaps();
            return ApiResult<List<MapDto>>.Ok(result);
        }

        public ApiResult<List<ThingDto>> GetMapThings(int mapId)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<List<ThingDto>>.Fail(error);
            }

            var result = MapHelper.GetMapThings(mapId);
            return ApiResult<List<ThingDto>>.Ok(result);
        }

        public ApiResult<List<ThingDto>> GetMapThingsInRadius(int mapId, int x, int z, int radius)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<List<ThingDto>>.Fail(error);
            }

            var result = MapHelper.GetMapThingsInRadius(mapId, x, z, radius);
            return ApiResult<List<ThingDto>>.Ok(result);
        }

        public ApiResult<List<ThingDto>> GetThingsAtCell(ThingsAtCellRequestDto body)
        {
            try
            {
                if (body == null)
                {
                    return ApiResult<List<ThingDto>>.Fail("Request body is null");
                }

                var map = MapHelper.GetMapByID(body.MapId);
                if (map == null)
                {
                    return ApiResult<List<ThingDto>>.Fail(MapLookup.MapNotFound(body.MapId));
                }

                if (body.Position == null)
                {
                    return ApiResult<List<ThingDto>>.Fail("Position cannot be null.");
                }

                IntVec3 cellPosition = new IntVec3(body.Position.X, body.Position.Y, body.Position.Z);

                // A cell the colony has not explored holds nothing a player can
                // point at (fox ticket #4220).
                if (MapHelper.IsFogged(map, cellPosition))
                {
                    return ApiResult<List<ThingDto>>.Ok(new List<ThingDto>());
                }

                List<Thing> things = cellPosition.GetThingList(map);
                List<ThingDto> thingDtos = new List<ThingDto>();

                foreach (Thing thing in things)
                {
                    thingDtos.Add(ResourcesHelper.ThingToDto(thing));
                }

                return ApiResult<List<ThingDto>>.Ok(thingDtos);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error getting things at cell: {ex}");
                return ApiResult<List<ThingDto>>.Fail(
                    $"Failed to get things at cell: {ex.Message}"
                );
            }
        }

        public ApiResult<List<BuildingDto>> GetMapBuildings(int mapId)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<List<BuildingDto>>.Fail(error);
            }

            var result = MapHelper.GetMapBuildings(mapId);
            return ApiResult<List<BuildingDto>>.Ok(result);
        }

        public ApiResult<MapCreaturesSummaryDto> GetMapCreaturesSummary(int mapId)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<MapCreaturesSummaryDto>.Fail(error);
            }

            var result = MapHelper.GetMapCreaturesSummary(mapId);
            return ApiResult<MapCreaturesSummaryDto>.Ok(result);
        }

        public ApiResult<MapPowerInfoDto> GetMapPowerInfo(int mapId)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<MapPowerInfoDto>.Fail(error);
            }

            var result = MapHelper.GetMapPowerInfoInternal(mapId);
            return ApiResult<MapPowerInfoDto>.Ok(result);
        }

        public ApiResult<OreDataDto> GetMapOre(int mapId)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<OreDataDto>.Fail(error);
            }

            var result = MapHelper.GetOreData(mapId);
            return ApiResult<OreDataDto>.Ok(result);
        }

        public ApiResult<MapRoomsDto> GetMapRooms(int mapId)
        {
            if (!TryMap(mapId, out Map map, out string error))
            {
                return ApiResult<MapRoomsDto>.Fail(error);
            }

            var result = MapHelper.GetRooms(map);
            return ApiResult<MapRoomsDto>.Ok(result);
        }

        public ApiResult<MapTerrainDto> GetMapTerrain(int mapId, TerrainRect.Box? box)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<MapTerrainDto>.Fail(error);
            }

            var result = MapHelper.GetMapTerrain(mapId, box);
            return ApiResult<MapTerrainDto>.Ok(result);
        }

        public ApiResult<List<AnimalDto>> GetMapAnimals(int mapId)
        {
            if (!TryMap(mapId, out _, out string error))
            {
                return ApiResult<List<AnimalDto>>.Fail(error);
            }

            var result = MapHelper.GetMapAnimals(mapId);
            return ApiResult<List<AnimalDto>>.Ok(result);
        }

        public ApiResult<FogGridDto> GetFogGrid(int mapId)
        {
            try
            {
                var map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult<FogGridDto>.Fail(MapLookup.MapNotFound(mapId));

                int totalCells = map.cellIndices.NumGridCells;
                FogGrid fogGrid = map.fogGrid;

                // RLE Compression Logic
                // We store pairs of counts: [Count Revealed, Count Fogged, Count Revealed...]
                // Since it's binary data, we don't need to store the value, just the alternating counts.
                // We assume the starting state is "Revealed" (false). If grid starts with Fogged, the first count is 0.

                var rleCounts = new List<int>();
                bool currentVal = false; // "false" = Revealed (IsFogged == false)
                int runLength = 0;

                for (int i = 0; i < totalCells; i++)
                {
                    // Note: fogGrid.IsFogged(i) returns true if HIDDEN
                    bool isFogged = fogGrid.IsFogged(i);

                    if (isFogged == currentVal)
                    {
                        runLength++;
                    }
                    else
                    {
                        rleCounts.Add(runLength);
                        currentVal = !currentVal; // Flip state
                        runLength = 1;
                    }
                }
                rleCounts.Add(runLength); // Add the final run

                // Optimize: Convert List<int> to Base64 String to save network bandwidth
                // Ints take 4 bytes. We convert the list to a byte array, then Base64 it.
                byte[] byteArray = new byte[rleCounts.Count * 4];
                Buffer.BlockCopy(rleCounts.ToArray(), 0, byteArray, 0, byteArray.Length);
                string base64Data = Convert.ToBase64String(byteArray);

                var fogGridDto = new FogGridDto
                {
                    MapId = map.uniqueID,
                    Width = map.Size.x,
                    Height = map.Size.z,
                    FogData = base64Data
                };

                return ApiResult<FogGridDto>.Ok(fogGridDto);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error getting fog grid: {ex}");
                return ApiResult<FogGridDto>.Fail(ex.Message);
            }
        }

        public ApiResult<MapWeatherDto> GetWeather(int mapId)
        {
            if (!TryMap(mapId, out Map map, out string error))
            {
                return ApiResult<MapWeatherDto>.Fail(error);
            }

            var result = new MapWeatherDto
            {
                Weather = map.weatherManager?.curWeather?.defName,
                Temperature = map.mapTemperature?.OutdoorTemp ?? 0f,
            };
            return ApiResult<MapWeatherDto>.Ok(result);
        }

        public ApiResult SetWeather(int mapId, string weatherDefName)
        {
            try
            {
                if (!TryMap(mapId, out Map map, out string error))
                {
                    return ApiResult.Fail(error);
                }

                WeatherDef weatherDef = DefDatabase<WeatherDef>.GetNamed(weatherDefName, false);
                if (weatherDef == null)
                {
                    return ApiResult.Fail(
                        $"Could not find weather Def with name: {weatherDefName}"
                    );
                }

                map.weatherManager.TransitionTo(weatherDef);
                return ApiResult.Ok();
            }
            catch (Exception ex)
            {
                return ApiResult.Fail(ex.Message);
            }
        }
    }
}
