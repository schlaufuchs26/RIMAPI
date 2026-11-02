using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    /// <summary>
    /// The Animals tab's training card (fox ticket #4212, gap 5): the tricks an
    /// animal is taught, the colonist it follows, the area it may roam and the two
    /// follow checkboxes.
    /// </summary>
    public class AnimalTrainingController
    {
        private readonly IAnimalTrainingService _animalTrainingService;

        public AnimalTrainingController(IAnimalTrainingService animalTrainingService)
        {
            _animalTrainingService = animalTrainingService;
        }

        [Get("/api/v1/animal/training")]
        [QueryParameters("pawn_id:integer")]
        [EndpointMetadata("Read an animal's training card: each trick with learned and wanted, the master, the area and the follow flags (?pawn_id=N)")]
        public async Task GetTraining(HttpListenerContext context)
        {
            int pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _animalTrainingService.Get(pawnId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/animal/training")]
        [RequestBody(typeof(SetTrainingRequest))]
        [EndpointMetadata("Switch a trick on or off, the training card's checkbox: pawn_id, trainable, wanted")]
        public async Task SetTraining(HttpListenerContext context)
        {
            SetTrainingRequest body = await ReadBody<SetTrainingRequest>(context);
            var result = _animalTrainingService.SetTraining(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/animal/master")]
        [RequestBody(typeof(AnimalMasterRequest))]
        [EndpointMetadata("Set the colonist an animal follows, the training card's master button: pawn_id, master_id (0 clears)")]
        public async Task SetMaster(HttpListenerContext context)
        {
            AnimalMasterRequest body = await ReadBody<AnimalMasterRequest>(context);
            var result = _animalTrainingService.SetMaster(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/animal/area")]
        [RequestBody(typeof(AnimalAreaRequest))]
        [EndpointMetadata("Restrict an animal to an area, or free it: pawn_id, area (empty means anywhere)")]
        public async Task SetArea(HttpListenerContext context)
        {
            AnimalAreaRequest body = await ReadBody<AnimalAreaRequest>(context);
            var result = _animalTrainingService.SetArea(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/animal/follow")]
        [RequestBody(typeof(AnimalFollowRequest))]
        [EndpointMetadata("The training card's two follow checkboxes: pawn_id, follow_drafted, follow_fieldwork")]
        public async Task SetFollow(HttpListenerContext context)
        {
            AnimalFollowRequest body = await ReadBody<AnimalFollowRequest>(context);
            var result = _animalTrainingService.SetFollow(body);
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
