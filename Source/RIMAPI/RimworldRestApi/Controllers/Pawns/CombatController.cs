using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class CombatController
    {
        private readonly ICombatService _combatService;
        private readonly ICoverService _coverService;

        public CombatController(ICombatService combatService, ICoverService coverService)
        {
            _combatService = combatService;
            _coverService = coverService;
        }

        [Get("/api/v1/map/combat")]
        [EndpointMetadata("Hostiles and colonists with position, weapon, health, drafted state and current job")]
        public async Task GetCombat(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _combatService.GetCombat(mapId);
            await context.SendJsonResponse(result);
        }

        // Cover around a pawn (or a bare cell) against one target: the game's
        // own block chance per cell, the neighbouring things that give it, and
        // whether the cell has a line to the target (ticket #3174). Without a
        // target there is no direction, so the call needs one.
        [Get("/api/v1/map/cover")]
        [EndpointMetadata("Cover cells around a pawn or cell, with the game's cover chance and line of sight to a target")]
        public async Task GetCover(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            int pawnId = RequestParser.HasParameter(context, "pawn_id")
                ? RequestParser.GetIntParameter(context, "pawn_id")
                : 0;
            int x = RequestParser.HasParameter(context, "x") ? RequestParser.GetIntParameter(context, "x") : -1;
            int z = RequestParser.HasParameter(context, "z") ? RequestParser.GetIntParameter(context, "z") : -1;
            int radius = RequestParser.HasParameter(context, "radius")
                ? RequestParser.GetIntParameter(context, "radius")
                : 0;
            int targetId = RequestParser.HasParameter(context, "target_id")
                ? RequestParser.GetIntParameter(context, "target_id")
                : 0;
            int targetX = RequestParser.HasParameter(context, "target_x")
                ? RequestParser.GetIntParameter(context, "target_x")
                : -1;
            int targetZ = RequestParser.HasParameter(context, "target_z")
                ? RequestParser.GetIntParameter(context, "target_z")
                : -1;
            var result = _coverService.GetCover(
                mapId, pawnId, x, z, radius, targetId, targetX, targetZ);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/pawn/draft")]
        [EndpointMetadata("Draft or undraft a list of colonists")]
        public async Task SetDrafted(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<DraftRequestDto>();
            var result = _combatService.SetDrafted(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/pawn/order")]
        [EndpointMetadata("Order one drafted colonist to move, attack, hold or switch fire-at-will")]
        public async Task Order(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<OrderRequestDto>();
            var result = _combatService.Order(body);
            await context.SendJsonResponse(result);
        }
    }
}
