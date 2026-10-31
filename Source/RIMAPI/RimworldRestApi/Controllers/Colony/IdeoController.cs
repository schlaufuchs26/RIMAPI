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

        [Get("/api/v1/ideo/rituals")]
        [EndpointMetadata("The ideoligion's rituals with the game's readiness verdict and repeat penalty")]
        public async Task GetRituals(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _ideoService.GetRituals(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ideo/ritual/start")]
        [EndpointMetadata("Start an ideoligion ritual or the leader's speech (the game's own start)")]
        public async Task StartRitual(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            string ritual = RequestParser.GetStringParameter(context, "ritual");
            // Every other field is optional, and RequestParser throws on a
            // missing one, so each is read only when the query carries it
            // (ticket #3263: a start without a pawn is the common call).
            int pawnId = RequestParser.HasParameter(context, "pawn_id")
                ? RequestParser.GetIntParameter(context, "pawn_id") : 0;
            int targetThingId = RequestParser.HasParameter(context, "target_id")
                ? RequestParser.GetIntParameter(context, "target_id") : 0;
            bool hasCell = RequestParser.HasParameter(context, "x") && RequestParser.HasParameter(context, "z");
            int x = hasCell ? RequestParser.GetIntParameter(context, "x") : 0;
            int z = hasCell ? RequestParser.GetIntParameter(context, "z") : 0;
            var result = _ideoService.StartRitual(mapId, ritual, pawnId, targetThingId, x, z, hasCell);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ideo/ritual/end")]
        [EndpointMetadata("End a running ritual through the game's own cancel")]
        public async Task EndRitual(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            string ritual = RequestParser.GetStringParameter(context, "ritual");
            var result = _ideoService.EndRitual(mapId, ritual);
            await context.SendJsonResponse(result);
        }
    }
}
