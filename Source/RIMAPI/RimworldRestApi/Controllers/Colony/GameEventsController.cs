using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class GameEventsController
    {
        private readonly IIncidentService _incidentService;
        private readonly IQuestService _questService;

        public GameEventsController(IIncidentService incidentService, IQuestService questService)
        {
            _incidentService = incidentService;
            _questService = questService;
        }

        [Get("/api/v1/quests")]
        [EndpointMetadata("The quests the game has on offer or is running: id, name, kind, state, expiry, the game's own CanAcceptQuest report, the reward choices and the accepter. The id this read prints is what quest/accept takes.")]
        public async Task GetQuestsData(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _incidentService.GetQuestsData(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/quest/accept")]
        [EndpointMetadata("Accept one quest by id, the same path the Quests tab's accept button runs: the game's own CanAcceptQuest check, the chosen reward option, and Quest.Accept. Body: {id, pawn_id? (the colonist who takes it), choice? (the reward choice's index)}. A quest the game refuses answers accepted=false with its own reason.")]
        public async Task AcceptQuest(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<QuestAcceptRequestDto>();
            var result = _questService.AcceptQuest(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/incidents")]
        [EndpointMetadata("Get map incidents")]
        public async Task GetIncidentsData(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _incidentService.GetIncidentsData(mapId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/lords")]
        [EndpointMetadata("Get lords on map (AI raid managing objects)")]
        public async Task GetLordsData(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var result = _incidentService.GetLordsData(mapId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/incident/trigger")]
        [EndpointMetadata("Trigger game incident")]
        public async Task TriggerIncident(HttpListenerContext context)
        {
            var requestData = await context.Request.ReadBodyAsync<TriggerIncidentRequestDto>();
            var result = _incidentService.TriggerIncident(requestData);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/incidents/top")]
        public async Task GetIncidentsTopChance(HttpListenerContext context)
        {
            var result = _incidentService.GetTopIncidents();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/incident/chance")]
        public async Task GetIncidentChance(HttpListenerContext context)
        {
            var requestData = await context.Request.ReadBodyAsync<IncidentChanceRequestDto>();
            var result = _incidentService.GetIncidentChance(requestData);
            await context.SendJsonResponse(result);
        }
    }
}
