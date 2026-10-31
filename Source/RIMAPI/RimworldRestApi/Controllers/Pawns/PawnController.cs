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
        public async Task GetColonist(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _colonistService.GetColonist(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/colonists/detailed")]
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
        public async Task GetColonistDetailedV1(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _colonistService.GetColonistDetailedV1(pawnId);
            await context.SendJsonResponse(result);
        }


        [Get("/api/v2/colonists/detailed")]
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
        public async Task GetColonistDetailed(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _colonistService.GetColonistDetailed(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/colonist/opinion-about")]
        public async Task GetPawnOpinionAboutPawn(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var otherId = RequestParser.GetIntParameter(context, "other_id");
            var result = _colonistService.GetOpinionAboutPawn(pawnId, otherId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/colonist/inventory")]
        public async Task GetColonistInventory(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _colonistService.GetColonistInventory(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/colonist/body/image")]
        public async Task GetPawnBodyImage(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "id");
            var result = _colonistService.GetColonistBodyParts(pawnId);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/pawn/portrait/image")]
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
        public async Task GetTraitDef(HttpListenerContext context)
        {
            string name = RequestParser.GetStringParameter(context, "name");
            var result = _colonistService.GetTraitDefDto(name);
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/time-assignments")]
        public async Task GetTimeAssignmentsList(HttpListenerContext context)
        {
            var result = _colonistService.GetTimeAssignmentsList();
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/colonist/time-assignment")]
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
        public async Task GetOutfits(HttpListenerContext context)
        {
            var result = _colonistService.GetOutfits();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/work-list")]
        public async Task GetWorkList(HttpListenerContext context)
        {
            var result = _colonistService.GetWorkList();
            await context.SendJsonResponse(result);
        }

        [Get("/api/v1/colonist/work-priorities")]
        [EndpointMetadata("Read one pawn's work priorities")]
        public async Task GetColonistWorkPriorities(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _colonistService.GetColonistWorkPriorities(pawnId);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/colonist/work-priority")]
        [EndpointMetadata("Set pawn work priorities")]
        public async Task SetColonistWorkPriority(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<WorkPriorityRequestDto>();
            var result = _colonistService.SetColonistWorkPriority(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/colonists/work-priority")]
        [EndpointMetadata("Set multiple work priorities to several pawns")]
        public async Task SetColonistsWorkPriority(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<ColonistsWorkPrioritiesRequestDto>();
            var result = _colonistService.SetColonistsWorkPriority(body);
            await context.SendJsonResponse(result);
        }

        [Post("/api/v1/jobs/make/equip")]
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

        // Taming a wild person or an animal: the mod adds the game's Tame
        // designation and queues the tame job on the best handler, because the
        // job's driver only runs while the designation stands (ticket #3137).
        [Post("/api/v1/jobs/make/tame")]
        [EndpointMetadata("Queue the game's own tame job for a wild person or an animal")]
        public async Task MakeJobTame(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var result = _colonistService.MakeJobTame(mapId, pawnId);
            await context.SendJsonResponse(result);
        }

        // Arrest or capture: the game's own job, with the prisoner bed it
        // carries the pawn to. arrestor_id is optional; without it the mod
        // takes the nearest colonist who can do it.
        [Post("/api/v1/jobs/make/arrest")]
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
        // who can reach him.
        [Post("/api/v1/jobs/make/rescue")]
        [EndpointMetadata("Queue the game's own rescue job: carry a downed pawn to a free bed")]
        public async Task MakeJobRescue(HttpListenerContext context)
        {
            var mapId = RequestParser.GetMapId(context);
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var rescuerId = RequestParser.HasParameter(context, "rescuer_id")
                ? RequestParser.GetIntParameter(context, "rescuer_id")
                : 0;
            var result = _colonistService.MakeJobRescue(mapId, pawnId, rescuerId);
            await context.SendJsonResponse(result);
        }

        // Feed: the game's own FeedPatient job, which needs the patient in a
        // bed (ticket #3445). feeder_id and meal_id are optional; without them
        // the mod takes the nearest warden and the best meal the patient may
        // eat.
        [Post("/api/v1/jobs/make/feed")]
        [EndpointMetadata("Queue the game's own FeedPatient job for a downed colonist in a bed")]
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
        [EndpointMetadata("Set a prisoner's interaction mode (the recruitment policy)")]
        public async Task SetPrisonerInteraction(HttpListenerContext context)
        {
            var pawnId = RequestParser.GetIntParameter(context, "pawn_id");
            var mode = RequestParser.GetStringParameter(context, "mode");
            var result = _colonistService.SetPrisonerInteraction(pawnId, mode);
            await context.SendJsonResponse(result);
        }
    }
}
