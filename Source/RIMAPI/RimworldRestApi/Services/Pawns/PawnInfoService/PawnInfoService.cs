using System;
using System.Collections.Generic;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    public class PawnInfoService : IPawnInfoService
    {
        // --- 1. Get List of Pawns on Map ---
        public ApiResult<List<PawnDto>> GetPawnsOnMap(int mapId)
        {
            try
            {
                var map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult<List<PawnDto>>.Fail($"Map {mapId} not found.");

                var result = new List<PawnDto>();

                // Get ALL pawns (colonists, prisoners, enemies, animals, mechs)
                foreach (var pawn in map.mapPawns.AllPawns)
                {
                    result.Add(PawnHelper.PawnToDto(pawn));
                }

                return ApiResult<List<PawnDto>>.Ok(result);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error getting pawns on map: {ex}");
                return ApiResult<List<PawnDto>>.Fail(ex.Message);
            }
        }

        // --- 2. Get Detailed Pawn Info ---
        public ApiResult<PawnDetailedDto> GetPawnDetails(int pawnId)
        {
            try
            {
                var pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null) return ApiResult<PawnDetailedDto>.Fail($"Pawn {pawnId} not found.");

                var details = PawnHelper.PawnToDetailedDto(pawn);
                return ApiResult<PawnDetailedDto>.Ok(details);
            }
            catch (Exception ex)
            {
                return ApiResult<PawnDetailedDto>.Fail(ex.Message);
            }
        }

        public ApiResult<PawnInventoryDto> GetPawnInventory(int pawnId)
        {
            try
            {
                var pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null) return ApiResult<PawnInventoryDto>.Fail($"Pawn {pawnId} not found.");

                var inventory = PawnHelper.GetPawnInventory(pawn);

                return ApiResult<PawnInventoryDto>.Ok(inventory);
            }
            catch (Exception ex)
            {
                return ApiResult<PawnInventoryDto>.Fail(ex.Message);
            }
        }

        // The people a colony can still gain (ticket #3137): wild people,
        // prisoners, slaves, guests and downed raiders. Only humanlike, spawned,
        // living pawns that are not free colonists are listed; an animal is
        // tamed by id from `map/pawns`, and a colonist is `colonists`.
        public ApiResult<List<PersonDto>> GetPeople(int mapId)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult<List<PersonDto>>.Fail($"Map {mapId} not found.");

                List<PersonDto> result = new List<PersonDto>();
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned) continue;
                    if (!pawn.RaceProps.Humanlike) continue;
                    if (pawn.IsColonistPlayerControlled) continue;
                    if (pawn.Faction == Faction.OfPlayer && !pawn.IsPrisonerOfColony && !pawn.IsSlaveOfColony) continue;
                    result.Add(PersonDtoOf(pawn));
                }
                result.Sort((a, b) => string.Compare(a.Kind, b.Kind, StringComparison.Ordinal));
                return ApiResult<List<PersonDto>>.Ok(result);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error reading the people on map {mapId}: {ex}");
                return ApiResult<List<PersonDto>>.Fail(ex.Message);
            }
        }

        private static PersonDto PersonDtoOf(Pawn pawn)
        {
            PersonDto dto = new PersonDto
            {
                Id = pawn.thingIDNumber,
                Name = pawn.Name?.ToStringShort ?? pawn.LabelShort,
                Kind = PersonKind(pawn),
                Faction = pawn.Faction?.Name,
                Downed = pawn.Downed,
                Health = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f,
                Tameable = TameUtility.CanTame(pawn),
                Position = new PositionDto { X = pawn.Position.x, Y = pawn.Position.y, Z = pawn.Position.z },
            };
            if (pawn.guest != null)
            {
                dto.Resistance = pawn.guest.Resistance;
                dto.Will = pawn.guest.will;
                dto.Recruitable = pawn.guest.Recruitable;
                dto.InteractionMode = pawn.guest.ExclusiveInteractionMode?.defName;
            }
            return dto;
        }

        // PersonKind names the side a pawn is on in the words the tool uses:
        // a wild man is factionless like a wanderer, and a prisoner's own
        // faction is not the one holding him.
        private static string PersonKind(Pawn pawn)
        {
            if (pawn.IsWildMan()) return "wild";
            if (pawn.IsPrisonerOfColony) return "prisoner";
            if (pawn.IsSlaveOfColony) return "slave";
            if (pawn.HostFaction == Faction.OfPlayer) return "guest";
            if (pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer)) return "hostile";
            if (pawn.Faction == null) return "factionless";
            return "neutral";
        }

        // --- Internal Mappers ---

        private PawnDto MapPawnToSummary(Pawn pawn)

        {
            return new PawnDto
            {
                Id = pawn.thingIDNumber,
                Name = pawn.Name?.ToStringShort ?? pawn.Label,
                Gender = pawn.gender.ToString(),
                Age = pawn.ageTracker.AgeBiologicalYears,
                Health = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 0f,
                Mood = pawn.needs?.mood?.CurLevelPercentage ?? 0f, // Null for animals/mechs
                Hunger = pawn.needs?.food?.CurLevelPercentage ?? 0f,
                Position = new PositionDto { X = pawn.Position.x, Y = pawn.Position.y, Z = pawn.Position.z }
            };
        }
    }
}