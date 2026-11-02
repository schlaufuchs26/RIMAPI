using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class GlobalMapController : BaseController
    {
        private readonly IGlobalMapService _globalMapService;
        private readonly ICachingService _cachingService;

        public GlobalMapController(IGlobalMapService globalMapService, ICachingService cachingService)
        {
            _globalMapService = globalMapService;
            _cachingService = cachingService;
        }

        [Get("/api/v1/world/caravans")]
        [EndpointMetadata("List the world caravans")]
        public async Task GetCaravans(HttpListenerContext context)
        {
            await _cachingService.CacheAwareResponseAsync(
                context,
                "caravans",
                () => Task.FromResult(_globalMapService.GetCaravans()),
                expiration: TimeSpan.FromSeconds(3),
                expirationType: CacheExpirationType.GameTick
            );
        }

        [Get("/api/v1/world/caravan/path")]
        [QueryParameters("id:integer")]
        [EndpointMetadata("Get a caravan's path")]
        public async Task GetCaravanPath(HttpListenerContext context)
        {
            var id = RequestParser.GetIntParameter(context, "id");
            await _cachingService.CacheAwareResponseAsync(
                context,
                "caravans",
                () => Task.FromResult(_globalMapService.GetCaravanPath(id)),
                expiration: TimeSpan.FromSeconds(1),
                expirationType: CacheExpirationType.GameTick
            );
        }

        [Get("/api/v1/world/settlements")]
        [EndpointMetadata("List the world settlements")]
        public async Task GetSettlements(HttpListenerContext context)
        {
            await _cachingService.CacheAwareResponseAsync(
                context,
                "settlements",
                () => Task.FromResult(_globalMapService.GetSettlements()),
                expiration: TimeSpan.FromSeconds(10),
                expirationType: CacheExpirationType.GameTick
            );
        }

        [Get("/api/v1/world/player/settlements")]
        [EndpointMetadata("List the player's settlements")]
        public async Task GetPlayerSettlements(HttpListenerContext context)
        {
            await _cachingService.CacheAwareResponseAsync(
                context,
                "player_settlements",
                () => Task.FromResult(_globalMapService.GetPlayerSettlements()),
                expiration: TimeSpan.FromSeconds(10),
                expirationType: CacheExpirationType.GameTick
            );
        }


        [Get("/api/v1/world/sites")]
        [EndpointMetadata("List the world sites")]
        public async Task GetSites(HttpListenerContext context)
        {
            await _cachingService.CacheAwareResponseAsync(
                context,
                "sites",
                () => Task.FromResult(_globalMapService.GetSites()),
                expiration: TimeSpan.FromSeconds(5),
                expirationType: CacheExpirationType.GameTick
            );
        }

        [Get("/api/v1/world/tile")]
        [QueryParameters("id:integer")]
        [EndpointMetadata("Get one world tile")]
        public async Task GetTile(HttpListenerContext context)
        {
            var tileId = RequestParser.GetIntParameter(context, "id");
            await _cachingService.CacheAwareResponseAsync(
                context,
                $"tile_{tileId}",
                () => Task.FromResult(_globalMapService.GetTile(tileId)),
                expiration: TimeSpan.FromSeconds(120),
                expirationType: CacheExpirationType.Absolute
            );
        }

        [Get("/api/v1/world/tile/coordinates")]
        [QueryParameters("id:integer")]
        [EndpointMetadata("Get a tile's coordinates")]
        public async Task GetTileCoordinates(HttpListenerContext context)
        {
            var tileId = RequestParser.GetIntParameter(context, "id");
            await _cachingService.CacheAwareResponseAsync(
                context,
                $"tile_{tileId}_coordinates",
                () => Task.FromResult(_globalMapService.GetTileCoordinates(tileId)),
                expiration: TimeSpan.FromSeconds(120),
                expirationType: CacheExpirationType.Absolute
            );
        }

        [Get("/api/v1/world/tile/details")]
        [QueryParameters("id:integer")]
        [EndpointMetadata("Get a tile's details")]
        public async Task GetTileDetails(HttpListenerContext context)
        {
            var tileId = RequestParser.GetIntParameter(context, "id");
            await _cachingService.CacheAwareResponseAsync(
                context,
                $"tile_{tileId}_details",
                () => Task.FromResult(_globalMapService.GetTileDetails(tileId)),
                expiration: TimeSpan.FromSeconds(120),
                expirationType: CacheExpirationType.Absolute
            );
        }

        [Get("/api/v1/world/landing")]
        [EndpointMetadata("Get the chosen landing site")]
        public async Task GetLanding(HttpListenerContext context)
        {
            // The site a founding picked from its wish list (ticket #3930).
            // The choice is made inside the world generation, so it is read
            // back here rather than returned by the start call.
            LandingChoiceDto choice = LandingSitePick.Last;
            if (choice == null)
            {
                await context.SendJsonResponse(
                    ApiResult<LandingChoiceDto>.Fail("No founding asked for a site in this game."));
                return;
            }
            await context.SendJsonResponse(ApiResult<LandingChoiceDto>.Ok(choice));
        }



        [Get("/api/v1/world/grid")]
        [EndpointMetadata("Get the world grid")]
        public async Task GetWorldGrid(HttpListenerContext context)
        {
            await _cachingService.CacheAwareResponseAsync(
                context,
                "world_grid",
                () => Task.FromResult(ApiResult<List<TileDto>>.Ok(GlobalMapHelper.GetWorldData())),
                expiration: TimeSpan.FromSeconds(600),
                expirationType: CacheExpirationType.Absolute
            );
        }

        [Get("/api/v1/world/grid/area")]
        [QueryParameters("radius:number?", "tile_id:integer?")]
        [EndpointMetadata("Get the world grid around a tile")]
        public async Task GetWorldGridArea(HttpListenerContext context)
        {
            if (!int.TryParse(context.Request.QueryString["tile_id"], out var tileId) ||
                !float.TryParse(context.Request.QueryString["radius"], out var radius))
            {
                await ResponseBuilder.SendError(context.Response, HttpStatusCode.BadRequest, "Invalid parameters. 'tile_id' (int) and 'radius' (float) are required.");
                return;
            }

            await _cachingService.CacheAwareResponseAsync(
                context,
                $"world_grid_area_{tileId}_{radius}",
                () => Task.FromResult(_globalMapService.GetTilesInRadius(tileId, radius)),
                expiration: TimeSpan.FromSeconds(600),
                expirationType: CacheExpirationType.Absolute
            );
        }
    }
}
