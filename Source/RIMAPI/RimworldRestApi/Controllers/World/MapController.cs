using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class MapController
    {
        private readonly IMapService _mapService;
        private readonly IBuildingService _buildingService;
        private readonly IPawnInfoService _pawnInfoService;

        public MapController(IMapService mapService, IBuildingService buildingService,
                             IPawnInfoService pawnInfoService)
        {
            _mapService = mapService;
            _buildingService = buildingService;
            _pawnInfoService = pawnInfoService;
        }

        [Get("/api/v1/maps")]
        [EndpointMetadata("Get all generated maps list the in game session")]
        public async Task GetGameState(HttpListenerContext context)
        {
            var result = _mapService.GetMaps();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/things")]
        public async Task GetMapThings(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapThings(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/things-at")]
        public async Task GetThingsAtCell(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<ThingsAtCellRequestDto>();
            var result = _mapService.GetThingsAtCell(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/things/radius")]
        public async Task GetMapThingsInRadius(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var x = RequestParser.GetIntParameter(context, "x");
            var z = RequestParser.GetIntParameter(context, "z");
            var radius = RequestParser.GetIntParameter(context, "radius");

            var result = _mapService.GetMapThingsInRadius(mapId, x, z, radius);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/plants")]
        [EndpointMetadata("Get all plants (trees, bushes, crops) on the map")]
        public async Task GetMapPlants(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapPlants(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/plants/area")]
        [EndpointMetadata("Get the plants standing on a rectangle (x1, z1, x2, z2): defName, growth and cell of each")]
        public async Task GetMapPlantsInRect(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var x1 = RequestParser.GetIntParameter(context, "x1");
            var z1 = RequestParser.GetIntParameter(context, "z1");
            var x2 = RequestParser.GetIntParameter(context, "x2");
            var z2 = RequestParser.GetIntParameter(context, "z2");
            var result = _mapService.GetMapPlantsInRect(mapId, x1, z1, x2, z2);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/designations")]
        [EndpointMetadata("Get every designation on the map: what is marked for chopping, mining, deconstructing, harvesting or hunting, with its cell or thing")]
        public async Task GetMapDesignations(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapDesignations(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/blueprints")]
        [EndpointMetadata("Get every blueprint and frame on the map: the def it builds, its cell, its material and the work left")]
        public async Task GetMapBlueprints(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapBlueprints(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/pens")]
        [EndpointMetadata("Get every animal pen on the map: the marker's cell, the game's enclosure verdict, the animals standing inside and the pen tab's food numbers and animal capacity")]
        public async Task GetMapPens(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapPens(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/area")]
        [EndpointMetadata("Get one area's cells as rectangles: where the Home area reaches, which is where colonists haul and therefore where a blueprint is ever built")]
        public async Task GetMapArea(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var label = RequestParser.GetStringParameter(context, "label", required: false);
            var result = _mapService.GetMapArea(mapId, label);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/area/expand")]
        [EndpointMetadata("Extend one area over a rectangle, the game's own expand designator applied at once")]
        public async Task ExpandMapArea(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<AreaExpandRequestDto>();
            var result = _mapService.ExpandMapArea(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/weather")]
        [EndpointMetadata("Get weather on the map")]
        public async Task GetMapWeather(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetWeather(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/power/info")]
        public async Task GetMapPowerInfo(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapPowerInfo(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/ore")]
        public async Task GetMapOre(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapOre(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/animals")]
        [EndpointMetadata("Get animals on the map: id, race, name, side (wild or tame), sex, life stage, health, the tame and hunt designations, and whether it is a manhunter or a predator")]
        public async Task GetMapAnimals(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapAnimals(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/creatures/summary")]
        public async Task GetMapCreaturesSummary(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapCreaturesSummary(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/farm/summary")]
        public async Task GetMapFarmSummary(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GenerateFarmSummary(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/zone/growing")]
        public async Task GetMapGrowingZoneById(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var zoneId = RequestParser.GetZoneId(context);
            var result = _mapService.GetGrowingZoneById(mapId, zoneId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/zone/growing")]
        [EndpointMetadata("Create a new growing zone with the specified plant and cells")]
        public async Task CreateGrowingZone(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<CreateGrowingZoneRequestDto>();
            var result = _mapService.CreateGrowingZone(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/zones")]
        [EndpointMetadata("Get zones on the map")]
        public async Task GetMapZones(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapZones(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/storage/unwanted")]
        [EndpointMetadata("Get the edible things no storage on the map accepts")]
        public async Task GetUnwantedFood(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetUnwantedFood(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/storage/capacity")]
        [EndpointMetadata("Get the storage room the map has for one product")]
        public async Task GetStorageCapacity(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            string defName = RequestParser.GetStringParameter(context, "def");
            var result = _mapService.GetStorageCapacity(mapId, defName);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/rooms")]
        public async Task GetMapRooms(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapRooms(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/terrain")]
        [EndpointMetadata("Get the map's natural terrain and constructed floors as a run-length grid; x1, z1, x2, z2 narrow it to that rectangle")]
        public async Task GetMapTerrain(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapTerrain(mapId, ReadTerrainBox(context));
            await context.SendJsonResponse(result);
        }

        // ReadTerrainBox takes the terrain read's optional rectangle: all four
        // corners or none. Half a rectangle has no meaning, and a caller that
        // sent two corners would silently get the whole map (fox ticket #3381).
        private static TerrainRect.Box? ReadTerrainBox(HttpListenerContext context)
        {
            string[] names = { "x1", "z1", "x2", "z2" };
            var corners = new int[names.Length];
            var missing = new List<string>();
            for (int i = 0; i < names.Length; i++)
            {
                string value = RequestParser.GetStringParameter(context, names[i], required: false);
                if (string.IsNullOrEmpty(value))
                {
                    missing.Add(names[i]);
                    continue;
                }
                if (!int.TryParse(value, out corners[i]))
                {
                    throw new ParameterFormatException(names[i], value, typeof(int));
                }
            }
            if (missing.Count == names.Length)
            {
                return null;
            }
            if (missing.Count > 0)
            {
                throw new ParameterNotFoundException(string.Join(", ", missing));
            }

            return new TerrainRect.Box
            {
                X1 = corners[0],
                Z1 = corners[1],
                X2 = corners[2],
                Z2 = corners[3],
            };
        }

        [Get("/api/v1/map/fog-grid")]
        public async Task GetMapFogGrid(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetFogGrid(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/buildings")]
        public async Task GetMapBuildings(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapBuildings(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/building/info")]
        [EndpointMetadata("Get building info")]
        public async Task GetBuildingInfo(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _buildingService.GetBuildingInfo(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/building/power")]
        [EndpointMetadata("Toggle power on/off for a flickable building")]
        public async Task SetBuildingPower(HttpListenerContext context)
        {
            var buildingId = RequestParser.GetIntParameter(context, "buildingId");
            var powerOn = RequestParser.GetBooleanParameter(context, "powerOn");
            var result = _buildingService.SetBuildingPower(buildingId, powerOn);
            await context.SendJsonResponse(result);
        }

        // A humanlike bed's "Set for prisoners" toggle: an arrest or a capture
        // needs a bed with that flag, and nothing could set it (ticket #3183).
        [Post("/api/v1/map/building/bed-for-prisoners")]
        [EndpointMetadata("Set a humanlike bed for prisoners (the bed's own toggle)")]
        public async Task SetBedForPrisoners(HttpListenerContext context)
        {
            var buildingId = RequestParser.GetIntParameter(context, "buildingId");
            var forPrisoners = RequestParser.GetBooleanParameter(context, "forPrisoners");
            var result = _buildingService.SetBedForPrisoners(buildingId, forPrisoners);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/weather/change")]
        [EndpointMetadata("Set weather on the map")]
        public async Task SetWeather(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var defName = RequestParser.GetStringParameter(context, "name");
            var result = _mapService.SetWeather(mapId, defName);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/destroy/corpses")]
        public async Task DestroyCorpses(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.DestroyCorpses(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/destroy/forbidden")]
        public async Task DestroyForbiddenItems(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.DestroyForbiddenItems(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/destroy/rect")]
        public async Task DestroyThingsInRect(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<DestroyRectRequestDto>();
            var result = _mapService.DestroyThingsInRect(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/repair/positions")]
        public async Task RepairAtPositions(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<RepairPositionsRequestDto>();
            var result = _mapService.RepairThingsAtPositions(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/repair/rect")]
        public async Task RepairInRect(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<RepairRectRequestDto>();
            var result = _mapService.RepairThingsInRect(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/droppod")]
        public async Task SpawnDropPod(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<SpawnDropPodRequestDto>();
            var result = _mapService.SpawnDropPod(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/pawns")]
        public async Task GetPawnsOnMap(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _pawnInfoService.GetPawnsOnMap(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/people")]
        [EndpointMetadata("The humanlike people who are not free colonists: wild people, prisoners, guests, downed raiders")]
        public async Task GetPeople(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _pawnInfoService.GetPeople(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/zone/stockpile")]
        [EndpointMetadata("Create a new stockpile zone on the map")]
        public async Task CreateStockpile(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<CreateStockpileRequestDto>();
            var result = _mapService.CreateStockpile(body);
            await context.SendJsonResponse(result);
        }

        [Delete("/api/v1/map/zone/stockpile/delete")]
        [EndpointMetadata("Delete a stockpile zone by zone ID")]
        public async Task DeleteStockpile(HttpListenerContext context)
        {
            var zoneId = RequestParser.GetZoneId(context);
            var result = _mapService.DeleteStockpile(zoneId);
            await context.SendJsonResponse(result);
        }

        [Delete("/api/v1/map/zone/delete")]
        [EndpointMetadata("Delete any zone (stockpile or growing) by zone ID")]
        public async Task DeleteZone(HttpListenerContext context)
        {
            var zoneId = RequestParser.GetZoneId(context);
            var result = _mapService.DeleteZone(zoneId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/zone/stockpile/update")]
        [EndpointMetadata("Update stockpile zone parameters by zone ID")]
        public async Task UpdateStockpile(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<UpdateStockpileRequestDto>();
            var result = _mapService.UpdateStockpile(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/datetime")]
        [EndpointMetadata("Get in-game date and time")]
        public async Task GetCurrentMapDatetime(HttpListenerContext context)
        {
            var result = _mapService.GetCurrentMapDatetime();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/datetime/tile")]
        [EndpointMetadata("Get in-game date and time in global map tile")]
        public async Task GetWorldTileDatetime(HttpListenerContext context)
        {
            var tileId = RequestParser.GetIntParameter(context, "tile_id");
            var result = _mapService.GetWorldTileDatetime(tileId);
            await context.SendJsonResponse(result);
        }

    }
}
