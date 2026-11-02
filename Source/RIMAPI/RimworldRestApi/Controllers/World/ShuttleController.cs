using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    /// <summary>
    /// The shuttles on a map and the boarding order (fox ticket #3862). A quest
    /// shuttle waits for the pawns its quest names; the game's own lever is the
    /// right-click "Enter shuttle" on the shuttle, and these two routes are
    /// that lever: `shuttles` names the cell and the people it waits for,
    /// `shuttle/enter` sends one named pawn. Sending a loaded shuttle off
    /// before the quest's timer runs out is `shuttle/launch` (ticket #4150),
    /// the game's own send-away order.
    /// </summary>
    public class ShuttleController
    {
        private readonly IShuttleService _shuttleService;

        public ShuttleController(IShuttleService shuttleService)
        {
            _shuttleService = shuttleService;
        }

        [Get("/api/v1/map/shuttles")]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("The shuttles on a map with their cell, the pawns and items they wait for, and the quest's countdown")]
        public async Task GetShuttles(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _shuttleService.Shuttles(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/shuttle/enter")]
        [RequestBody(typeof(ShuttleEnterRequestDto))]
        [EndpointMetadata("Send a named pawn onto a landed shuttle, the game's own right-click boarding order")]
        public async Task EnterShuttle(HttpListenerContext context)
        {
            // The ids travel in the JSON body, as on every other write of the
            // mod; the query string is the fallback for a hand-run curl.
            var body = await context.Request.ReadBodyAsync<ShuttleEnterRequestDto>();
            int pawnId = body != null && body.PawnId > 0 ? body.PawnId : RequestParser.GetIntParameter(context, "pawnId");
            int shuttleId = body != null && body.ShuttleId > 0 ? body.ShuttleId : 0;
            if (shuttleId == 0 && RequestParser.HasParameter(context, "shuttleId"))
                shuttleId = RequestParser.GetIntParameter(context, "shuttleId");
            var result = _shuttleService.Enter(pawnId, shuttleId);
            await context.SendJsonResponse(result);
        }

        // Sending a loaded shuttle off (fox ticket #4150): the game's own
        // "Send shuttle" button, which a player clicks instead of waiting out
        // the quest's three-day timer. It names no pawn; the shuttle is the one
        // that already holds everyone and everything its quest waits for, and a
        // shuttle that is not ready is refused with what is still missing.
        [Post("/api/v1/map/shuttle/launch")]
        [RequestBody(typeof(ShuttleLaunchRequestDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("Send a loaded shuttle away through the game's own send-away order, instead of waiting for the quest's timer")]
        public async Task LaunchShuttle(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<ShuttleLaunchRequestDto>();
            int mapId = body != null && body.MapId > 0 ? body.MapId : 0;
            if (mapId == 0 && RequestParser.HasParameter(context, "map_id"))
                mapId = RequestParser.GetIntParameter(context, "map_id");
            int shuttleId = body != null && body.ShuttleId > 0 ? body.ShuttleId : 0;
            if (shuttleId == 0 && RequestParser.HasParameter(context, "shuttleId"))
                shuttleId = RequestParser.GetIntParameter(context, "shuttleId");
            var result = _shuttleService.Launch(mapId, shuttleId);
            await context.SendJsonResponse(result);
        }
    }
}
