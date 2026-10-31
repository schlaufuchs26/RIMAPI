using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    // The colony's ideoligion roles (ticket #3186). The alert "Ritual Pujari
    // role unfilled" and the -5 "no priest" thought had no lever in the tool,
    // and neither the Assign tab nor the ideoligion window offered a slot a
    // click could reach, so a run could only watch the mood drain.
    public class IdeoController
    {
        private readonly IIdeoService _ideoService;

        public IdeoController(IIdeoService ideoService)
        {
            _ideoService = ideoService;
        }

        [Get("/api/v1/ideo/roles")]
        [EndpointMetadata("The colony ideoligion's roles with the pawns that fill them")]
        public async Task GetRoles(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _ideoService.GetRoles(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ideo/role/assign")]
        [EndpointMetadata("Fill an ideoligion role with one colonist (the game's own assign)")]
        public async Task AssignRole(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            int pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            string role = RequestParser.GetStringParameter(context, "role");
            var result = _ideoService.AssignRole(mapId, pawnId, role);
            await context.SendJsonResponse(result);
        }
    }
}
