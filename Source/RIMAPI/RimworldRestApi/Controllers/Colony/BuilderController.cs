using System.Threading.Tasks;
using System.Net;
using RIMAPI.Core;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class BuilderController
    {
        private readonly IBuilderService _builderService;

        public BuilderController(IBuilderService builderService)
        {
            _builderService = builderService;
        }

        [Post("/api/v1/builder/copy")]
        [ResponseBody(typeof(BlueprintDto))]
        [RequestBody(typeof(CopyAreaRequestDto))]
        [EndpointMetadata("Copy an area")]
        public async Task CopyArea(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<CopyAreaRequestDto>();
            var result = _builderService.CopyArea(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/builder/paste")]
        [ResponseBody]
        [RequestBody(typeof(PasteAreaRequestDto))]
        [EndpointMetadata("Paste a copied area")]
        public async Task PasteArea(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<PasteAreaRequestDto>();
            var result = _builderService.PasteArea(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/builder/blueprint")]
        [ResponseBody(typeof(PlaceResultDto))]
        [RequestBody(typeof(PasteAreaRequestDto))]
        [EndpointMetadata("Place blueprints from a copy")]
        public async Task PlaceBlueprints(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<PasteAreaRequestDto>();
            var result = _builderService.PlaceBlueprints(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/builder/cancel")]
        [ResponseBody(typeof(CancelResultDto))]
        [RequestBody(typeof(CancelBlueprintsRequestDto))]
        [EndpointMetadata("Cancel blueprints in a rectangle", Notes = "Cancel every blueprint and frame inside a rectangle, the game's own cancel order")]
        public async Task CancelBlueprints(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<CancelBlueprintsRequestDto>();
            var result = _builderService.CancelBlueprints(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/builder/check-zone")]
        [ResponseBody(typeof(CheckZoneResultDto))]
        [RequestBody(typeof(CheckZoneRequestDto))]
        [EndpointMetadata("Check a build area for obstacles")]
        public async Task CheckZone(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<CheckZoneRequestDto>();
            var result = _builderService.CheckZone(body);
            await context.SendJsonResponse(result);
        }
    }
}