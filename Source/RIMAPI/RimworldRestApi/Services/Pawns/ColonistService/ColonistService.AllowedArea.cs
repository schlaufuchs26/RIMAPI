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
    public partial class ColonistService
    {
        // SetAllowedArea restricts a pawn to one of the map's allowed areas, or
        // lifts the restriction, the way the player's own dropdown on the
        // pawn's row does (fox ticket #3645). The colonists read has printed
        // `allowed_area` since #3404, but nothing could set it: a run could not
        // keep a colonist behind the walls during a raid, and the field was
        // only checkable with the game's UI or a hand-edited save.
        //
        // Only areas the game offers in that dropdown count
        // (AssignableAsAllowed): Home and the named allowed areas, not the roof
        // areas. The answer is the pawn's area afterwards, not an
        // acknowledgement, because the game can drop the assignment for a pawn
        // it does not keep on the map.
        public ApiResult SetAllowedArea(int mapId, int pawnId, string areaWord)
        {
            try
            {
                Map map = MapHelper.GetMapByID(mapId);
                if (map == null) return ApiResult.Fail($"Map with ID={mapId} not found");
                Pawn pawn = PawnHelper.FindPawnForOrder(map, pawnId);
                if (pawn == null) return ApiResult.Fail($"Pawn with ID={pawnId} not found on map {mapId}");
                // A carried pawn has no allowed area: the game keeps the
                // restriction per map and he stands on none (fox #3719).
                string carried = CombatRefusal.CarriedRefusal(
                    Describe(pawn), pawn.CarriedBy?.LabelShort, "restrict");
                if (carried != null) return ApiResult.Fail(carried);
                if (!pawn.Spawned)
                {
                    return ApiResult.Fail($"{Describe(pawn)} is not on the map, so the game keeps no allowed area for him");
                }
                if (pawn.playerSettings == null)
                {
                    return ApiResult.Fail($"{Describe(pawn)} has no player settings, so the game gives him no allowed area");
                }
                string word = (areaWord ?? string.Empty).Trim();
                if (word.Length == 0)
                {
                    return ApiResult.Fail(
                        "allowed_area needs an area: a label the map has, or Unrestricted to lift the restriction");
                }

                List<Area> areas = map.areaManager.AllAreas
                    .Where(a => a.AssignableAsAllowed())
                    .ToList();

                AllowedAreaDto result = new AllowedAreaDto
                {
                    PawnId = pawn.thingIDNumber,
                    Name = pawn.LabelShort,
                };

                Area wanted = null;
                if (!AllowedAreaRules.IsUnrestricted(word))
                {
                    int index = AllowedAreaRules.IndexOf(
                        word,
                        areas.Select(a => a.Label).ToList(),
                        areas.Select(a => a.GetType().Name).ToList());
                    if (index < 0)
                    {
                        return ApiResult.Fail(
                            $"no allowed area is named \"{word}\"; the map has: " +
                            AllowedAreaRules.OptionsText(areas.Select(a => a.Label).ToList()) +
                            " (or Unrestricted to lift the restriction)");
                    }
                    wanted = areas[index];
                }

                pawn.playerSettings.AreaRestrictionInPawnCurrentMap = wanted;

                Area now = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
                result.AllowedArea = now?.Label;
                result.AllowedAreaCells = now?.TrueCount ?? 0;
                if (now != wanted)
                {
                    result.Note = "the game did not take the change";
                }
                return ApiResult.Ok(result);
            }
            catch (Exception ex)
            {
                LogApi.Error($"Error setting allowed area for pawn {pawnId}: {ex}");
                return ApiResult.Fail(ex.Message);
            }
        }
    }
}
