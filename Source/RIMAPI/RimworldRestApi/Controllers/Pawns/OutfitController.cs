using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    /// <summary>
    /// The Assign tab's apparel policy (fox ticket #4212, gap 2). The read
    /// (`GET /api/v1/outfits`) already existed; these are the writes the Manage
    /// outfits dialog offers, so a colony can act on tattered apparel instead of
    /// only reading that it wears it.
    /// </summary>
    public class OutfitController
    {
        private readonly IOutfitService _outfitService;

        public OutfitController(IOutfitService outfitService)
        {
            _outfitService = outfitService;
        }

        [Post("/api/v1/outfits/create")]
        [RequestBody(typeof(ApparelPolicyRequest))]
        [EndpointMetadata("Create an apparel policy with a label, the Manage outfits dialog's New button")]
        public async Task CreateOutfit(HttpListenerContext context)
        {
            ApparelPolicyRequest body = await ReadBody<ApparelPolicyRequest>(context);
            var result = _outfitService.Create(body);
            await context.SendJsonResponse(result);
        }

        [Put("/api/v1/outfits/update")]
        [RequestBody(typeof(ApparelPolicyRequest))]
        [EndpointMetadata(
            "Change an apparel policy: its label, its allowed and disallowed apparel and special filters, its hit points and quality range"
        )]
        public async Task UpdateOutfit(HttpListenerContext context)
        {
            ApparelPolicyRequest body = await ReadBody<ApparelPolicyRequest>(context);
            var result = _outfitService.Update(body);
            await context.SendJsonResponse(result);
        }

        [Delete("/api/v1/outfits/delete")]
        [QueryParameters("id:integer")]
        [EndpointMetadata("Delete an apparel policy; the game refuses one a pawn still wears")]
        public async Task DeleteOutfit(HttpListenerContext context)
        {
            int id = RequestParser.GetIntParameter(context, "id");
            var result = _outfitService.Delete(id);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/pawn/outfit")]
        [RequestBody(typeof(PawnApparelPolicyRequest))]
        [EndpointMetadata("Assign an apparel policy to a pawn, the Assign tab's dropdown")]
        public async Task AssignOutfit(HttpListenerContext context)
        {
            PawnApparelPolicyRequest body = await ReadBody<PawnApparelPolicyRequest>(context);
            var result = _outfitService.Assign(body);
            await context.SendJsonResponse(result);
        }

        private static async Task<T> ReadBody<T>(HttpListenerContext context) where T : class
        {
            if (!context.Request.HasEntityBody)
                return null;
            return await context.Request.ReadBodyAsync<T>();
        }
    }
}
