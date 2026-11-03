using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    /// <summary>
    /// The Health tab's operations (fox ticket #4212, gap 4). The bill that makes
    /// a doctor amputate or install a bionic, and the medicine policy that decides
    /// what the doctor may use while doing it.
    /// </summary>
    public class OperationController
    {
        private readonly IOperationService _operationService;

        public OperationController(IOperationService operationService)
        {
            _operationService = operationService;
        }

        [Get("/api/v1/pawn/operations")]
        [ResponseBody(typeof(PawnOperationsDto))]
        [QueryParameters("pawn_id:integer")]
        [EndpointMetadata("Get a pawn's operation bills", Notes = "Read a pawn's operation bills and medicine policy (?pawn_id=N)")]
        public async Task GetOperations(HttpListenerContext context)
        {
            int pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _operationService.GetOperations(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/pawn/surgery-recipes")]
        [ResponseBody(typeof(List<SurgeryRecipeDto>))]
        [QueryParameters("pawn_id:integer")]
        [EndpointMetadata("List the operations a pawn can get", Notes = "List the operations a pawn could be given, with the body parts each can target (?pawn_id=N)")]
        public async Task GetSurgeryRecipes(HttpListenerContext context)
        {
            int pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _operationService.GetSurgeryRecipes(pawnId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/pawn/operation")]
        [ResponseBody(typeof(PawnOperationsDto))]
        [RequestBody(typeof(AddOperationRequest))]
        [EndpointMetadata("Add an operation bill", Notes = "Add an operation bill, the health tab's Add bill: pawn_id, recipe, body_part")]
        public async Task AddOperation(HttpListenerContext context)
        {
            AddOperationRequest body = await ReadBody<AddOperationRequest>(context);
            var result = _operationService.Add(body);
            await context.SendJsonResponse(result);
        }

        [Delete("/api/v1/pawn/operation")]
        [ResponseBody(typeof(PawnOperationsDto))]
        [QueryParameters("id:string=The bill's id string, as the operation list reports it.", "pawn_id:integer")]
        [EndpointMetadata("Cancel an operation bill (?pawn_id=N&id=Bill_...)")]
        public async Task CancelOperation(HttpListenerContext context)
        {
            int pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            string id = RequestParser.GetStringParameter(context, "id");
            var result = _operationService.Cancel(pawnId, id);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/pawn/medicine-policy")]
        [ResponseBody(typeof(PawnOperationsDto))]
        [RequestBody(typeof(MedicinePolicyRequest))]
        [EndpointMetadata("Set a pawn's medicine policy", Notes = "Set a pawn's medicine policy: pawn_id and care (NoCare, NoMeds, HerbalOrWorse, NormalOrWorse, Best)")]
        public async Task SetMedicinePolicy(HttpListenerContext context)
        {
            MedicinePolicyRequest body = await ReadBody<MedicinePolicyRequest>(context);
            var result = _operationService.SetMedicine(body);
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
