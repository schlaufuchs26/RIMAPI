using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    /// <summary>
    /// Forming a caravan (fox ticket #4212, wave 2). The world map was read-only:
    /// eleven read routes and no write, so a colony could see its neighbours and
    /// never visit them. This route runs the dialog's Send button.
    /// </summary>
    public class CaravanController
    {
        private readonly ICaravanService _caravanService;

        public CaravanController(ICaravanService caravanService)
        {
            _caravanService = caravanService;
        }

        [Post("/api/v1/world/caravan/form")]
        [EndpointMetadata(
            "Send named pawns and goods off as a caravan to a tile, the game's own form-and-send jobs"
        )]
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
    }
}
