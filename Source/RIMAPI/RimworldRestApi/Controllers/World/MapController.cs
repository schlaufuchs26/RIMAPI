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
        [ResponseBody(typeof(List<MapDto>))]
        [EndpointMetadata("Get all generated maps list the in game session")]
        public async Task GetGameState(HttpListenerContext context)
        {
            var result = _mapService.GetMaps();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/things")]
        [ResponseBody(typeof(List<ThingDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get the items on the map", Notes = "Get the items lying on the map; cells the colony has not explored are left out")]
        public async Task GetMapThings(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapThings(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/things-at")]
        [ResponseBody(typeof(List<ThingDto>))]
        [RequestBody(typeof(ThingsAtCellRequestDto))]
        [EndpointMetadata("Get the things in one cell", Notes = "Get the things standing in one cell; a cell the colony has not explored answers nothing")]
        public async Task GetThingsAtCell(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<ThingsAtCellRequestDto>();
            var result = _mapService.GetThingsAtCell(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/things/radius")]
        [ResponseBody(typeof(List<ThingDto>))]
        [QueryParameters("map_id:integer", "radius:integer", "x:integer", "z:integer")]
        [EndpointMetadata("Get things within a radius", Notes = "Get the items, buildings and plants within a radius of one cell; cells the colony has not explored are left out")]
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
        [ResponseBody(typeof(List<ThingDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the visible plants", Notes = "Get all plants (trees, bushes, crops) the colony can see; fogged cells are left out")]
        public async Task GetMapPlants(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapPlants(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/plants/area")]
        [ResponseBody(typeof(List<PlantDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get the plants on a rectangle", Notes = "Get the plants standing on a rectangle (x1, z1, x2, z2): defName, growth and cell of each, fogged cells left out")]
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
        [ResponseBody(typeof(List<DesignationDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List every designation", Notes = "Get every designation on the map: what is marked for chopping, mining, deconstructing, harvesting or hunting, with its cell or thing")]
        public async Task GetMapDesignations(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapDesignations(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/blueprints")]
        [ResponseBody(typeof(List<BlueprintEntryDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the blueprints and frames", Notes = "Get every blueprint and frame on the map: the def it builds, its cell, its material and the work left")]
        public async Task GetMapBlueprints(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapBlueprints(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/pens")]
        [ResponseBody(typeof(List<PenDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the animal pens", Notes = "Get every animal pen on the map: the marker's cell, the game's enclosure verdict, the animals standing inside and the pen tab's food numbers and animal capacity")]
        public async Task GetMapPens(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapPens(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/area")]
        [ResponseBody(typeof(AreaCellsDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get an area's cells", Notes = "Get one area's cells as rectangles: where the Home area reaches, which is where colonists haul and therefore where a blueprint is ever built")]
        public async Task GetMapArea(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var label = RequestParser.GetStringParameter(context, "label", required: false);
            var result = _mapService.GetMapArea(mapId, label);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/area/expand")]
        [ResponseBody(typeof(AreaCellsDto))]
        [RequestBody(typeof(AreaExpandRequestDto))]
        [EndpointMetadata("Extend an area", Notes = "Extend one area over a rectangle, the game's own expand designator applied at once")]
        public async Task ExpandMapArea(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<AreaExpandRequestDto>();
            var result = _mapService.ExpandMapArea(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/weather")]
        [ResponseBody(typeof(MapWeatherDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get the weather")]
        public async Task GetMapWeather(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetWeather(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/power/info")]
        [ResponseBody(typeof(MapPowerInfoDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get the map's power grid")]
        public async Task GetMapPowerInfo(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapPowerInfo(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/ore")]
        [ResponseBody(typeof(OreDataDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get the ore the colony has found", Notes = "Get the ore the colony has found, grouped by def: cells and hit points per deposit, fogged cells left out")]
        public async Task GetMapOre(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapOre(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/animals")]
        [ResponseBody(typeof(List<AnimalDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the visible animals", Notes = "Get the animals the colony can see: id, race, name, side (wild or tame), sex, life stage, health, the tame and hunt designations, and whether it is a manhunter or a predator; animals in fogged cells are left out")]
        public async Task GetMapAnimals(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapAnimals(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/creatures/summary")]
        [ResponseBody(typeof(MapCreaturesSummaryDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get a creature summary")]
        public async Task GetMapCreaturesSummary(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapCreaturesSummary(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/farm/summary")]
        [ResponseBody(typeof(MapFarmSummaryDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get a farm summary")]
        public async Task GetMapFarmSummary(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GenerateFarmSummary(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/zone/growing")]
        [ResponseBody(typeof(GrowingZoneDto))]
        [QueryParameters("map_id:integer", "zone_id:integer")]
        [EndpointMetadata("Get one growing zone")]
        public async Task GetMapGrowingZoneById(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var zoneId = RequestParser.GetZoneId(context);
            var result = _mapService.GetGrowingZoneById(mapId, zoneId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/zone/growing")]
        [ResponseBody(typeof(GrowingZoneDto))]
        [RequestBody(typeof(CreateGrowingZoneRequestDto))]
        [EndpointMetadata("Create a growing zone")]
        public async Task CreateGrowingZone(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<CreateGrowingZoneRequestDto>();
            var result = _mapService.CreateGrowingZone(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/zone/growing/plants")]
        [ResponseBody(typeof(GrowingZonePlantsDto))]
        [QueryParameters("map_id:integer", "zone_id:integer")]
        [EndpointMetadata("List the crops a growing zone can sow", Notes = "The crops this zone's Plant button offers: the game's own list, minus the ones whose research is missing, with the Plants skill each needs")]
        public async Task GetGrowingZonePlants(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var zoneId = RequestParser.GetZoneId(context);
            var result = _mapService.GetGrowingZonePlants(mapId, zoneId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/zone/growing/update")]
        [ResponseBody(typeof(GrowingZoneDto))]
        [RequestBody(typeof(UpdateGrowingZoneRequestDto))]
        [EndpointMetadata("Set a growing zone's crop and toggles", Notes = "The Plant button's crop and the Sow and Cut gizmos; a crop outside the zone's own list is refused, and a crop no colonist can plant answers the game's warning")]
        public async Task UpdateGrowingZone(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<UpdateGrowingZoneRequestDto>();
            var result = _mapService.UpdateGrowingZone(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/zone/rename")]
        [ResponseBody(typeof(ZoneDto))]
        [RequestBody(typeof(RenameZoneRequestDto))]
        [EndpointMetadata("Rename a zone", Notes = "The zone's rename dialog: an empty name, one longer than 28 characters, one another zone carries or one a storage group carries is refused")]
        public async Task RenameZone(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<RenameZoneRequestDto>();
            var result = _mapService.RenameZone(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/area")]
        [ResponseBody(typeof(AreaCellsDto))]
        [RequestBody(typeof(CreateAreaRequestDto))]
        [EndpointMetadata("Create an allowed area", Notes = "The Manage areas dialog's New area button: an empty allowed area, at most 10 per map, with the game's own default name unless one is sent")]
        public async Task CreateArea(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<CreateAreaRequestDto>();
            var result = _mapService.CreateArea(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/area/rename")]
        [ResponseBody(typeof(AreaCellsDto))]
        [RequestBody(typeof(RenameAreaRequestDto))]
        [EndpointMetadata("Rename an allowed area", Notes = "The Manage areas dialog's Rename button, under the dialog's own rules; the game's own areas (Home, Build roof) have no rename")]
        public async Task RenameArea(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<RenameAreaRequestDto>();
            var result = _mapService.RenameArea(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/area/invert")]
        [ResponseBody(typeof(AreaCellsDto))]
        [RequestBody(typeof(AreaChangeRequestDto))]
        [EndpointMetadata("Invert an allowed area", Notes = "The Manage areas dialog's Invert button: every cell outside the area comes in and every cell inside goes out, over the whole map grid")]
        public async Task InvertArea(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<AreaChangeRequestDto>();
            var result = _mapService.InvertArea(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/area/shrink")]
        [ResponseBody(typeof(AreaCellsDto))]
        [RequestBody(typeof(ShrinkAreaRequestDto))]
        [EndpointMetadata("Shrink an area by a rectangle", Notes = "The Manage areas dialog's Shrink button: clear a rectangle from the area and report how many cells left it, how many were never in it and how many were outside the map")]
        public async Task ShrinkArea(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<ShrinkAreaRequestDto>();
            var result = _mapService.ShrinkArea(body);
            await context.SendJsonResponse(result);
        }

        [Delete("/api/v1/map/area/delete")]
        [ResponseBody]
        [QueryParameters("map_id:integer", "label:string?")]
        [EndpointMetadata("Delete an allowed area", Notes = "The Manage areas dialog's Delete button: the area leaves the list and every pawn restricted to it loses the restriction; the game's own areas cannot be deleted")]
        public async Task DeleteArea(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var label = RequestParser.GetStringParameter(context, "label", false);
            var result = _mapService.DeleteArea(
                new AreaChangeRequestDto { MapId = mapId, Label = label }
            );
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/zones")]
        [ResponseBody(typeof(MapZonesDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get zones on the map")]
        public async Task GetMapZones(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapZones(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/storage/unwanted")]
        [ResponseBody(typeof(UnwantedFoodDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get the edible things no storage on the map accepts")]
        public async Task GetUnwantedFood(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetUnwantedFood(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/storage/capacity")]
        [ResponseBody(typeof(StorageCapacityDto))]
        [QueryParameters("def:string=The def's internal name, e.g. \"Steel\".", "map_id:integer")]
        [EndpointMetadata("Get the storage room the map has for one product")]
        public async Task GetStorageCapacity(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            string defName = RequestParser.GetStringParameter(context, "def");
            var result = _mapService.GetStorageCapacity(mapId, defName);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/food/rotting")]
        [ResponseBody(typeof(MapRottingFoodDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get the perishable food", Notes = "Get the perishable food in rooms above 0 C, with its room and the hours it has left")]
        public async Task GetRottingFood(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetRottingFood(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/rooms")]
        [ResponseBody(typeof(MapRoomsDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the map's rooms")]
        public async Task GetMapRooms(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapRooms(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/terrain")]
        [ResponseBody(typeof(MapTerrainDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get the terrain grid", Notes = "Get the map's natural terrain and constructed floors as a run-length grid; x1, z1, x2, z2 narrow it to that rectangle")]
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
        [ResponseBody(typeof(FogGridDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get the fog grid")]
        public async Task GetMapFogGrid(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetFogGrid(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/buildings")]
        [ResponseBody(typeof(List<BuildingDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the colony's buildings", Notes = "Get the colony's buildings; a building in a cell the colony has not explored is left out")]
        public async Task GetMapBuildings(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.GetMapBuildings(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/building/info")]
        [ResponseBody(typeof(BuildingDto))]
        [QueryParameters("building_id:integer")]
        [EndpointMetadata("Get building info")]
        public async Task GetBuildingInfo(HttpListenerContext context)
        {
            // The route answers for one building, so it reads a building id;
            // `map_id` was the stock spelling and still works (fox #3705).
            var buildingId = RequestParser.GetBuildingId(context);
            var result = _buildingService.GetBuildingInfo(buildingId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/building/power")]
        [EndpointMetadata("Toggle a building's power")]
        [ResponseBody(typeof(PoweredBuildingDto))]
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
        [ResponseBody(typeof(BedDto))]
        [EndpointMetadata("Set a humanlike bed for prisoners (the bed's own toggle)")]
        public async Task SetBedForPrisoners(HttpListenerContext context)
        {
            var buildingId = RequestParser.GetIntParameter(context, "buildingId");
            var forPrisoners = RequestParser.GetBooleanParameter(context, "forPrisoners");
            var result = _buildingService.SetBedForPrisoners(buildingId, forPrisoners);
            await context.SendJsonResponse(result);
        }

        // A bed's owner: the game's own bed assignment (fox ticket #3771).
        // `pawnId` names the colonist who takes the bed; a call without one
        // clears the bed's owner, so a colonist comes off a bed without the bed
        // being taken apart.
        [Post("/api/v1/map/building/bed-owner")]
        [EndpointMetadata("Assign a bed to a colonist")]
        [ResponseBody(typeof(BedOwnerDto))]
        public async Task SetBedOwner(HttpListenerContext context)
        {
            var buildingId = RequestParser.GetIntParameter(context, "buildingId");
            var pawnId = RequestParser.HasParameter(context, "pawnId")
                ? RequestParser.GetIntParameter(context, "pawnId")
                : 0;
            var result = _buildingService.SetBedOwner(buildingId, pawnId);
            await context.SendJsonResponse(result);
        }

        // A bed's own medical setting (fox ticket #4235): the bed tab's
        // "Medical" gizmo. A call without `medical` reads the flag, with it the
        // call sets the flag and answers what the game holds afterwards. The
        // medicine policy is the pawn's, not the bed's.
        [Post("/api/v1/map/building/bed-medical")]
        [ResponseBody(typeof(BedMedicalDto))]
        [QueryParameters("medical:boolean?=True marks the bed for medical use, false clears the setting.")]
        [EndpointMetadata("Get or set a bed's medical setting", Notes = "Read or set a bed's own medical setting (the medical bed toggle)")]
        public async Task SetBedMedical(HttpListenerContext context)
        {
            var buildingId = RequestParser.GetIntParameter(context, "buildingId");
            bool? medical = RequestParser.HasParameter(context, "medical")
                ? (bool?)RequestParser.GetBooleanParameter(context, "medical")
                : null;
            var result = _buildingService.SetBedMedical(buildingId, medical);
            await context.SendJsonResponse(result);
        }

        // A cooler's and a heater's target temperature (fox ticket #3744): the
        // one setting that decides whether a room freezes. A call without
        // `target` reads it, with `target` it sets it and reads it back.
        [Post("/api/v1/map/building/temperature-target")]
        [ResponseBody(typeof(TemperatureTargetDto))]
        [QueryParameters("target:number=The target temperature in degrees Celsius.")]
        [EndpointMetadata("Read or set a cooler's or heater's target temperature")]
        public async Task SetTemperatureTarget(HttpListenerContext context)
        {
            var buildingId = RequestParser.GetIntParameter(context, "buildingId");
            float? target = RequestParser.HasParameter(context, "target")
                ? (float?)RequestParser.GetFloatParameter(context, "target")
                : null;
            var result = _buildingService.SetTemperatureTarget(buildingId, target);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/map/weather/change")]
        [QueryParameters("map_id:integer", "name:string=The weather def name, e.g. \"Clear\".")]
        [EndpointMetadata("Set weather on the map")]
        [ResponseBody]
        public async Task SetWeather(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var defName = RequestParser.GetStringParameter(context, "name");
            var result = _mapService.SetWeather(mapId, defName);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/map/destroy/corpses")]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Destroy the map's corpses")]
        [ResponseBody]
        public async Task DestroyCorpses(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.DestroyCorpses(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/map/destroy/forbidden")]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Destroy the forbidden items")]
        [ResponseBody]
        public async Task DestroyForbiddenItems(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _mapService.DestroyForbiddenItems(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/map/destroy/rect")]
        [RequestBody(typeof(DestroyRectRequestDto))]
        [EndpointMetadata("Destroy things in a rectangle")]
        [ResponseBody]
        public async Task DestroyThingsInRect(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<DestroyRectRequestDto>();
            var result = _mapService.DestroyThingsInRect(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/map/repair/positions")]
        [RequestBody(typeof(RepairPositionsRequestDto))]
        [EndpointMetadata("Repair things at positions")]
        [ResponseBody]
        public async Task RepairAtPositions(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<RepairPositionsRequestDto>();
            var result = _mapService.RepairThingsAtPositions(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/map/repair/rect")]
        [RequestBody(typeof(RepairRectRequestDto))]
        [EndpointMetadata("Repair things in a rectangle")]
        [ResponseBody]
        public async Task RepairInRect(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<RepairRectRequestDto>();
            var result = _mapService.RepairThingsInRect(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/map/droppod")]
        [RequestBody(typeof(SpawnDropPodRequestDto))]
        [EndpointMetadata("Spawn a drop pod")]
        [ResponseBody]
        public async Task SpawnDropPod(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<SpawnDropPodRequestDto>();
            var result = _mapService.SpawnDropPod(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/pawns")]
        [ResponseBody(typeof(List<PawnDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the pawns on a map")]
        public async Task GetPawnsOnMap(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _pawnInfoService.GetPawnsOnMap(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/map/people")]
        [ResponseBody(typeof(List<PersonDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the non-colonist people", Notes = "The humanlike people who are not free colonists: wild people, prisoners, guests, downed raiders")]
        public async Task GetPeople(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _pawnInfoService.GetPeople(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/zone/stockpile")]
        [ResponseBody(typeof(StockpileResponseDto))]
        [RequestBody(typeof(CreateStockpileRequestDto))]
        [EndpointMetadata("Create a new stockpile zone on the map")]
        public async Task CreateStockpile(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<CreateStockpileRequestDto>();
            var result = _mapService.CreateStockpile(body);
            await context.SendJsonResponse(result);
        }

        [Delete("/api/v1/map/zone/stockpile/delete")]
        [ResponseBody]
        [QueryParameters("zone_id:integer")]
        [EndpointMetadata("Delete a stockpile zone by zone ID")]
        public async Task DeleteStockpile(HttpListenerContext context)
        {
            var zoneId = RequestParser.GetZoneId(context);
            var result = _mapService.DeleteStockpile(zoneId);
            await context.SendJsonResponse(result);
        }

        [Delete("/api/v1/map/zone/delete")]
        [ResponseBody]
        [QueryParameters("zone_id:integer")]
        [EndpointMetadata("Delete any zone (stockpile or growing) by zone ID")]
        public async Task DeleteZone(HttpListenerContext context)
        {
            var zoneId = RequestParser.GetZoneId(context);
            var result = _mapService.DeleteZone(zoneId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/zone/stockpile/update")]
        [ResponseBody(typeof(StockpileResponseDto))]
        [RequestBody(typeof(UpdateStockpileRequestDto))]
        [EndpointMetadata("Update stockpile zone parameters by zone ID")]
        public async Task UpdateStockpile(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<UpdateStockpileRequestDto>();
            var result = _mapService.UpdateStockpile(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/storage/update")]
        [ResponseBody(typeof(StorageSettingsDto))]
        [RequestBody(typeof(UpdateStorageRequestDto))]
        [EndpointMetadata("Get or set a container's storage", Notes = "Read or write a container's storage priority and filter by thing ID")]
        public async Task UpdateStorage(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<UpdateStorageRequestDto>();
            var result = _mapService.UpdateThingStorage(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/datetime")]
        [ResponseBody(typeof(MapTimeDto))]
        [EndpointMetadata("Get in-game date and time")]
        public async Task GetCurrentMapDatetime(HttpListenerContext context)
        {
            var result = _mapService.GetCurrentMapDatetime();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/datetime/tile")]
        [ResponseBody(typeof(MapTimeDto))]
        [QueryParameters("tile_id:integer")]
        [EndpointMetadata("Get in-game date and time in global map tile")]
        public async Task GetWorldTileDatetime(HttpListenerContext context)
        {
            var tileId = RequestParser.GetIntParameter(context, "tile_id");
            var result = _mapService.GetWorldTileDatetime(tileId);
            await context.SendJsonResponse(result);
        }

    }
}
