using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    /// <summary>
    /// The settlement writes (fox ticket #4226, wave 2). A player can give up a
    /// home from its own gizmo bar; nothing outside the game could, so a colony
    /// that outgrew its map had no way to move.
    /// </summary>
    public class SettlementController
    {
        private readonly ISettlementService _settlementService;

        public SettlementController(ISettlementService settlementService)
        {
            _settlementService = settlementService;
        }

        [Post("/api/v1/world/settlement/abandon")]
        [ResponseBody(typeof(AbandonSettlementResultDto))]
        [RequestBody(typeof(AbandonSettlementRequest))]
        [EndpointMetadata("Abandon a settlement", Notes = "Abandon a player settlement, the Abandon button of its own gizmo bar, with the command's own refusal")]
        public async Task AbandonSettlement(HttpListenerContext context)
        {
            AbandonSettlementRequest body = context.Request.HasEntityBody
                ? await context.Request.ReadBodyAsync<AbandonSettlementRequest>()
                : null;
            var result = _settlementService.Abandon(body);
            await context.SendJsonResponse(result);
        }
    }
}
