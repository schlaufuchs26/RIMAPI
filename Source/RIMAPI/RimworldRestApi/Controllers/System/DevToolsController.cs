using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class DevToolsController
    {
        private readonly IDevToolsService _devToolsService;
        private readonly IDevActionService _devActionService;

        public DevToolsController(
            IDevToolsService gameStateService,
            IDevActionService devActionService
        )
        {
            _devToolsService = gameStateService;
            _devActionService = devActionService;
        }

        // One dev op of the fox's `dev` action (fox ticket #3757): build a
        // state a check needs in the throwaway game. The body names the op and
        // its fields; the answer is the state the game holds afterwards. The
        // service refuses the whole route unless the game runs in dev mode, so
        // a colony the fox plays honestly cannot be tampered with.
        [Post("/api/v1/dev/action")]
        [EndpointMetadata(
            "Build a test situation in a dev-mode game: damage, heal, hediff, spawn, mental state, skill, trait, incident, quest, wealth, ticks"
        )]
        public async Task PostDevAction(HttpListenerContext context)
        {
            DevActionRequest body;
            if (context.Request.HasEntityBody)
            {
                body = await context.Request.ReadBodyAsync<DevActionRequest>();
            }
            else
            {
                body = new DevActionRequest
                {
                    Op = RequestParser.GetStringParameter(context, "op"),
                    Name = RequestParser.GetStringParameter(context, "name"),
                };
            }

            var result = _devActionService.Run(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/console")]
        [RequestBody(typeof(DebugConsoleRequest))]
        [QueryParameters("action:string", "message:string")]
        [EndpointMetadata("Send message to the debug console")]
        public async Task PostConsoleAction(HttpListenerContext context)
        {
            DebugConsoleRequest body;
            if (context.Request.HasEntityBody)
            {
                body = await context.Request.ReadBodyAsync<DebugConsoleRequest>();
            }
            else
            {
                body = new DebugConsoleRequest
                {
                    Action = RequestParser.GetStringParameter(context, "action"),
                    Message = RequestParser.GetStringParameter(context, "message"),
                };
            }

            var result = _devToolsService.ConsoleAction(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/dev/materials-atlas")]
        [ResponseExample(typeof(MaterialsAtlasList))]
        public async Task GetMaterialsAtlasList(HttpListenerContext context)
        {
            var result = _devToolsService.GetMaterialsAtlasList();
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/materials-atlas/clear")]
        public async Task PostMaterialsAtlasPoolClear(HttpListenerContext context)
        {
            var result = _devToolsService.MaterialsAtlasPoolClear();
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/stuff/color")]
        [RequestBody(typeof(StuffColorRequest))]
        [EndpointMetadata("Change stuff color")]
        public async Task PostStuffColor(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<StuffColorRequest>();
            var result = _devToolsService.SetStuffColor(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/dev/endpoints")]
        [EndpointMetadata("Get list of all registered endpoints")]
        public async Task GetEndpoints(HttpListenerContext context)
        {
            var result = _devToolsService.GetEndpoints();
            await context.SendJsonResponse(result);
        }
    }
}
