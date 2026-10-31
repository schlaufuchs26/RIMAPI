using System.Threading.Tasks;
using System.Net;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;
using System;

namespace RIMAPI.Controllers
{
    public class TradeController
    {
        private readonly ITradeService _tradeService;
        private readonly ICachingService _cachingService;

        public TradeController(ITradeService tradeService, ICachingService cachingService)
        {
            _tradeService = tradeService;
            _cachingService = cachingService;
        }

        [Get("/api/v1/traders/defs")]
        public async Task GetTradersDefs(HttpListenerContext context)
        {
            await _cachingService.CacheAwareResponseAsync(
                context,
                "/api/v1/traders/defs",
                dataFactory: () => Task.FromResult(_tradeService.GetAllTraderDefs()),
                expiration: TimeSpan.FromMinutes(5),
                priority: CachePriority.Normal,
                expirationType: CacheExpirationType.Absolute
            );
        }

        // The live traders on the map (ticket #3170): who is here, what silver
        // it holds and what it sells. Not cached: a caravan leaves, a ship
        // departs and a stock shrinks with every purchase.
        [Get("/api/v1/traders")]
        [EndpointMetadata("Traders on the map with their silver and stock")]
        public async Task GetTraders(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _tradeService.GetTraders(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/traders/trade")]
        [EndpointMetadata("Buy or sell one def through the game's own trade deal")]
        public async Task Trade(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<TradeRequestDto>();
            if (body != null && body.MapId == 0)
            {
                body.MapId = RequestParser.GetMapId(context);
            }
            var result = _tradeService.Trade(body);
            await context.SendJsonResponse(result);
        }
    }
}
