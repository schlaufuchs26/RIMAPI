using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using RIMAPI.Core;
using RIMAPI.Services;
using RIMAPI.Models;
using RIMAPI.Http;

namespace RIMAPI.Controllers
{
    public class LearningController
    {
        private readonly ILearningService _learningService;

        public LearningController(ILearningService learningService)
        {
            _learningService = learningService;
        }

        [Get("/api/v1/client/learning/defs")]
        [EndpointMetadata("List the learning concept defs")]
        [ResponseBody(typeof(List<string>))]
        public async Task GetConceptsDefs(HttpListenerContext context)
        {
            var result = _learningService.GetLearningConceptsDefs();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/client/learning/all")]
        [EndpointMetadata("List all learning concepts")]
        [ResponseBody(typeof(List<LearningConceptDto>))]
        public async Task GetAllConcepts(HttpListenerContext context)
        {
            var result = _learningService.GetAllConcepts();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/client/learning/active")]
        [EndpointMetadata("List the active learning concepts")]
        [ResponseBody(typeof(List<LearningConceptDto>))]
        public async Task GetActiveConcepts(HttpListenerContext context)
        {
            var result = _learningService.GetActiveConcepts();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/client/learning/concept")]
        [QueryParameters("def_name:string")]
        [EndpointMetadata("Get one learning concept")]
        [ResponseBody(typeof(LearningConceptDto))]
        public async Task GetConcept(HttpListenerContext context)
        {
            var defName = RequestParser.GetStringParameter(context, "def_name");
            var result = _learningService.GetConceptByDef(defName);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/client/learning/mark-learned")]
        [RequestBody(typeof(LearningConceptMarkDto))]
        [EndpointMetadata("Mark a concept learned")]
        [ResponseBody(typeof(bool))]
        public async Task MarkLearned(HttpListenerContext context)
        {
            var request = await context.Request.ReadBodyAsync<LearningConceptMarkDto>();
            var result = _learningService.MarkConceptLearned(request);
            await context.SendJsonResponse(result);
        }
    }
}