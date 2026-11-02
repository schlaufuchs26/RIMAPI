using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Models;
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

        [Get("/api/v1/ideo")]
        [ResponseBody(typeof(IdeoSummaryDto))]
        [EndpointMetadata("Get the colony ideoligion", Notes = "The colony ideoligion: name, structure, memes, precepts and whether it can be reformed")]
        public async Task GetIdeo(HttpListenerContext context)
        {
            // The colony's ideoligion is not a map's: the read takes no map_id
            // so a caller can ask before it knows a map (ticket #3612).
            var result = _ideoService.GetIdeo();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/ideo/roles")]
        [ResponseBody(typeof(IdeoRolesDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the ideoligion roles")]
        public async Task GetRoles(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _ideoService.GetRoles(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ideo/role/assign")]
        [ResponseBody(typeof(RoleAssignResultDto))]
        [QueryParameters("map_id:integer", "pawn_id:integer", "role:string")]
        [EndpointMetadata("Assign a colonist to a role", Notes = "Fill an ideoligion role with one colonist (the game's own assign)")]
        public async Task AssignRole(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            int pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            string role = RequestParser.GetStringParameter(context, "role");
            var result = _ideoService.AssignRole(mapId, pawnId, role);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/ideo/rituals")]
        [ResponseBody(typeof(IdeoRitualsDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the ideoligion rituals", Notes = "The ideoligion's rituals with the game's readiness verdict and repeat penalty")]
        public async Task GetRituals(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _ideoService.GetRituals(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/ideo/ritual/start")]
        [ResponseBody(typeof(RitualStartResultDto))]
        [QueryParameters("map_id:integer", "pawn_id:integer", "ritual:string", "target_id:integer", "x:integer", "z:integer")]
        [EndpointMetadata("Start an ideoligion ritual", Notes = "Start an ideoligion ritual or the leader's speech (the game's own start)")]
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
        [ResponseBody(typeof(RitualEndResultDto))]
        [QueryParameters("map_id:integer", "ritual:string")]
        [EndpointMetadata("End a running ritual")]
        public async Task EndRitual(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            string ritual = RequestParser.GetStringParameter(context, "ritual");
            var result = _ideoService.EndRitual(mapId, ritual);
            await context.SendJsonResponse(result);
        }
    }
}
