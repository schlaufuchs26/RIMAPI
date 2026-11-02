using System.Threading.Tasks;
using System.Collections.Generic;
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
        [ResponseBody(typeof(List<TraderKindDto>))]
        [EndpointMetadata("List the trader kinds")]
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
        [ResponseBody(typeof(List<TraderDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the traders on the map")]
        public async Task GetTraders(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _tradeService.GetTraders(mapId);
            await context.SendJsonResponse(result);
        }

        // The map's orbital trade beacons, whether or not a trade ship is in
        // orbit (fox ticket #3632). A beacon is a map fact: a colony reads it
        // to build, power and stock one before a trader arrives.
        [Get("/api/v1/traders/beacons")]
        [ResponseBody(typeof(List<TradeBeaconDto>))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the orbital trade beacons")]
        public async Task GetTradeBeacons(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _tradeService.GetTradeBeacons(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/traders/trade")]
        [ResponseBody(typeof(TradeResultDto))]
        [RequestBody(typeof(TradeRequestDto))]
        [QueryParameters("map_id:integer")]
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
