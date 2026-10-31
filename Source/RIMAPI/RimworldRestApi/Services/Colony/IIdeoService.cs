using RIMAPI.Core;
using RIMAPI.Models;

namespace RIMAPI.Services
{
    // Ideoligion roles (ticket #3186): the read lists the colony ideoligion's
    // roles with the pawns that fill them, the command fills one.
    public interface IIdeoService
    {
        ApiResult<IdeoRolesDto> GetRoles(int mapId);
        ApiResult<RoleAssignResultDto> AssignRole(int mapId, int pawnId, string role);
    }
}
