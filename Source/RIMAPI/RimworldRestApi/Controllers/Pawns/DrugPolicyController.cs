using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    /// <summary>
    /// The Assign tab's drug policy (fox ticket #4212, gap 3). The dialog's four
    /// actions and the per-pawn dropdown, so a colony can set the rules that keep
    /// its people off addiction instead of only reading that they exist.
    /// </summary>
    public class DrugPolicyController
    {
        private readonly IDrugPolicyService _drugPolicyService;

        public DrugPolicyController(IDrugPolicyService drugPolicyService)
        {
            _drugPolicyService = drugPolicyService;
        }

        [Get("/api/v1/drug-policies")]
        [EndpointMetadata("Read every drug policy with its per-drug rules and how many pawns follow it")]
        public async Task GetDrugPolicies(HttpListenerContext context)
        {
            var result = _drugPolicyService.GetAll();
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/drug-policies/create")]
        [RequestBody(typeof(DrugPolicyWriteRequest))]
        [EndpointMetadata("Create a drug policy, copying the default policy or a named one, the Manage drug policies dialog's New button")]
        public async Task CreateDrugPolicy(HttpListenerContext context)
        {
            DrugPolicyWriteRequest body = await ReadBody<DrugPolicyWriteRequest>(context);
            var result = _drugPolicyService.Create(body);
            await context.SendJsonResponse(result);
        }

        [Put("/api/v1/drug-policies/update")]
        [RequestBody(typeof(DrugPolicyWriteRequest))]
        [EndpointMetadata("Change a drug policy's label and the rules of the drugs it holds")]
        public async Task UpdateDrugPolicy(HttpListenerContext context)
        {
            DrugPolicyWriteRequest body = await ReadBody<DrugPolicyWriteRequest>(context);
            var result = _drugPolicyService.Update(body);
            await context.SendJsonResponse(result);
        }

        [Delete("/api/v1/drug-policies/delete")]
        [QueryParameters("id:integer")]
        [EndpointMetadata("Delete a drug policy; the game refuses its default one and one a pawn still follows")]
        public async Task DeleteDrugPolicy(HttpListenerContext context)
        {
            int id = RequestParser.GetIntParameter(context, "id");
            var result = _drugPolicyService.Delete(id);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/pawn/drug-policy")]
        [RequestBody(typeof(PawnDrugPolicyRequest))]
        [EndpointMetadata("Give a pawn a drug policy, the Assign tab's dropdown")]
        public async Task AssignDrugPolicy(HttpListenerContext context)
        {
            PawnDrugPolicyRequest body = await ReadBody<PawnDrugPolicyRequest>(context);
            var result = _drugPolicyService.Assign(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/drug-policies/set-default")]
        [QueryParameters("id:integer")]
        [EndpointMetadata("Make a drug policy the default one a new pawn gets, the dialog's star")]
        public async Task SetDefaultDrugPolicy(HttpListenerContext context)
        {
            int id = RequestParser.GetIntParameter(context, "id");
            var result = _drugPolicyService.SetDefault(id);
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
