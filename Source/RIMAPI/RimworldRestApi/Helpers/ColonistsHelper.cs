using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RIMAPI.Models;
using Verse;

namespace RIMAPI.Helpers
{
    public static class ColonistsHelper
    {
        public static List<Pawn> GetColonistsList()
        {
            return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists.ToList();
        }

        public static PawnDto GetSingleColonistDto(int pawnId)
        {
            return GetColonistsList()
                    .Where(s => s.thingIDNumber == pawnId)
                    .Select(p => PawnHelper.PawnToDto(p))
                    .FirstOrDefault();
        }

        public static PawnDetailedRequestDto GetSingleColonistDetailedDto(int pawnId)
        {
            var colonist = GetColonistsList()
                    .Where(s => s.thingIDNumber == pawnId)
                    .FirstOrDefault();

            return new PawnDetailedRequestDto
            {
                Pawn = PawnHelper.PawnToDto(colonist),
                Detailes = PawnHelper.PawnToDetailedDto(colonist)
            };
        }

        public static List<PawnDto> GetColonistsDtos()
        {
            return GetColonistsList()
                    .Select(p => PawnHelper.PawnToDto(p))
                    .ToList();
        }

        public static List<PawnDetailedRequestDto> GetColonistsDetailedDtos()
        {
            var result = new List<PawnDetailedRequestDto>();

            var colonists = GetColonistsList();
            foreach (Pawn colonist in colonists)
            {
                result.Add(new PawnDetailedRequestDto
                {
                    Pawn = PawnHelper.PawnToDto(colonist),
                    Detailes = PawnHelper.PawnToDetailedDto(colonist)
                });
            }

            return result;
        }

        // ToApiV1 builds the detailed read's answer from the two halves the
        // colonist helpers return (ticket #4172). The same 25-line mapping stood
        // twice in ColonistService, a file already past the size cap, so a new
        // field had to be added in both places and neither could be tested
        // without the game.
        public static ApiV1PawnDetailedDto ToApiV1(PawnDetailedRequestDto data)
        {
            return new ApiV1PawnDetailedDto
            {
                Colonist = data.Pawn,
                BodySize = data.Detailes.BodySize,
                Sleep = data.Detailes.Sleep,
                Comfort = data.Detailes.Comfort,
                Beauty = data.Detailes.Beauty,
                Joy = data.Detailes.Joy,
                Energy = data.Detailes.Energy,
                DrugsDesire = data.Detailes.DrugsDesire,
                SurroundingBeauty = data.Detailes.SurroundingBeauty,
                FreshAir = data.Detailes.FreshAir,
                ColonistWorkInfo = data.Detailes.WorkInfo,
                MoodInfo = data.Detailes.MoodInfo,
                PoliciesInfo = data.Detailes.PoliciesInfo,
                ColonistMedicalInfo = data.Detailes.MedicalInfo,
                SocialInfo = data.Detailes.SocialInfo,
                ReservedBy = data.Detailes.ReservedBy,
            };
        }

        public static List<PawnPositionDto> GetColonistPositions()
        {
            var positions = new List<PawnPositionDto>();

            try
            {
                var map = Find.CurrentMap;
                if (map == null)
                    return positions;

                var freeColonists = map.mapPawns?.FreeColonists;
                if (freeColonists == null)
                    return positions;

                foreach (var pawn in freeColonists)
                {
                    if (pawn == null || !pawn.Spawned)
                        continue;

                    positions.Add(new PawnPositionDto
                    {
                        Id = pawn.thingIDNumber,
                        MapId = map.uniqueID,
                        X = pawn.Position.x,
                        Z = pawn.Position.z
                    });
                }
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error getting colonist positions - {ex}");
            }

            return positions;
        }
    }
}
