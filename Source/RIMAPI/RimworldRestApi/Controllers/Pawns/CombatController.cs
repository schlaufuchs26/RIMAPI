using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class CombatController
    {
        private readonly ICombatService _combatService;
        private readonly ICoverService _coverService;
        private readonly IDeathService _deathService;

        public CombatController(ICombatService combatService, ICoverService coverService, IDeathService deathService)
        {
            _combatService = combatService;
            _coverService = coverService;
            _deathService = deathService;
        }

        [Get("/api/v1/map/combat")]
        [ResponseBody(typeof(CombatStateDto))]
        [QueryParameters("map_id:integer")]
        [EndpointMetadata("List the hostiles and colonists", Notes = "Hostiles and colonists with position, weapon, health, drafted state and current job")]
        public async Task GetCombat(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            var result = _combatService.GetCombat(mapId);
            await context.SendJsonResponse(result);
        }

        // The kills the game recorded (fox ticket #3643): the tale a kill
        // writes names killer, victim and the weapon's def, which is what a
        // report needs to tell a dead raider from one who fled the map.
        [Get("/api/v1/map/deaths")]
        [ResponseBody(typeof(List<DeathDto>))]
        [QueryParameters("limit:integer", "since:integer")]
        [EndpointMetadata("List the recorded kills", Notes = "Kills the game recorded: killer, victim, weapon and the tale's kind")]
        public async Task GetDeaths(HttpListenerContext context)
        {
            int since = RequestParser.HasParameter(context, "since")
                ? RequestParser.GetIntParameter(context, "since")
                : 0;
            int limit = RequestParser.HasParameter(context, "limit")
                ? RequestParser.GetIntParameter(context, "limit")
                : 0;
            var result = _deathService.ListDeaths(since, limit);
            await context.SendJsonResponse(result);
        }

        // Cover around a pawn (or a bare cell) against one target: the game's
        // own block chance per cell, the neighbouring things that give it, and
        // whether the cell has a line to the target (ticket #3174). Without a
        // target there is no direction, so the call needs one.
        [Get("/api/v1/map/cover")]
        [ResponseBody(typeof(CoverStateDto))]
        [QueryParameters("map_id:integer", "pawn_id:integer", "radius:integer", "target_id:integer=The hostile's numeric id, as the combat read reports it.", "target_x:integer=The target cell's x coordinate, in map cells.", "target_z:integer=The target cell's z coordinate, in map cells.", "x:integer", "z:integer")]
        [EndpointMetadata("Get the cover cells around a point", Notes = "Cover cells around a pawn or cell, with the game's cover chance and line of sight to a target")]
        public async Task GetCover(HttpListenerContext context)
        {
            int mapId = RequestParser.GetMapId(context);
            int pawnId = RequestParser.HasParameter(context, "pawn_id")
                ? RequestParser.GetIntParameter(context, "pawn_id")
                : 0;
            int x = RequestParser.HasParameter(context, "x") ? RequestParser.GetIntParameter(context, "x") : -1;
            int z = RequestParser.HasParameter(context, "z") ? RequestParser.GetIntParameter(context, "z") : -1;
            int radius = RequestParser.HasParameter(context, "radius")
                ? RequestParser.GetIntParameter(context, "radius")
                : 0;
            int targetId = RequestParser.HasParameter(context, "target_id")
                ? RequestParser.GetIntParameter(context, "target_id")
                : 0;
            int targetX = RequestParser.HasParameter(context, "target_x")
                ? RequestParser.GetIntParameter(context, "target_x")
                : -1;
            int targetZ = RequestParser.HasParameter(context, "target_z")
                ? RequestParser.GetIntParameter(context, "target_z")
                : -1;
            var result = _coverService.GetCover(
                mapId, pawnId, x, z, radius, targetId, targetX, targetZ);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/pawn/draft")]
        [ResponseBody(typeof(List<DraftResultDto>))]
        [RequestBody(typeof(DraftRequestDto))]
        [EndpointMetadata("Draft or undraft a list of colonists")]
        public async Task SetDrafted(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<DraftRequestDto>();
            var result = _combatService.SetDrafted(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/pawn/order")]
        [ResponseBody(typeof(OrderResultDto))]
        [RequestBody(typeof(OrderRequestDto))]
        [EndpointMetadata("Order a drafted colonist", Notes = "Order one drafted colonist to move, attack, hold or switch fire-at-will")]
        public async Task Order(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<OrderRequestDto>();
            var result = _combatService.Order(body);
            await context.SendJsonResponse(result);
        }
    }
}
