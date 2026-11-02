using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class PawnSocialController
    {
        private readonly IPawnSocialService _socialService;

        public PawnSocialController(IPawnSocialService socialService)
        {
            _socialService = socialService;
        }

        [Get("/api/v1/game/defs/interactions")]
        [ResponseBody(typeof(List<InteractionDefDto>))]
        [EndpointMetadata("List the social interaction defs", Notes = "Retrieves a list of all valid InteractionDef names in the game.")]
        public async Task GetInteractionDefs(HttpListenerContext context)
        {
            var result = _socialService.GetInteractionDefs();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/pawns/interactions")]
        [ResponseBody(typeof(PawnInteractionStatusDto))]
        [QueryParameters("pawn_id:integer")]
        [EndpointMetadata("Get a pawn's interaction readiness")]
        public async Task GetPawnInteractionStatus(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _socialService.GetPawnInteractionStatus(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/pawns/interactions/log")]
        [ResponseBody(typeof(PawnInteractionLogDto))]
        [QueryParameters("limit:integer", "pawn_id:integer")]
        [EndpointMetadata("Get a pawn's interaction history", Notes = "Retrieves the recent interaction history for a specific pawn.")]
        public async Task GetPawnInteractionLog(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var limit = RequestParser.HasParameter(context, "limit") ? RequestParser.GetIntParameter(context, "limit") : 50;
            var result = _socialService.GetPawnInteractionLog(pawnId, limit);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/pawns/relations")]
        [ResponseBody(typeof(PawnRelationsDto))]
        [QueryParameters("pawn_id:integer")]
        [EndpointMetadata("Get a pawn's relationships", Notes = "Retrieves all established familial and romantic relationships for this pawn.")]
        public async Task GetPawnRelations(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _socialService.GetPawnRelations(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/pawns/opinions")]
        [ResponseBody(typeof(List<PawnOpinionDto>))]
        [QueryParameters("pawn_id:integer")]
        [EndpointMetadata("List a pawn's opinions", Notes = "Retrieves a list of this pawn's numerical opinion toward every other colonist on the map.")]
        public async Task GetPawnOpinions(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _socialService.GetPawnOpinions(pawnId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/pawns/interactions/force")]
        [RequestBody(typeof(ForceInteractionRequestDto))]
        [EndpointMetadata("Force a social interaction", Notes = "Forces a specific social interaction to occur immediately between two pawns.")]
        [ResponseBody]
        public async Task ForceInteraction(HttpListenerContext context)
        {
            var request = await context.Request.ReadBodyAsync<ForceInteractionRequestDto>();
            var result = _socialService.ForceInteraction(request);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/dev/pawns/relations/add")]
        [RequestBody(typeof(AddRelationRequestDto))]
        [EndpointMetadata("Create a social bond")]
        [ResponseBody]
        public async Task AddRelation(HttpListenerContext context)
        {
            var request = await context.Request.ReadBodyAsync<AddRelationRequestDto>();
            var result = _socialService.AddRelation(request);
            await context.SendJsonResponse(result);
        }

        [Delete("/api/v1/dev/pawns/relations/remove")]
        [QueryParameters("relation_def_name:string")]
        [EndpointMetadata("Sever a social bond")]
        [ResponseBody]
        public async Task RemoveRelation(HttpListenerContext context)
        {
            var pawn1Id = RequestParser.GetIntParameter(context, "pawn1_id");
            var pawn2Id = RequestParser.GetIntParameter(context, "pawn2_id");
            var relationDefName = RequestParser.GetStringParameter(context, "relation_def_name");

            var request = new RemoveRelationRequestDto
            {
                Pawn1Id = pawn1Id,
                Pawn2Id = pawn2Id,
                RelationDefName = relationDefName
            };

            var result = _socialService.RemoveRelation(request);
            await context.SendJsonResponse(result);
        }
    }
}
