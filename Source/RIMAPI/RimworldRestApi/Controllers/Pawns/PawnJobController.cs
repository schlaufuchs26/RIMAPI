using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class PawnJobController
    {
        private readonly IPawnJobService _pawnJobService;

        public PawnJobController(IPawnJobService pawnJobService)
        {
            _pawnJobService = pawnJobService;
        }

        [Post("/api/v1/pawn/job")]
        [ResponseBody]
        [RequestBody(typeof(PawnJobRequestDto))]
        [EndpointMetadata("Assign a job to a pawn by JobDef name, with optional target")]
        public async Task AssignJob(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<PawnJobRequestDto>();
            var result = _pawnJobService.AssignJob(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/pawn/medical/tend")]
        [ResponseBody]
        [RequestBody(typeof(MedicalTendRequestDto))]
        [EndpointMetadata("Assign a doctor to tend a patient")]
        public async Task AssignTendJob(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<MedicalTendRequestDto>();
            var result = _pawnJobService.AssignTendJob(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/pawn/medical/bed-rest")]
        [ResponseBody]
        [RequestBody(typeof(MedicalBedRestRequestDto))]
        [EndpointMetadata("Assign a pawn to bed rest, optionally specifying a bed")]
        public async Task AssignBedRest(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<MedicalBedRestRequestDto>();
            var result = _pawnJobService.AssignBedRest(body);
            await context.SendJsonResponse(result);
        }
    }
}
