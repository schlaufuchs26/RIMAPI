using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class FactionController
    {
        private readonly IFactionService _factionService;

        public FactionController(IFactionService factionService)
        {
            _factionService = factionService;
        }

        [Get("/api/v1/factions")]
        [EndpointMetadata("List the factions")]
        [ResponseBody(typeof(List<FactionsDto>))]
        public async Task GetFactions(HttpListenerContext context)
        {
            var result = _factionService.GetFactions();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/faction/player")]
        [EndpointMetadata("Get the player faction")]
        [ResponseBody(typeof(FactionDto))]
        public async Task GetPlayerFaction(HttpListenerContext context)
        {
            var result = _factionService.GetPlayerFaction();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/faction")]
        [ResponseBody(typeof(FactionDto))]
        [QueryParameters("id:integer=The faction's numeric id, as the faction list reports it.")]
        [EndpointMetadata("Get one faction")]
        public async Task GetFaction(HttpListenerContext context)
        {
            int id = RequestParser.GetIntParameter(context, "id");
            var result = _factionService.GetFaction(id);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/faction/relation-with")]
        [ResponseBody(typeof(FactionRelationDto))]
        [QueryParameters("id:integer=The faction's numeric id, as the faction list reports it.", "other_id:integer=The other faction's numeric id, as the faction list reports it.")]
        [EndpointMetadata("Get a relation between two factions")]
        public async Task GetFactionRelationWith(HttpListenerContext context)
        {
            int id = RequestParser.GetIntParameter(context, "id");
            int otherId = RequestParser.GetIntParameter(context, "other_id");
            var result = _factionService.GetFactionRelationWith(id, otherId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/faction/relations")]
        [ResponseBody(typeof(FactionRelationsDto))]
        [QueryParameters("id:integer=The faction's numeric id, as the faction list reports it.")]
        [EndpointMetadata("List one faction's relations")]
        public async Task GetFactionRelations(HttpListenerContext context)
        {
            int id = RequestParser.GetIntParameter(context, "id");
            var result = _factionService.GetFactionRelations(id);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/faction/def")]
        [ResponseBody(typeof(FactionDefDto))]
        [QueryParameters("name:string=The faction def's internal name, e.g. \"Outlander\".")]
        [EndpointMetadata("Get a faction definition")]
        public async Task GetFactionDef(HttpListenerContext context)
        {
            var name = RequestParser.GetStringParameter(context, "name");
            var result = _factionService.GetFactionDef(name);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/faction/change/goodwill")]
        [RequestBody(typeof(FactionChangeRelationRequestDto))]
        [EndpointMetadata("Change faction goodwill")]
        [ResponseBody(typeof(FactionChangeRelationResponceDto))]
        public async Task ChangeFactionRelationWith(HttpListenerContext context)
        {
            FactionChangeRelationRequestDto body =
                await context.Request.ReadBodyAsync<FactionChangeRelationRequestDto>();
            var result = _factionService.ChangeFactionRelationWith(
                body.Id,
                body.OtherId,
                body.Value,
                body.SendMessage,
                body.CanSendHostilityLetter
            );
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/faction/goodwill")]
        [ResponseBody(typeof(FactionChangeRelationResponceDto))]
        [RequestBody(typeof(FactionChangeRelationRequestDto))]
        [EndpointMetadata("Set faction goodwill")]
        public async Task SetFactionGoodwill(HttpListenerContext context)
        {
            FactionChangeRelationRequestDto body =
                await context.Request.ReadBodyAsync<FactionChangeRelationRequestDto>();
            var result = _factionService.SetFactionGoodwill(
                body.Id,
                body.OtherId,
                body.Value,
                body.SendMessage,
                body.CanSendHostilityLetter
            );
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/faction/icon")]
        [ResponseBody(typeof(FactionIconImageDto))]
        [QueryParameters("id:integer=The faction's numeric id, as the faction list reports it.")]
        [EndpointMetadata("Get a faction icon")]
        public async Task GetFactionIcon(HttpListenerContext context)
        {
            int id = RequestParser.GetIntParameter(context, "id");
            var result = _factionService.GetFactionIcon(id);
            await context.SendJsonResponse(result);
        }
    }
}
