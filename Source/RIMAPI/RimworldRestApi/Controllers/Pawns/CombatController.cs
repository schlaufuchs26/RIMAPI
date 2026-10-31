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

        public CombatController(ICombatService combatService)
        {
            _combatService = combatService;
        }

        [Get("/api/v1/map/combat")]
        [EndpointMetadata("Hostiles and colonists with position, weapon, health, drafted state and current job")]
        public async Task GetCombat(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _combatService.GetCombat(mapId);
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
