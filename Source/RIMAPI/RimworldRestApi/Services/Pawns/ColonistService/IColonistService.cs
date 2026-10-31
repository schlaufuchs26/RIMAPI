
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
        ApiResult<List<TimeAssignmentDto>> GetTimeAssignmentsList();
        ApiResult SetColonistWorkPriority(WorkPriorityRequestDto body);
        ApiResult SetColonistsWorkPriority(ColonistsWorkPrioritiesRequestDto body);
        ApiResult<TraitDefDto> GetTraitDefDto(string traitName);
        ApiResult SetTimeAssignment(PawnTimeAssignmentRequestDto body);
        ApiResult MakeJobEquip(int mapId, int pawnId, int equipmentId);

        ApiResult MakeJobTend(int mapId, int pawnId, int patientId, int medicineId);

        // Growing the colony (ticket #3137): tame a wild person or animal,
        // arrest or capture a pawn into a prisoner bed, and set a prisoner's
        // recruitment policy. Rescue (ticket #3183) carries a downed non-enemy
        // to a free bed, which is how a crashed pod survivor is saved.
        ApiResult MakeJobTame(int mapId, int pawnId);
        ApiResult MakeJobArrest(int mapId, int pawnId, int arrestorId);
        ApiResult MakeJobRescue(int mapId, int pawnId, int rescuerId);
        ApiResult SetPrisonerInteraction(int pawnId, string mode);

        ApiResult MakeJobHaul(int mapId, int pawnId, int itemId, int destId);

        ApiResult<List<OutfitDto>> GetOutfits();
        ApiResult<ImageDto> GetPawnPortraitImage(
            int pawnId,
            int width,
            int height,
            string direction
        );
    }
}