using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    // Ideoligion roles (ticket #3186): the read lists the colony ideoligion's
    // roles with the pawns that fill them, the command fills one. The rituals
    // and the leader's speech are the mood levers next to them (ticket #3263):
    // one read lists them with the game's readiness verdict, one command starts
    // one through the game's own start.
    public interface IIdeoService
    {
        ApiResult<IdeoRolesDto> GetRoles(int mapId);
        ApiResult<RoleAssignResultDto> AssignRole(int mapId, int pawnId, string role);
        ApiResult<IdeoRitualsDto> GetRituals(int mapId);
        ApiResult<RitualStartResultDto> StartRitual(
            int mapId, string ritualName, int pawnId, int targetThingId, int x, int z, bool hasCell);
        ApiResult<RitualEndResultDto> EndRitual(int mapId, string ritualName);
    }
}
