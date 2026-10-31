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

        // GetRituals lists the ideoligion's rituals with the game's own
        // readiness verdict (ticket #3263). The verdict is the sentence the
        // Begin dialog shows in place of the button, so a run reads the same
        // reason the player would: an unfilled role, a missing altar, a ritual
        // of that name already running.
        public ApiResult<IdeoRitualsDto> GetRituals(int mapId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult<IdeoRitualsDto>.Fail($"Map with ID={mapId} not found");

                Ideo ideo = PlayerIdeo(out string reason);
                if (ideo == null) return ApiResult<IdeoRitualsDto>.Fail(reason);

                List<Pawn> colonists = Colonists(map);
                IdeoRitualsDto result = new IdeoRitualsDto
                {
                    Ideo = ideo.name,
                    Tick = Find.TickManager.TicksGame,
                    Rituals = new List<IdeoRitualDto>(),
                };
                foreach (Precept_Ritual ritual in Rituals(ideo))
                {
                    IdeoRitualDto dto = DescribeRitual(ritual, colonists);
                    if (dto.Ready) result.ReadyCount++;
                    result.Rituals.Add(dto);
                }
                return ApiResult<IdeoRitualsDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<IdeoRitualsDto>.Fail(ex.ToString());
            }
        }

        // StartRitual starts one ritual the way the Begin dialog's own confirm
        // does: it builds the role assignments the game builds, asks the
        // behavior worker whether it may start, and hands it to
        // RitualBehaviorWorker.TryExecuteOn. The answer is read back from the
        // game, because TryExecuteOn returns void and stays silent on a refusal.
        public ApiResult<RitualStartResultDto> StartRitual(
            int mapId, string ritualName, int pawnId, int targetThingId, int x, int z, bool hasCell)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult<RitualStartResultDto>.Fail($"Map with ID={mapId} not found");

                Ideo ideo = PlayerIdeo(out string reason);
                if (ideo == null) return ApiResult<RitualStartResultDto>.Fail(reason);

                Precept_Ritual ritual = FindRitual(ideo, ritualName, out string ritualError);
                if (ritual == null) return ApiResult<RitualStartResultDto>.Fail(ritualError);
                if (ritual.behavior == null)
                {
                    return ApiResult<RitualStartResultDto>.Fail(
                        $"{ritual.Label} has no ritual behavior in this game build, so it cannot be started");
                }

                Pawn organizer = null;
                if (pawnId > 0)
                {
                    organizer = FindPawn(map, pawnId);
                    if (organizer == null)
                        return ApiResult<RitualStartResultDto>.Fail($"Pawn with ID={pawnId} not found");
                }

                TargetInfo target = TargetInfo.Invalid;
                bool targetGiven = false;
                if (hasCell)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(map))
                        return ApiResult<RitualStartResultDto>.Fail($"{x},{z} is outside the map");
                    target = new TargetInfo(cell, map);
                    targetGiven = true;
                }
                else if (targetThingId > 0)
                {
                    Thing thing = FindThing(map, targetThingId);
                    if (thing == null)
                        return ApiResult<RitualStartResultDto>.Fail(
                            $"Thing with ID={targetThingId} not found on this map (the `things` read prints ids)");
                    target = new TargetInfo(thing);
                    targetGiven = true;
                }

                // An obligation is the game's own "this ritual is due": the
                // target it was created for is the one that pays it off.
                RitualObligation obligation = PickObligation(ritual, target, targetGiven);
                if (obligation != null && !targetGiven) target = obligation.FirstValidTarget;
                // Without a named place, ask the ritual's own target filter, the
                // same call the ideo window makes when it opens the Begin dialog.
                if (!target.IsValid) target = BestTarget(ritual, organizer ?? RoleHolder(ritual));

                RitualStartResultDto result = new RitualStartResultDto
                {
                    Ideo = ideo.name,
                    Ritual = ritual.Label,
                    Def = ritual.def.defName,
                    Target = TargetRef(target),
                    Participants = new List<RolePawnDto>(),
                };
                if (organizer != null)
                {
                    result.Organizer = organizer.LabelShort;
                    result.OrganizerId = organizer.thingIDNumber;
                }

                if (!target.IsValid)
                {
                    result.Reason = "no target: pass the cell the ritual is held at (x1/z1) or the building "
                        + "or spot (target_id), as the Begin dialog asks";
                    return ApiResult<RitualStartResultDto>.Ok(result);
                }
                string why = ritual.behavior.CanStartRitualNow(target, ritual, null, null);
                if (!string.IsNullOrEmpty(why))
                {
                    result.Reason = why;
                    return ApiResult<RitualStartResultDto>.Ok(result);
                }

                RitualRoleAssignments assignments = Dialog_BeginRitual.CreateRitualRoleAssignments(
                    ritual, target, map, null, null, null, null);
                if (assignments == null || assignments.Participants == null || assignments.Participants.Count == 0)
                {
                    result.Reason = "no colonist can take part: the ritual's roles are empty, and its own "
                        + "requirements (a role holder, a free pawn) are not met";
                    return ApiResult<RitualStartResultDto>.Ok(result);
                }

                ritual.behavior.TryExecuteOn(target, organizer, ritual, obligation, assignments, true);

                // TryExecuteOn returns nothing and gives up silently, so the
                // answer comes from the game state: a LordJob_Ritual of this
                // ritual has to stand now.
                LordJob_Ritual running = Find.IdeoManager.GetActiveRituals(map)
                    .FirstOrDefault(r => r.Ritual == ritual);
                if (running == null)
                {
                    result.Reason = "the game accepted the order but no ritual of that name is running; "
                        + "`messages` and `dialogs` say why it did not start";
                    return ApiResult<RitualStartResultDto>.Ok(result);
                }

                result.Started = true;
                result.Target = TargetRef(running.selectedTarget);
                result.ObligationFulfilled = obligation != null
                    && (ritual.activeObligations == null || !ritual.activeObligations.Contains(obligation));
                RitualRoleAssignments live = running.assignments ?? assignments;
                if (live.Participants != null)
                {
                    foreach (Pawn pawn in live.Participants)
                    {
                        if (pawn != null) result.Participants.Add(PawnRef(pawn));
                    }
                }
                return ApiResult<RitualStartResultDto>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResult<RitualStartResultDto>.Fail(ex.ToString());
            }
        }

        // Rituals are precepts of the ideoligion; there is no separate list.
        private static List<Precept_Ritual> Rituals(Ideo ideo)
        {
            return ideo.PreceptsListForReading.OfType<Precept_Ritual>().ToList();
        }

        // DescribeRitual fills one read entry. The target is the game's own
        // choice (an obligation's target, else the ritual filter's best one),
        // because CanStartRitualNow only checks the roles once it has a valid
        // target: without one every ritual would read as ready.
        private static IdeoRitualDto DescribeRitual(Precept_Ritual ritual, List<Pawn> colonists)
        {
            IdeoRitualDto dto = new IdeoRitualDto
            {
                Def = ritual.def.defName,
                Label = ritual.Label,
                Anytime = ritual.isAnytime,
                DateTriggered = ritual.IsDateTriggered,
                RepeatPenaltyActive = ritual.RepeatPenaltyActive,
                RepeatQualityPenalty = ritual.RepeatQualityPenalty,
                RepeatPenaltyTimeLeft = ritual.RepeatPenaltyTimeLeft,
                Obligations = new List<RitualObligationDto>(),
                Roles = new List<RitualRoleDto>(),
            };
            if (ritual.abilityOnCooldownUntilTick > 0)
            {
                dto.CooldownTicks = Math.Max(0, ritual.abilityOnCooldownUntilTick - Find.TickManager.TicksGame);
            }

            RitualObligation first = null;
            if (ritual.activeObligations != null)
            {
                foreach (RitualObligation obligation in ritual.activeObligations)
                {
                    if (!obligation.StillValid) continue;
                    if (first == null) first = obligation;
                    dto.Obligations.Add(new RitualObligationDto
                    {
                        Id = obligation.ID,
                        Expires = obligation.expires,
                        TicksUntilExpiration = obligation.TicksUntilExpiration,
                        Target = TargetRef(obligation.FirstValidTarget),
                    });
                }
            }

            if (ritual.behavior != null && ritual.behavior.def != null && ritual.behavior.def.roles != null)
            {
                foreach (RitualRole role in ritual.behavior.def.roles)
                {
                    dto.Roles.Add(new RitualRoleDto
                    {
                        Id = role.id,
                        Label = role.Label.ToString(),
                        Required = role.required,
                        MaxCount = role.maxCount,
                    });
                }
            }

            TargetInfo target = first != null
                ? first.FirstValidTarget
                : BestTarget(ritual, RoleHolder(ritual) ?? colonists.FirstOrDefault());
            dto.Target = TargetRef(target);

            // CanStartRitualNow is not safe without a target: the speech worker
            // dereferences it, and the game's own ideo window only asks once the
            // player picked a place. So a ritual without a target reads as not
            // ready and says which field supplies the place.
            if (!target.IsValid)
            {
                dto.Ready = false;
                dto.Reason = "no target yet: the game picks the place at Begin, so pass the cell (x1/z1) "
                    + "or the thing (target_id) the ritual is held at";
                return dto;
            }
            string why = null;
            try
            {
                why = ritual.behavior?.CanStartRitualNow(target, ritual, null, null);
            }
            catch (Exception ex)
            {
                // A third-party behavior worker can throw on a target it did not
                // expect; the read still lists the ritual, with that reason.
                why = "the game could not judge this ritual at that target: " + ex.Message;
            }
            dto.Ready = string.IsNullOrEmpty(why);
            dto.Reason = why ?? "";
            return dto;
        }

        // RoleHolder is the pawn who fills the first ideoligion role the ritual
        // asks for (the leader for the speech), which is who the target filter
        // measures from. RitualRole.precept names the PreceptDef of that role.
        private static Pawn RoleHolder(Precept_Ritual ritual)
        {
            if (ritual.ideo == null || ritual.ideo.RolesListForReading == null
                || ritual.behavior?.def?.roles == null) return null;
            foreach (RitualRole wanted in ritual.behavior.def.roles)
            {
                if (wanted.precept == null) continue;
                Precept_Role role = ritual.ideo.RolesListForReading
                    .FirstOrDefault(r => r.def == wanted.precept);
                if (role == null) continue;
                Pawn pawn = role.ChosenPawns().FirstOrDefault(p => p != null);
                if (pawn != null) return pawn;
            }
            return null;
        }

        // BestTarget is the game's own target choice for a ritual that has none
        // yet, the same call CompAbilityEffect_StartRitual makes. A target
        // filter of a third-party ritual may throw on an odd map; the readiness
        // hint then simply comes without a target.
        private static TargetInfo BestTarget(Precept_Ritual ritual, Pawn organizer)
        {
            if (ritual.targetFilter == null || organizer == null || organizer.Map == null) return TargetInfo.Invalid;
            try
            {
                return ritual.targetFilter.BestTarget(new TargetInfo(organizer), TargetInfo.Invalid);
            }
            catch (Exception)
            {
                return TargetInfo.Invalid;
            }
        }

        // PickObligation takes the due obligation a start pays off: the one
        // whose target the caller named, or the first still-valid one when the
        // caller named none.
        private static RitualObligation PickObligation(Precept_Ritual ritual, TargetInfo target, bool targetGiven)
        {
            if (ritual.activeObligations == null) return null;
            List<RitualObligation> valid = ritual.activeObligations.Where(o => o.StillValid).ToList();
            if (valid.Count == 0) return null;
            if (!targetGiven) return valid[0];
            return valid.FirstOrDefault(o => SameTarget(o.FirstValidTarget, target));
        }

        private static bool SameTarget(TargetInfo a, TargetInfo b)
        {
            if (!a.IsValid || !b.IsValid) return false;
            if (a.HasThing != b.HasThing) return false;
            return a.HasThing ? a.Thing == b.Thing : a.Cell == b.Cell;
        }

        private static RitualTargetDto TargetRef(TargetInfo target)
        {
            RitualTargetDto dto = new RitualTargetDto();
            if (!target.IsValid) return dto;
            dto.Valid = true;
            dto.X = target.Cell.x;
            dto.Z = target.Cell.z;
            if (target.HasThing && target.Thing != null)
            {
                dto.ThingId = target.Thing.thingIDNumber;
                dto.Label = target.Thing.LabelShort;
            }
            return dto;
        }

        private static Pawn FindPawn(Map map, int pawnId)
        {
            return map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.thingIDNumber == pawnId)
                ?? map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber == pawnId);
        }

        private static Thing FindThing(Map map, int thingId)
        {
            return map.listerThings.AllThings.FirstOrDefault(t => t.thingIDNumber == thingId);
        }

        // FindRitual takes the defName or the label the game shows, ignoring
        // case, so a run reads "Leader speech" from the window and does not have
        // to know the defName behind it.
        private static Precept_Ritual FindRitual(Ideo ideo, string name, out string error)
        {
            error = null;
            List<Precept_Ritual> rituals = Rituals(ideo);
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "no ritual named: pass the ritual's label or defName (`rituals` prints both)";
                return null;
            }
            string wanted = name.Trim();
            Precept_Ritual ritual = rituals.FirstOrDefault(r =>
                    string.Equals(r.def.defName, wanted, StringComparison.OrdinalIgnoreCase))
                ?? rituals.FirstOrDefault(r => string.Equals(r.Label, wanted, StringComparison.OrdinalIgnoreCase))
                ?? rituals.FirstOrDefault(r =>
                    string.Equals(r.Label.Replace(" ", ""), wanted.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
            if (ritual == null)
            {
                string known = string.Join(", ", rituals.Select(r => r.Label));
                error = $"no ritual \"{wanted}\" in the ideoligion; it has {known}";
            }
            return ritual;
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
