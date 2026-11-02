using System.Threading.Tasks;
using System.Net;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class PawnInfoController
    {
        private readonly IPawnInfoService _pawnInfoService;

        public PawnInfoController(IPawnInfoService pawnInfoService)
        {
            _pawnInfoService = pawnInfoService;
        }

        [Get("/api/v1/pawns/details")]
        [ResponseBody(typeof(PawnDetailedDto))]
        [QueryParameters("id:integer")]
        [EndpointMetadata("Get one pawn's details")]
        public async Task GetPawnDetails(HttpListenerContext context)
        {
            int pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _pawnInfoService.GetPawnDetails(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/pawns/inventory")]
        [ResponseBody(typeof(PawnInventoryDto))]
        [QueryParameters("id:integer")]
        [EndpointMetadata("Get one pawn's inventory")]
        public async Task GetPawnInventory(HttpListenerContext context)
        {
            int pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _pawnInfoService.GetPawnInventory(pawnId);
            await context.SendJsonResponse(result);
        }
    }
}