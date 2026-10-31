using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    // Ideoligion roles are the one colony lever the tool had none of (ticket
    // #3186): colony 8 raised "Ritual Pujari role unfilled" and every colonist
    // carried the -5 "no priest" thought, while the Assign tab and the whole
    // ideoligion window showed no slot to click. The roles are precepts of the
    // colony's ideoligion, so this service reads them off `ideo.RolesListForReading`
    // and fills one through `Precept_Role.Assign`, the game's own call: its
    // requirements check (the same one the role's tooltip names), the ability
    // refill and the mood thoughts all happen inside it, instead of a second
    // copy of them here.
    public class IdeoService : IIdeoService
    {
        public ApiResult<IdeoRolesDto> GetRoles(int mapId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult<IdeoRolesDto>.Fail($"Map with ID={mapId} not found");

                Ideo ideo = PlayerIdeo(out string reason);
                if (ideo == null) return ApiResult<IdeoRolesDto>.Fail(reason);

                List<Pawn> colonists = Colonists(map);
                IdeoRolesDto result = new IdeoRolesDto
                {
                    Ideo = ideo.name,
                    ColonistCount = colonists.Count,
                    Roles = new List<IdeoRoleDto>(),
                };
                foreach (Precept_Role role in ideo.RolesListForReading)
                {
                    IdeoRoleDto dto = new IdeoRoleDto
                    {
                        Def = role.def.defName,
                        Label = role.Label,
                        Active = role.Active,
                        LeaderRole = role.def.leaderRole,
                        Multi = role is Precept_RoleMulti,
                        Assigned = new List<RolePawnDto>(),
                        Eligible = new List<RolePawnDto>(),
                    };
                    foreach (Pawn pawn in role.ChosenPawns())
                    {
                        if (pawn != null) dto.Assigned.Add(PawnRef(pawn));
                    }
                    dto.Empty = dto.Assigned.Count == 0;
                    if (dto.Active && dto.Empty)
                    {
                        foreach (Pawn pawn in colonists)
                        {
                            if (role.RequirementsMet(pawn)) dto.Eligible.Add(PawnRef(pawn));
                        }
                    }
                    if (dto.Active && dto.Empty) result.EmptyCount++;
                    result.Roles.Add(dto);
                }
                return ApiResult<IdeoRolesDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<IdeoRolesDto>.Fail(ex.ToString());
            }
        }

        public ApiResult<RoleAssignResultDto> AssignRole(int mapId, int pawnId, string roleName)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult<RoleAssignResultDto>.Fail($"Map with ID={mapId} not found");

                Ideo ideo = PlayerIdeo(out string reason);
                if (ideo == null) return ApiResult<RoleAssignResultDto>.Fail(reason);

                Pawn pawn = map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.thingIDNumber == pawnId)
                    ?? map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber == pawnId);
                if (pawn == null) return ApiResult<RoleAssignResultDto>.Fail($"Pawn with ID={pawnId} not found");

                // The game's own gate for a role holder (Precept_Role.ValidatePawn):
                // a free colonist, alive and not destroyed.
                if (pawn.Faction != Faction.OfPlayer || !pawn.IsFreeNonSlaveColonist)
                {
                    return ApiResult<RoleAssignResultDto>.Fail(
                        $"{pawn.LabelShort} is not one of the colony's free colonists, so no role of its "
                        + "ideoligion takes him");
                }
                if (pawn.Dead) return ApiResult<RoleAssignResultDto>.Fail($"{pawn.LabelShort} is dead");

                Precept_Role role = FindRole(ideo, roleName, out string roleError);
                if (role == null) return ApiResult<RoleAssignResultDto>.Fail(roleError);
                if (!role.Active)
                {
                    return ApiResult<RoleAssignResultDto>.Fail(
                        $"{role.Label} is not active in the ideoligion \"{ideo.name}\": the role belongs to a meme "
                        + "this ideoligion does not have, so nobody can fill it");
                }
                if (role.IsAssigned(pawn))
                {
                    return ApiResult<RoleAssignResultDto>.Fail(
                        $"{pawn.LabelShort} already fills {role.Label}, so nothing changed");
                }
                if (!role.RequirementsMet(pawn))
                {
                    RoleRequirement unmet = role.GetFirstUnmetRequirement(pawn);
                    string why = unmet != null ? unmet.GetLabel(role) : "the role's own requirements";
                    return ApiResult<RoleAssignResultDto>.Fail(
                        $"{pawn.LabelShort} cannot take {role.Label}: {why}");
                }

                // A single role holds one pawn, and Assign hands it over from the
                // one who had it; report who that was, because the game sends him
                // a "role lost" letter and the mood hit is his.
                Pawn previous = role.ChosenPawnSingle();
                role.Assign(pawn, true);
                if (!role.IsAssigned(pawn))
                {
                    return ApiResult<RoleAssignResultDto>.Fail(
                        $"the game refused the assignment: {pawn.LabelShort} does not fill {role.Label} afterwards");
                }

                RoleAssignResultDto result = new RoleAssignResultDto
                {
                    Ideo = ideo.name,
                    Role = role.Label,
                    RoleDef = role.def.defName,
                    PawnId = pawn.thingIDNumber,
                    Pawn = pawn.LabelShort,
                    Assigned = role.ChosenPawns().Where(p => p != null).Select(PawnRef).ToList(),
                };
                if (previous != null && previous != pawn)
                {
                    result.PreviousPawnId = previous.thingIDNumber;
                    result.PreviousPawn = previous.LabelShort;
                }
                return ApiResult<RoleAssignResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<RoleAssignResultDto>.Fail(ex.ToString());
            }
        }

        // PlayerIdeo is the colony's own ideoligion, and the reason there is
        // none: without the Ideology expansion no faction carries one, so a
        // caller that asks about roles is told that instead of "empty list".
        private static Ideo PlayerIdeo(out string reason)
        {
            reason = null;
            Faction faction = Faction.OfPlayer;
            if (faction == null || faction.ideos == null || faction.ideos.PrimaryIdeo == null)
            {
                reason = "the colony has no ideoligion: ideoligion roles need the Ideology expansion, "
                    + "and this colony runs without one";
                return null;
            }
            return faction.ideos.PrimaryIdeo;
        }

        private static List<Pawn> Colonists(Map map)
        {
            return map.mapPawns.FreeColonists.ToList();
        }

        // FindRole takes the defName or the label the game shows, ignoring case:
        // a run reads "Pujari" from the alert and should not have to learn that
        // the def behind it is a moral-guide role.
        private static Precept_Role FindRole(Ideo ideo, string name, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "no role named: pass the role's label or defName (`roles` prints both)";
                return null;
            }
            string wanted = name.Trim();
            List<Precept_Role> roles = ideo.RolesListForReading;
            Precept_Role role = roles.FirstOrDefault(r =>
                    string.Equals(r.def.defName, wanted, StringComparison.OrdinalIgnoreCase))
                ?? roles.FirstOrDefault(r => string.Equals(r.Label, wanted, StringComparison.OrdinalIgnoreCase))
                ?? roles.FirstOrDefault(r =>
                    string.Equals(r.Label.Replace(" ", ""), wanted.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
            if (role == null)
            {
                string known = string.Join(", ", roles.Select(r => r.Label));
                error = $"no role \"{wanted}\" in the ideoligion; it has {known}";
            }
            return role;
        }

        private static RolePawnDto PawnRef(Pawn pawn)
        {
            return new RolePawnDto { Id = pawn.thingIDNumber, Name = pawn.LabelShort };
        }
    }
}
