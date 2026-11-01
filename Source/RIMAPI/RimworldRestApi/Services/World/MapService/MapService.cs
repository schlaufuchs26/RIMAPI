using System;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

// The class shell and the clock reads. Every route family lives in its own
// file beside this one: MapService.Reads, .Farm, .Zones, .Designations, .Pens
// and .Operations, because the one file passed 950 lines (fox ticket #3659).
namespace RIMAPI.Services
{
    public partial class MapService : IMapService
    {
        public MapService() { }

        public static int GetMapTileId(Map map) => MapHelper.GetMapTileId(map);

        public ApiResult<MapTimeDto> GetCurrentMapDatetime()
        {
            try
            {
                var map = Find.CurrentMap;
                if (map == null)
                    return ApiResult<MapTimeDto>.Fail("No current map found");

                var time = GetDatetimeAt(GetMapTileId(Find.CurrentMap));
                return ApiResult<MapTimeDto>.Ok(time);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error getting current map datetime: {ex}");
                return ApiResult<MapTimeDto>.Fail($"Failed to get datetime: {ex.Message}");
            }
        }

        public ApiResult<MapTimeDto> GetWorldTileDatetime(int tileID)
        {
            try
            {
                var time = GetDatetimeAt(tileID);

                return ApiResult<MapTimeDto>.Ok(time);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error getting world tile datetime for tile {tileID}: {ex}");
                return ApiResult<MapTimeDto>.Fail($"Failed to get datetime: {ex.Message}");
            }
        }

        public MapTimeDto GetDatetimeAt(int tileID)
        {
            MapTimeDto mapTimeDto = new MapTimeDto();
            try
            {
                if (Current.ProgramState != ProgramState.Playing || Find.WorldGrid == null)
                {
                    return mapTimeDto;
                }

                var vector = Find.WorldGrid.LongLatOf(tileID);
                mapTimeDto.Datetime = GenDate.DateFullStringWithHourAt(
                    Find.TickManager.TicksAbs,
                    vector
                );

                return mapTimeDto;
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error - {ex}");
                return mapTimeDto;
            }
        }
    }
}
