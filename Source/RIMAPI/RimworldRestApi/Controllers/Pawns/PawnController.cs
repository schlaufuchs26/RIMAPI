using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RIMAPI.Core;
using RIMAPI.Http;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class PawnController : BaseController
    {
        private readonly IColonistService _colonistService;
        private readonly ICachingService _cachingService;

        public PawnController(IColonistService colonistService, ICachingService cachingService)
        {
            _colonistService = colonistService;
            _cachingService = cachingService;
        }

        [Get("/api/v1/colonists")]
        [ResponseBody(typeof(List<PawnDto>))]
        public async Task GetColonists(HttpListenerContext context)
        {
            await _cachingService.CacheAwareResponseAsync(
                context,
                "/api/v1/colonists",
                dataFactory: () => Task.FromResult(_colonistService.GetColonists()),
                priority: CachePriority.Normal,
                expirationType: CacheExpirationType.GameTick,
                gameTicksExpiration: 1800
            );
        }

        [Get("/api/v1/colonists/positions")]
        [ResponseBody(typeof(List<PawnPositionDto>))]
        public async Task GetColonistPositions(HttpListenerContext context)
        {
            await _cachingService.CacheAwareResponseAsync(
                context,
                "/api/v1/colonists/positions",
                dataFactory: () => Task.FromResult(_colonistService.GetColonistPositions()),
                priority: CachePriority.High,
                expirationType: CacheExpirationType.GameTick,
                gameTicksExpiration: 1800
           );
        }

        [Get("/api/v1/colonist")]
        [ResponseBody(typeof(PawnDto))]
        [QueryParameters("id:integer")]
        public async Task GetColonist(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _colonistService.GetColonist(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/colonists/detailed")]
        [ResponseBody(typeof(List<ApiV1PawnDetailedDto>))]
        public async Task GetColonistsDetailedV1(HttpListenerContext context)
        {
            await _cachingService.CacheAwareResponseAsync(
                context,
                "/api/v1/colonists/detailed",
                dataFactory: () => Task.FromResult(_colonistService.GetColonistsDetailedV1()),
                priority: CachePriority.Normal,
                expirationType: CacheExpirationType.GameTick,
                gameTicksExpiration: 1800
            );
        }

        [Get("/api/v1/colonist/detailed")]
        [ResponseBody(typeof(ApiV1PawnDetailedDto))]
        [QueryParameters("id:integer")]
        public async Task GetColonistDetailedV1(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _colonistService.GetColonistDetailedV1(pawnId);
            await context.SendJsonResponse(result);
        }


        [Get("/api/v2/colonists/detailed")]
        [ResponseBody(typeof(List<PawnDetailedRequestDto>))]
        public async Task GetColonistsDetailed(HttpListenerContext context)
        {
            await _cachingService.CacheAwareResponseAsync(
                context,
                "/api/v2/colonists/detailed",
                dataFactory: () => Task.FromResult(_colonistService.GetColonistsDetailed()),
                priority: CachePriority.Normal,
                expirationType: CacheExpirationType.GameTick,
                gameTicksExpiration: 1800
            );
        }

        [Get("/api/v2/colonist/detailed")]
        [ResponseBody(typeof(PawnDetailedRequestDto))]
        [QueryParameters("id:integer")]
        public async Task GetColonistDetailed(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _colonistService.GetColonistDetailed(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/colonist/opinion-about")]
        [ResponseBody(typeof(OpinionAboutPawnDto))]
        [QueryParameters("id:integer", "other_id:integer")]
        public async Task GetPawnOpinionAboutPawn(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var otherId = RequestParser.GetIntParameter(context, "other_id");
            var result = _colonistService.GetOpinionAboutPawn(pawnId, otherId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/colonist/inventory")]
        [ResponseBody(typeof(PawnInventoryDto))]
        [QueryParameters("id:integer")]
        public async Task GetColonistInventory(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _colonistService.GetColonistInventory(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/colonist/body/image")]
        [ResponseBody(typeof(BodyPartsDto))]
        [QueryParameters("id:integer")]
        public async Task GetPawnBodyImage(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _colonistService.GetColonistBodyParts(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/pawn/portrait/image")]
        [ResponseBody(typeof(ImageDto))]
        [QueryParameters("direction:string", "height:integer", "pawn_id:integer", "width:integer")]
        public async Task GetPawnPortraitImage(HttpListenerContext context)
        {
            int pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            int width = RequestParser.GetIntParameter(context, "width");
            int height = RequestParser.GetIntParameter(context, "height");
            string direction = RequestParser.GetStringParameter(context, "direction");

            var result = _colonistService.GetPawnPortraitImage(pawnId, width, height, direction);

            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/trait-def")]
        [ResponseBody(typeof(TraitDefDto))]
        [QueryParameters("name:string")]
        public async Task GetTraitDef(HttpListenerContext context)
        {
            string name = RequestParser.GetStringParameter(context, "name");
            var result = _colonistService.GetTraitDefDto(name);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/time-assignments")]
        [ResponseBody(typeof(List<TimeAssignmentDto>))]
        public async Task GetTimeAssignmentsList(HttpListenerContext context)
        {
            var result = _colonistService.GetTimeAssignmentsList();
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/colonist/time-assignment")]
        [ResponseBody]
        [RequestBody(typeof(PawnTimeAssignmentRequestDto))]
        [QueryParameters("assignment:string?", "hour:integer?", "pawn_id:integer?")]
        [EndpointMetadata("Schedule pawn assignment during provided hour")]
        public async Task SetTimeAssignment(HttpListenerContext context)
        {
            PawnTimeAssignmentRequestDto body;

            if (context.Request.HasEntityBody)
            {
                body = await context.Request.ReadBodyAsync<PawnTimeAssignmentRequestDto>();
            }
            else
            {
                body = new PawnTimeAssignmentRequestDto
                {
                    PawnId = RequestParser.GetIntParameter(context, "pawn_id"),
                    Hour = RequestParser.GetIntParameter(context, "hour"),
                    Assignment = RequestParser.GetStringParameter(context, "assignment")
                };
            }

            var result = _colonistService.SetTimeAssignment(body);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/outfits")]
        [ResponseBody(typeof(List<OutfitDto>))]
        public async Task GetOutfits(HttpListenerContext context)
        {
            var result = _colonistService.GetOutfits();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/work-list")]
        [ResponseBody(typeof(WorkListDto))]
        public async Task GetWorkList(HttpListenerContext context)
        {
            var result = _colonistService.GetWorkList();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/colonist/work-priorities")]
        [ResponseBody(typeof(ColonistWorkPrioritiesDto))]
        [QueryParameters("pawn_id:integer")]
        [EndpointMetadata("Read one pawn's work priorities")]
        public async Task GetColonistWorkPriorities(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _colonistService.GetColonistWorkPriorities(pawnId);
            await context.SendJsonResponse(result);
        }

        // Every colonist's work levels in one read (fox ticket #4158): the build
        // path names who would build a plan, and one read per pawn made a
        // colony of eight pay eight loopback calls per placement.
        [Get("/api/v1/colonists/work-priorities")]
        [ResponseBody(typeof(ColonistsWorkPrioritiesDto))]
        [EndpointMetadata("Read every colonist's work priorities in one call")]
        public async Task GetColonistsWorkPriorities(HttpListenerContext context)
        {
            var result = _colonistService.GetColonistsWorkPriorities();
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/colonist/work-priority")]
        [ResponseBody]
        [RequestBody(typeof(WorkPriorityRequestDto))]
        [EndpointMetadata("Set pawn work priorities")]
        public async Task SetColonistWorkPriority(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<WorkPriorityRequestDto>();
            var result = _colonistService.SetColonistWorkPriority(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/colonists/work-priority")]
        [ResponseBody]
        [RequestBody(typeof(ColonistsWorkPrioritiesRequestDto))]
        [EndpointMetadata("Set multiple work priorities to several pawns")]
        public async Task SetColonistsWorkPriority(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<ColonistsWorkPrioritiesRequestDto>();
            var result = _colonistService.SetColonistsWorkPriority(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/jobs/make/equip")]
        [ResponseBody]
        [QueryParameters("item_id:integer", "map_id:integer", "pawn_id:integer")]
        [EndpointMetadata("Set multiple work priorities to several pawns")]
        public async Task MakeJobEquip(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var itemId = RequestParser.GetIntParameter(context, "item_id");
            var result = _colonistService.MakeJobEquip(mapId, pawnId, itemId);
            await context.SendJsonResponse(result);
        }

        // Tending a patient: pawn_id is the doctor, patient_id the colonist to
        // treat, medicine_id optional (0 or absent means no medicine). The same
        // id in both is self-tend, which the pawn's Health tab option allows
        // (ticket #3247). The medicine parameter is optional, and
        // GetIntParameter throws on a missing one, so it is only read when the
        // caller sent it (ticket #3117).
        [Post("/api/v1/jobs/make/tend")]
        [ResponseBody]
        [QueryParameters("map_id:integer", "medicine_id:integer", "patient_id:integer", "pawn_id:integer")]
        [EndpointMetadata("Queue the game's own tend job: a doctor treats a patient, or a pawn itself when its Self-tend option is on")]
        public async Task MakeJobTend(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var patientId = RequestParser.GetIntParameter(context, "patient_id");
            var medicineId = RequestParser.HasParameter(context, "medicine_id")
                ? RequestParser.GetIntParameter(context, "medicine_id")
                : 0;
            var result = _colonistService.MakeJobTend(mapId, pawnId, patientId, medicineId);
            await context.SendJsonResponse(result);
        }

        // The Health tab's Self-tend option as a write (ticket #3279): the
        // option gates the self-tend job above, and a colony of one has no
        // other way to move it. enabled defaults to true, so a bare call turns
        // it on.
        [Post("/api/v1/colonist/self-tend")]
        [ResponseBody]
        [QueryParameters("enabled:boolean?", "pawn_id:integer")]
        [EndpointMetadata("Switch one pawn's Self-tend option on or off")]
        public async Task SetSelfTend(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var enabled = !RequestParser.HasParameter(context, "enabled")
                || RequestParser.GetBooleanParameter(context, "enabled");
            var result = _colonistService.SetSelfTend(pawnId, enabled);
            await context.SendJsonResponse(result);
        }

        // Hauling one named thing to one named place: item_id is the thing to
        // carry, dest_id the storage building it goes into, pawn_id the hauler
        // (0 or absent lets the mod pick a colonist that can haul). A corpse
        // into a grave is the case this exists for (ticket #3136).
        [Post("/api/v1/jobs/make/haul")]
        [ResponseBody]
        [QueryParameters("dest_id:integer", "item_id:integer", "map_id:integer", "pawn_id:integer")]
        [EndpointMetadata("Queue the game's own haul job for one thing and one destination")]
        public async Task MakeJobHaul(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.HasParameter(context, "pawn_id")
                ? RequestParser.GetIntParameter(context, "pawn_id")
                : 0;
            var itemId = RequestParser.GetIntParameter(context, "item_id");
            var destId = RequestParser.HasParameter(context, "dest_id")
                ? RequestParser.GetIntParameter(context, "dest_id")
                : 0;
            var result = _colonistService.MakeJobHaul(mapId, pawnId, itemId, destId);
            await context.SendJsonResponse(result);
        }

        // One job on one named colonist now: the player's right-click
        // "Prioritize X" entry (fox ticket #3626). thing_id or x/z names the
        // target, job the menu's word for it (tame, harvest, mine), work
        // narrows the scan to one work type. Every field but pawn_id and the
        // target is optional.
        [Post("/api/v1/jobs/make/prioritize")]
        [ResponseBody]
        [QueryParameters("job:string?", "map_id:integer", "pawn_id:integer", "thing_id:integer", "work:string?", "x:integer", "z:integer")]
        [EndpointMetadata("Order one job on one named colonist now, the way the right-click menu does")]
        public async Task MakeJobPrioritize(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var job = RequestParser.GetStringParameter(context, "job", false);
            var work = RequestParser.GetStringParameter(context, "work", false);
            var thingId = RequestParser.HasParameter(context, "thing_id")
                ? RequestParser.GetIntParameter(context, "thing_id")
                : 0;
            var x = RequestParser.HasParameter(context, "x")
                ? RequestParser.GetIntParameter(context, "x")
                : 0;
            var z = RequestParser.HasParameter(context, "z")
                ? RequestParser.GetIntParameter(context, "z")
                : 0;
            if (thingId <= 0 && !RequestParser.HasParameter(context, "x"))
            {
                await context.SendJsonResponse(
                    ApiResult.Fail("prioritize needs a target: thing_id, or x and z for a cell"));
                return;
            }
            var result = _colonistService.MakeJobPrioritize(mapId, pawnId, job, thingId, x, z, work);
            await context.SendJsonResponse(result);
        }

        // Taming a wild person or an animal: the mod adds the game's Tame
        // designation and queues the tame job on the best handler, because the
        // job's driver only runs while the designation stands (ticket #3137).
        [Post("/api/v1/jobs/make/tame")]
        [ResponseBody]
        [QueryParameters("map_id:integer", "pawn_id:integer")]
        [EndpointMetadata("Queue the game's own tame job for a wild person or an animal")]
        public async Task MakeJobTame(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _colonistService.MakeJobTame(mapId, pawnId);
            await context.SendJsonResponse(result);
        }

        // Slaughtering a colony animal: the Animals tab's own Slaughter mark,
        // which the game's handler job runs off (fox ticket #3507).
        [Post("/api/v1/jobs/make/slaughter")]
        [ResponseBody]
        [QueryParameters("map_id:integer", "pawn_id:integer")]
        [EndpointMetadata("Queue the game's own slaughter job for a colony animal")]
        public async Task MakeJobSlaughter(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _colonistService.MakeJobSlaughter(mapId, pawnId);
            await context.SendJsonResponse(result);
        }

        // Arrest or capture: the game's own job, with the prisoner bed it
        // carries the pawn to. arrestor_id is optional; without it the mod
        // takes the nearest colonist who can do it.
        [Post("/api/v1/jobs/make/arrest")]
        [ResponseBody]
        [QueryParameters("arrestor_id:integer", "map_id:integer", "pawn_id:integer")]
        [EndpointMetadata("Queue the game's own arrest or capture job into a prisoner bed")]
        public async Task MakeJobArrest(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var arrestorId = RequestParser.HasParameter(context, "arrestor_id")
                ? RequestParser.GetIntParameter(context, "arrestor_id")
                : 0;
            var result = _colonistService.MakeJobArrest(mapId, pawnId, arrestorId);
            await context.SendJsonResponse(result);
        }

        // Rescue: the game's own job carries a downed non-enemy to a free bed,
        // which is how a crashed pod survivor joins the colony (ticket #3183).
        // rescuer_id is optional; without it the mod takes the nearest colonist
        // who can reach him. bed_id names the bed instead of the game's own
        // lookup, which is how a downed colony animal reaches an animal bed or
        // an animal sleeping spot (fox ticket #3519).
        [Post("/api/v1/jobs/make/rescue")]
        [ResponseBody]
        [QueryParameters("bed_id:integer", "map_id:integer", "pawn_id:integer", "rescuer_id:integer")]
        [EndpointMetadata("Queue the game's own rescue job: carry a downed pawn or colony animal to a bed, or to the bed named in bed_id")]
        public async Task MakeJobRescue(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var rescuerId = RequestParser.HasParameter(context, "rescuer_id")
                ? RequestParser.GetIntParameter(context, "rescuer_id")
                : 0;
            var bedId = RequestParser.HasParameter(context, "bed_id")
                ? RequestParser.GetIntParameter(context, "bed_id")
                : 0;
            var result = _colonistService.MakeJobRescue(mapId, pawnId, rescuerId, bedId);
            await context.SendJsonResponse(result);
        }

        // Feed: the game's own FeedPatient job, which needs the patient in a
        // bed (ticket #3445). feeder_id and meal_id are optional; without them
        // the mod takes the nearest colonist who can feed and the best meal the
        // patient may eat. The giver is the game's own for the patient's kind:
        // a prisoner's warden, the doctor's humanlike feeder, or its animal
        // feeder for an animal in an animal bed (fox ticket #3519).
        [Post("/api/v1/jobs/make/feed")]
        [ResponseBody]
        [QueryParameters("feeder_id:integer", "map_id:integer", "meal_id:integer", "pawn_id:integer")]
        [EndpointMetadata("Queue the game's own FeedPatient job for a downed patient in a bed: a colonist, a prisoner or a colony animal")]
        public async Task MakeJobFeed(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var feederId = RequestParser.HasParameter(context, "feeder_id")
                ? RequestParser.GetIntParameter(context, "feeder_id")
                : 0;
            var mealId = RequestParser.HasParameter(context, "meal_id")
                ? RequestParser.GetIntParameter(context, "meal_id")
                : 0;
            var result = _colonistService.MakeJobFeed(mapId, pawnId, feederId, mealId);
            await context.SendJsonResponse(result);
        }

        // Chat: the game's own social interaction with a stranger who wants to
        // be talked into joining (ticket #3275). chatter_id is optional; without
        // it the mod takes the nearest colonist who can reach him and talk.
        [Post("/api/v1/jobs/make/chat")]
        [ResponseBody]
        [QueryParameters("chatter_id:integer", "map_id:integer", "pawn_id:integer")]
        [EndpointMetadata("Queue the game's own chat with a visitor who wants to join")]
        public async Task MakeJobChat(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var chatterId = RequestParser.HasParameter(context, "chatter_id")
                ? RequestParser.GetIntParameter(context, "chatter_id")
                : 0;
            var result = _colonistService.MakeJobChat(mapId, pawnId, chatterId);
            await context.SendJsonResponse(result);
        }

        // The prisoner tab's interaction setting: recruit, reduce resistance,
        // release, maintain. mode takes the defName (AttemptRecruit) or the
        // label the game shows (recruit).
        [Post("/api/v1/pawn/prisoner/interaction")]
        [ResponseBody]
        [QueryParameters("mode:string", "pawn_id:integer")]
        [EndpointMetadata("Set a prisoner's interaction mode (the recruitment policy)")]
        public async Task SetPrisonerInteraction(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var mode = RequestParser.GetStringParameter(context, "mode");
            var result = _colonistService.SetPrisonerInteraction(pawnId, mode);
            await context.SendJsonResponse(result);
        }

        // The pawn's allowed area: the player's dropdown on the pawn's row,
        // which no route could reach before (fox ticket #3645). area takes a
        // label the map has (Home, a named area) or Unrestricted to lift the
        // restriction; the answer is the pawn's area afterwards.
        [Post("/api/v1/pawn/allowed_area")]
        [ResponseBody]
        [QueryParameters("area:string?", "map_id:integer", "pawn_id:integer")]
        [EndpointMetadata("Restrict a pawn to an allowed area, or lift the restriction")]
        public async Task SetAllowedArea(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var area = RequestParser.GetStringParameter(context, "area", false);
            var result = _colonistService.SetAllowedArea(mapId, pawnId, area);
            await context.SendJsonResponse(result);
        }
    }
}
