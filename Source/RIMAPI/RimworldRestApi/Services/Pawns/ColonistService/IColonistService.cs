
using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    public interface IColonistService
    {
        ApiResult<List<PawnDto>> GetColonists();
        ApiResult<List<PawnPositionDto>> GetColonistPositions();
        ApiResult<PawnDto> GetColonist(int pawnId);
        ApiResult<List<ApiV1PawnDetailedDto>> GetColonistsDetailedV1();
        ApiResult<ApiV1PawnDetailedDto> GetColonistDetailedV1(int pawnId);
        ApiResult<List<PawnDetailedRequestDto>> GetColonistsDetailed();
        ApiResult<PawnDetailedRequestDto> GetColonistDetailed(int pawnId);
        ApiResult<PawnInventoryDto> GetColonistInventory(int pawnId);
        ApiResult<BodyPartsDto> GetColonistBodyParts(int pawnId);
        ApiResult<OpinionAboutPawnDto> GetOpinionAboutPawn(int pawnId, int otherPawnId);
        ApiResult<WorkListDto> GetWorkList();
        ApiResult<ColonistWorkPrioritiesDto> GetColonistWorkPriorities(int pawnId);
        // Every colonist's work levels in one read (fox ticket #4158).
        ApiResult<ColonistsWorkPrioritiesDto> GetColonistsWorkPriorities();
        ApiResult<List<TimeAssignmentDto>> GetTimeAssignmentsList();
        ApiResult SetColonistWorkPriority(WorkPriorityRequestDto body);
        ApiResult SetColonistsWorkPriority(ColonistsWorkPrioritiesRequestDto body);
        ApiResult<TraitDefDto> GetTraitDefDto(string traitName);
        ApiResult SetTimeAssignment(PawnTimeAssignmentRequestDto body);
        ApiResult MakeJobEquip(int mapId, int pawnId, int equipmentId);

        ApiResult MakeJobTend(int mapId, int pawnId, int patientId, int medicineId);
        // The Health tab's Self-tend option, which gates the self-tend job
        // above (ticket #3279).
        ApiResult SetSelfTend(int pawnId, bool enabled);

        // Growing the colony (ticket #3137): tame a wild person or animal,
        // arrest or capture a pawn into a prisoner bed, and set a prisoner's
        // recruitment policy. Rescue (ticket #3183) carries a downed non-enemy
        // to a free bed, which is how a crashed pod survivor is saved.
        ApiResult MakeJobTame(int mapId, int pawnId);
        // Slaughter ends a colony animal's life for meat and leather, the
        // Animals tab's own mark (fox ticket #3507).
        ApiResult MakeJobSlaughter(int mapId, int pawnId);
        ApiResult MakeJobChat(int mapId, int pawnId, int chatterId);
        ApiResult MakeJobArrest(int mapId, int pawnId, int arrestorId);
        ApiResult MakeJobRescue(int mapId, int pawnId, int rescuerId, int bedId);
        // Feeding a patient is the game's FeedPatient job and needs the
        // patient in a bed (ticket #3445); an animal is fed by the doctor's
        // animal feeder and lies in an animal bed (fox ticket #3519).
        ApiResult MakeJobFeed(int mapId, int pawnId, int feederId, int mealId);
        ApiResult SetPrisonerInteraction(int pawnId, string mode);

        // AllowedArea restricts a pawn to one of the map's allowed areas, or
        // lifts the restriction, the player's dropdown on the pawn's row (fox
        // ticket #3645).
        ApiResult SetAllowedArea(int mapId, int pawnId, string area);

        ApiResult MakeJobHaul(int mapId, int pawnId, int itemId, int destId);

        // Prioritize orders one job on one named colonist now, the way the
        // player's right-click "Prioritize X" entry does (fox ticket #3626).
        ApiResult MakeJobPrioritize(
            int mapId, int pawnId, string jobName, int thingId, int x, int z, string workName);

        ApiResult<List<OutfitDto>> GetOutfits();
        ApiResult<ImageDto> GetPawnPortraitImage(
            int pawnId,
            int width,
            int height,
            string direction
        );
    }
}