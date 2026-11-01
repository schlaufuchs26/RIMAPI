using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    /// <summary>
    /// The shuttles on a map and the boarding order (fox ticket #3862). A quest
    /// shuttle waits for the pawns its quest names; the game's own lever is the
    /// right-click "Enter shuttle" on the shuttle, and these two routes are
    /// that lever: `shuttles` names the cell and the people it waits for,
    /// `shuttle/enter` sends one named pawn.
    /// </summary>
    public class ShuttleController
    {
        private readonly IShuttleService _shuttleService;

        public ShuttleController(IShuttleService shuttleService)
        {
            _shuttleService = shuttleService;
        }

        [Get("/api/v1/map/shuttles")]
        [EndpointMetadata("The shuttles on a map with their cell, the pawns and items they wait for, and the quest's countdown")]
        public async Task GetShuttles(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _shuttleService.Shuttles(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/map/shuttle/enter")]
        [EndpointMetadata("Send a named pawn onto a landed shuttle, the game's own right-click boarding order")]
        public async Task EnterShuttle(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "pawnId");
            var shuttleId = RequestParser.HasParameter(context, "shuttleId")
                ? RequestParser.GetIntParameter(context, "shuttleId")
                : 0;
            var result = _shuttleService.Enter(pawnId, shuttleId);
            await context.SendJsonResponse(result);
        }
    }
}
