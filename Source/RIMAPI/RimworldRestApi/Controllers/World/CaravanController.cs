using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    /// <summary>
    /// The caravan writes (fox tickets #4212 and #4226, wave 2). The world map was
    /// read-only: eleven read routes and no write, so a colony could see its
    /// neighbours and never visit them. Form runs the form-caravan dialog's Send
    /// button; steer, trade and settle run what the world map's own right-click and
    /// the caravan's gizmo bar run.
    /// </summary>
    public class CaravanController
    {
        private readonly ICaravanService _caravanService;
        private readonly ISettlementService _settlementService;

        public CaravanController(
            ICaravanService caravanService,
            ISettlementService settlementService
        )
        {
            _caravanService = caravanService;
            _settlementService = settlementService;
        }

        [Post("/api/v1/world/caravan/form")]
        [ResponseBody(typeof(FormCaravanResultDto))]
        [RequestBody(typeof(FormCaravanRequest))]
        [QueryParameters("destination_tile:integer?", "map_id:integer?")]
        [EndpointMetadata("Form a caravan", Notes = "Send named pawns and goods off as a caravan to a tile, the game's own form-and-send jobs")]
        public async Task FormCaravan(HttpListenerContext context)
        {
            // map_id is optional here: the body carries it, the query is the
            // fallback, and map 0 is the colony's own map (ControllerParameterTests
            // refuses a second read of the same parameter).
            int mapId = 0;
            if (int.TryParse(context.Request.QueryString["map_id"], out int fromQuery))
                mapId = fromQuery;
            FormCaravanRequest body;
            if (context.Request.HasEntityBody)
            {
                body = await context.Request.ReadBodyAsync<FormCaravanRequest>();
            }
            else
            {
                body = new FormCaravanRequest
                {
                    DestinationTile = RequestParser.GetIntParameter(context, "destination_tile"),
                };
            }
            if (body == null)
                body = new FormCaravanRequest();
            if (body.MapId == 0)
                body.MapId = mapId;

            var result = _caravanService.Form(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/world/caravan/steer")]
        [ResponseBody(typeof(CaravanSteerResultDto))]
        [RequestBody(typeof(SteerCaravanRequest))]
        [EndpointMetadata("Steer a caravan", Notes = "Change a formed caravan's destination: a plain tile is a travel order, a settlement is the game's own Visit or Trade arrival action")]
        public async Task SteerCaravan(HttpListenerContext context)
        {
            SteerCaravanRequest body = context.Request.HasEntityBody
                ? await context.Request.ReadBodyAsync<SteerCaravanRequest>()
                : null;
            var result = _caravanService.Steer(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/world/caravan/trade")]
        [ResponseBody(typeof(CaravanTradeResultDto))]
        [RequestBody(typeof(CaravanTradeRequest))]
        [EndpointMetadata("Trade with a settlement", Notes = "Trade with a settlement: send the caravan there, or open the game's own trade dialog when it already stands at it")]
        public async Task TradeCaravan(HttpListenerContext context)
        {
            CaravanTradeRequest body = context.Request.HasEntityBody
                ? await context.Request.ReadBodyAsync<CaravanTradeRequest>()
                : null;
            var result = _caravanService.Trade(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/world/caravan/settle")]
        [ResponseBody(typeof(SettleCaravanResultDto))]
        [RequestBody(typeof(SettleCaravanRequest))]
        [EndpointMetadata("Settle a caravan", Notes = "Settle a new home where the caravan stands, the Settle button of the caravan's own gizmo bar")]
        public async Task SettleCaravan(HttpListenerContext context)
        {
            SettleCaravanRequest body = context.Request.HasEntityBody
                ? await context.Request.ReadBodyAsync<SettleCaravanRequest>()
                : null;
            var result = _settlementService.Settle(body);
            await context.SendJsonResponse(result);
        }
    }
}
