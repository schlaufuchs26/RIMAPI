using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class ThingController
    {
        private readonly IResourceService _resourcesService;
        private readonly IThingsService _thingsService;

        public ThingController(IResourceService resourcesService, IThingsService thingsService)
        {
            _resourcesService = resourcesService;
            _thingsService = thingsService;
        }

        [Get("/api/v1/resources/summary")]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get a resources summary")]
        public async Task GetResourcesSummary(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _resourcesService.GetResourcesSummary(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/resources/stored")]
        [QueryParameters("category:string?", "map_id:integer")]
        [EndpointMetadata("List the stored resources")]
        public async Task GetResourcesStored(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var categoryDef = RequestParser.GetStringParameter(context, "category", false);

            if (string.IsNullOrEmpty(categoryDef))
            {
                var result = _resourcesService.GetAllStoredResources(mapId);
                await context.SendJsonResponse(result);
            }
            else
            {
                var result = _resourcesService.GetAllStoredResourcesByCategory(mapId, categoryDef);
                await context.SendJsonResponse(result);
            }
        }

        [Get("/api/v1/resources/storages/summary")]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Get a storage summary")]
        public async Task GetStoragesSummary(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _resourcesService.GetStoragesSummary(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/item/spawn")]
        [RequestBody(typeof(SpawnItemRequestDto))]
        [EndpointMetadata("Spawn an item")]
        public async Task SpawnItem(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<SpawnItemRequestDto>();
            var result = _resourcesService.SpawnItem(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/item/recipes")]
        [QueryParameters("def_name:string")]
        [EndpointMetadata("List an item's recipes")]
        public async Task GetItemRecipe(HttpListenerContext context)
        {
            var itemDef = RequestParser.GetStringParameter(context, "def_name");
            var result = _thingsService.GetItemRecipes(itemDef);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/item/sources")]
        [QueryParameters("def_name:string")]
        [EndpointMetadata("List where an item comes from")]
        public async Task GetItemSource(HttpListenerContext context)
        {
            var itemDef = RequestParser.GetStringParameter(context, "def_name");
            var result = _thingsService.GetItemSources(itemDef);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/things/set-forbidden")]
        [RequestBody(typeof(SetForbiddenRequestDto))]
        [EndpointMetadata("Set things forbidden")]
        public async Task SetForbidden(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<SetForbiddenRequestDto>();
            var result = _thingsService.SetForbidden(body);
            await context.SendJsonResponse(result);
        }
    }
}
