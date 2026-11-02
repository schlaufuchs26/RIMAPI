using System.Threading.Tasks;
using System.Net;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;

namespace RIMAPI.Controllers
{
    public class ServerCacheController
    {
        private readonly ICachingService _cachingService;

        public ServerCacheController(ICachingService cachingService)
        {
            _cachingService = cachingService;
        }

        [Post("/api/v1/cache/enable")]
        [EndpointMetadata("Enable the response cache")]
        [ResponseBody(typeof(ServerCacheResponseDto))]
        public async Task EnableCache(HttpListenerContext context)
        {
            var result = _cachingService.SetEnabled(true);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/cache/disable")]
        [EndpointMetadata("Disable the response cache")]
        [ResponseBody(typeof(ServerCacheResponseDto))]
        public async Task DisableCache(HttpListenerContext context)
        {
            var result = _cachingService.SetEnabled(false);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/cache/status")]
        [QueryParameters("detailed:boolean?")]
        [EndpointMetadata("Get the response cache status")]
        [ResponseBody(typeof(CacheStatusDto))]
        public async Task GetCacheStatus(HttpListenerContext context)
        {
            var isDetailed = RequestParser.GetBooleanParameter(context, "detailed", false);
            var stats = _cachingService.GetStatistics(isDetailed);
            var status = new CacheStatusDto { Enabled = _cachingService.IsEnabled(), Statistics = stats };

            await context.SendJsonResponse(ApiResult<CacheStatusDto>.Ok(status));
        }

        [Post("/api/v1/cache/clear")]
        [EndpointMetadata("Clear the response cache")]
        [ResponseBody(typeof(ServerCacheResponseDto))]
        public async Task ClearCache(HttpListenerContext context)
        {
            var result = _cachingService.Clear();
            await context.SendJsonResponse(result);
        }
    }
}