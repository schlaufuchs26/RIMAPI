using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Models;
using RIMAPI.Http;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class ResearchController
    {
        private readonly IResearchService _researchService;

        public ResearchController(IResearchService researchService)
        {
            _researchService = researchService;
        }

        [Get("/api/v1/research/progress")]
        [ResponseBody(typeof(ResearchProjectDto))]
        [EndpointMetadata("Get the research progress")]
        public async Task GetResearchProgress(HttpListenerContext context)
        {
            var result = _researchService.GetResearchProgress();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/research/finished")]
        [ResponseBody(typeof(ResearchFinishedDto))]
        [EndpointMetadata("List the finished research")]
        public async Task GetResearchFinished(HttpListenerContext context)
        {
            var result = _researchService.GetResearchFinished();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/research/tree")]
        [ResponseBody(typeof(ResearchTreeDto))]
        [EndpointMetadata("Get the research tree")]
        public async Task GetResearchTree(HttpListenerContext context)
        {
            var result = _researchService.GetResearchTree();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/research/project")]
        [ResponseBody(typeof(ResearchProjectDto))]
        [QueryParameters("name:string=The research project's def name, e.g. \"Electricity\".")]
        [EndpointMetadata("Get one research project")]
        public async Task GetResearchProjectByName(HttpListenerContext context)
        {
            var name = RequestParser.GetStringParameter(context, "name");
            var result = _researchService.GetResearchProjectByName(name);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/research/summary")]
        [ResponseBody(typeof(ResearchSummaryDto))]
        [EndpointMetadata("Get a research summary")]
        public async Task GetResearchSummary(HttpListenerContext context)
        {
            var result = _researchService.GetResearchSummary();
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/research/target")]
        [ResponseBody(typeof(ResearchProjectDto))]
        [QueryParameters("force:boolean?=Set the project even when another one is already running.", "name:string=The research project's def name, e.g. \"Electricity\".")]
        [EndpointMetadata("Set the current research project by defName")]
        public async Task SetResearchTarget(HttpListenerContext context)
        {
            var name = RequestParser.GetStringParameter(context, "name");
            var force = RequestParser.GetBooleanParameter(context, "force");
            var result = _researchService.SetResearchTarget(name, force);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/research/stop")]
        [ResponseBody]
        [EndpointMetadata("Stop the currently active research project")]
        public async Task StopCurrentProject(HttpListenerContext context)
        {
            var result = _researchService.StopCurrentProject();
            await context.SendJsonResponse(result);
        }
    }
}
