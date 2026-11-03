using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Core;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The writes the zone and area panels offer (fox ticket #4316, parity wave
    /// 6). Every method runs the game's own action instead of re-implementing
    /// it: SetPlantDefToGrow is what the Plant button calls, allowSow and
    /// allowCut are the two gizmo toggles, RenamableLabel is what
    /// Dialog_Rename&lt;T&gt; writes, Area.Invert and Area.Delete are the Manage
    /// areas dialog's buttons, and the cells go through the same indexer the
    /// expand and shrink designators use. The refusals follow the dialogs, so a
    /// caller is stopped by the same rules a player is (ZoneEditRules).
    /// </summary>
    public static class ZoneEdit
    {
        /// <summary>
        /// The crops this zone's Plant button offers. The game builds the menu
        /// from PlantUtility.ValidPlantTypesForGrowers for the selected zone and
        /// drops the crops whose research is missing (and the ones that need a
        /// permanent darkness the map does not have), so the list is exactly
        /// what a player sees.
        /// </summary>
        public static ApiResult<GrowingZonePlantsDto> GetSelectablePlants(Map map, int zoneId)
        {
            Zone_Growing zone = FindGrowingZone(map, zoneId, out string zoneError);
            if (zone == null)
            {
                return ApiResult<GrowingZonePlantsDto>.Fail(zoneError);
            }

            return ApiResult<GrowingZonePlantsDto>.Ok(
                new GrowingZonePlantsDto
                {
                    ZoneId = zone.ID,
                    PlantDefName = zone.GetPlantDefToGrow()?.defName,
                    Plants = SelectablePlants(map, zone)
                        .Select(def => new SelectablePlantDto
                        {
                            DefName = def.defName,
                            Label = def.label,
                            SowMinSkill = def.plant.sowMinSkill,
                            Purpose = def.plant.purpose.ToString(),
                        })
                        .ToList(),
                }
            );
        }

        /// <summary>
        /// Set a growing zone's crop and its two toggles. The answer is the zone
        /// after the write, plus the game's own "nobody can plant this" warning
        /// when the chosen crop needs a Plants skill no colonist reaches.
        /// </summary>
        public static ApiResult<GrowingZoneDto> UpdateGrowingZone(
            Map map,
            UpdateGrowingZoneRequestDto request
        )
        {
            if (request == null)
            {
                return ApiResult<GrowingZoneDto>.Fail("Request cannot be null.");
            }

            string refusal = ZoneEditRules.GrowingRefusal(
                request.PlantDef,
                request.AllowSow,
                request.AllowCut
            );
            if (refusal != null)
            {
                return ApiResult<GrowingZoneDto>.Fail(refusal);
            }

            Zone_Growing zone = FindGrowingZone(map, request.ZoneId, out string zoneError);
            if (zone == null)
            {
                return ApiResult<GrowingZoneDto>.Fail(zoneError);
            }

            ThingDef plantDef = null;
            if (!string.IsNullOrWhiteSpace(request.PlantDef))
            {
                List<ThingDef> selectable = SelectablePlants(map, zone);
                refusal = ZoneEditRules.PlantRefusal(
                    request.PlantDef,
                    selectable.Select(def => def.defName).ToList()
                );
                if (refusal != null)
                {
                    return ApiResult<GrowingZoneDto>.Fail(refusal);
                }

                plantDef = selectable.First(def => def.defName == request.PlantDef);
                zone.SetPlantDefToGrow(plantDef);
            }

            if (request.AllowSow.HasValue)
            {
                zone.allowSow = request.AllowSow.Value;
            }

            if (request.AllowCut.HasValue)
            {
                zone.allowCut = request.AllowCut.Value;
            }

            GrowingZoneDto dto = FarmHelper.GetGrowingZoneById(map, zone.ID);
            if (plantDef == null)
            {
                return ApiResult<GrowingZoneDto>.Ok(dto);
            }

            string warning = NoGrowerWarning(map, plantDef);
            return warning == null
                ? ApiResult<GrowingZoneDto>.Ok(dto)
                : ApiResult<GrowingZoneDto>.Partial(dto, new[] { warning });
        }

        /// <summary>
        /// Rename a zone, whatever its type. The dialog refuses an empty name, a
        /// name longer than 28 characters, a name another zone carries and a
        /// name a storage group carries; this does the same.
        /// </summary>
        public static ApiResult<ZoneDto> RenameZone(Map map, RenameZoneRequestDto request)
        {
            if (request == null)
            {
                return ApiResult<ZoneDto>.Fail("Request cannot be null.");
            }

            Zone zone = map.zoneManager.AllZones.FirstOrDefault(z => z.ID == request.ZoneId);
            if (zone == null)
            {
                return ApiResult<ZoneDto>.Fail(
                    $"No zone on map {map.uniqueID} has id {request.ZoneId}. "
                        + "GET /api/v1/map/zones lists the zones and their ids."
                );
            }

            List<string> taken = map
                .zoneManager.AllZones.Where(z => z.ID != zone.ID)
                .Select(z => z.label)
                .ToList();
            string refusal = ZoneEditRules.LabelRefusal(request.Label, taken);
            if (refusal != null)
            {
                return ApiResult<ZoneDto>.Fail(refusal);
            }

            if (map.storageGroups.HasGroupWithName(request.Label))
            {
                return ApiResult<ZoneDto>.Fail(
                    $"The name \"{request.Label}\" is already in use by a storage group; "
                        + "the game's rename dialog refuses it for the same reason."
                );
            }

            string before = zone.label;
            zone.RenamableLabel = request.Label;
            LogApi.Info($"Renamed zone {zone.ID} from '{before}' to '{request.Label}'");
            return ApiResult<ZoneDto>.Ok(MapHelper.GetZoneDto(zone, map));
        }

        /// <summary>
        /// The Manage areas dialog's New area button: an empty allowed area, at
        /// most 10 per map.
        /// </summary>
        public static ApiResult<AreaCellsDto> CreateArea(Map map, CreateAreaRequestDto request)
        {
            if (request == null)
            {
                return ApiResult<AreaCellsDto>.Fail("Request cannot be null.");
            }

            if (!string.IsNullOrEmpty(request.Label))
            {
                string refusal = ZoneEditRules.LabelRefusal(
                    request.Label,
                    map.areaManager.AllAreas.Select(a => a.Label).ToList()
                );
                if (refusal != null)
                {
                    return ApiResult<AreaCellsDto>.Fail(refusal);
                }
            }

            if (!map.areaManager.CanMakeNewAllowed())
            {
                return ApiResult<AreaCellsDto>.Fail(
                    $"The map already holds {AreaManager.MaxAllowedAreas} allowed areas; "
                        + "the game's Manage areas dialog stops there and hides the New area button."
                );
            }

            Area_Allowed area;
            if (!map.areaManager.TryMakeNewAllowed(out area))
            {
                return ApiResult<AreaCellsDto>.Fail(
                    "The game refused a new allowed area on this map."
                );
            }

            if (!string.IsNullOrEmpty(request.Label))
            {
                area.RenamableLabel = request.Label;
            }

            LogApi.Info($"Created allowed area {area.ID} ('{area.Label}')");
            return ApiResult<AreaCellsDto>.Ok(MapHelper.GetAreaCells(map, area));
        }

        /// <summary>Rename one allowed area, under the dialog's own rules.</summary>
        public static ApiResult<AreaCellsDto> RenameArea(Map map, RenameAreaRequestDto request)
        {
            if (request == null)
            {
                return ApiResult<AreaCellsDto>.Fail("Request cannot be null.");
            }

            Area area = MapHelper.FindArea(map, request.Label, out string areaError);
            if (area == null)
            {
                return ApiResult<AreaCellsDto>.Fail(areaError);
            }

            string mutableError = MutableRefusal(area, "renamed");
            if (mutableError != null)
            {
                return ApiResult<AreaCellsDto>.Fail(mutableError);
            }

            string refusal = ZoneEditRules.LabelRefusal(
                request.NewLabel,
                map.areaManager.AllAreas.Where(a => a != area).Select(a => a.Label).ToList()
            );
            if (refusal != null)
            {
                return ApiResult<AreaCellsDto>.Fail(refusal);
            }

            string before = area.Label;
            area.RenamableLabel = request.NewLabel;
            LogApi.Info($"Renamed area {area.ID} from '{before}' to '{request.NewLabel}'");
            return ApiResult<AreaCellsDto>.Ok(MapHelper.GetAreaCells(map, area));
        }

        /// <summary>The dialog's Invert button: Area.Invert, over the whole grid.</summary>
        public static ApiResult<AreaCellsDto> InvertArea(Map map, AreaChangeRequestDto request)
        {
            if (request == null)
            {
                return ApiResult<AreaCellsDto>.Fail("Request cannot be null.");
            }

            Area area = MapHelper.FindArea(map, request.Label, out string areaError);
            if (area == null)
            {
                return ApiResult<AreaCellsDto>.Fail(areaError);
            }

            string mutableError = MutableRefusal(area, "inverted");
            if (mutableError != null)
            {
                return ApiResult<AreaCellsDto>.Fail(mutableError);
            }

            area.Invert();
            LogApi.Info($"Inverted area {area.ID} ('{area.Label}')");
            return ApiResult<AreaCellsDto>.Ok(MapHelper.GetAreaCells(map, area));
        }

        /// <summary>
        /// The dialog's Shrink button: clear a rectangle from the area, the
        /// game's own shrink designator applied at once.
        /// </summary>
        public static ApiResult<AreaCellsDto> ShrinkArea(Map map, ShrinkAreaRequestDto request)
        {
            if (request == null)
            {
                return ApiResult<AreaCellsDto>.Fail("Request cannot be null.");
            }

            if (request.PointA == null || request.PointB == null)
            {
                return ApiResult<AreaCellsDto>.Fail(
                    "point_a and point_b are required: the rectangle the area shrinks by."
                );
            }

            Area area = MapHelper.FindArea(map, request.Label, out string areaError);
            if (area == null)
            {
                return ApiResult<AreaCellsDto>.Fail(areaError);
            }

            string mutableError = MutableRefusal(area, "shrunk");
            if (mutableError != null)
            {
                return ApiResult<AreaCellsDto>.Fail(mutableError);
            }

            int x1 = Math.Min(request.PointA.X, request.PointB.X);
            int x2 = Math.Max(request.PointA.X, request.PointB.X);
            int z1 = Math.Min(request.PointA.Z, request.PointB.Z);
            int z2 = Math.Max(request.PointA.Z, request.PointB.Z);

            int removed = 0,
                kept = 0,
                outside = 0;
            for (int x = x1; x <= x2; x++)
            {
                for (int z = z1; z <= z2; z++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(map))
                    {
                        outside++;
                        continue;
                    }

                    if (!area[cell])
                    {
                        kept++;
                        continue;
                    }

                    area[cell] = false;
                    removed++;
                }
            }

            AreaCellsDto dto = MapHelper.GetAreaCells(map, area);
            dto.Removed = removed;
            dto.Already = kept;
            dto.Outside = outside;
            return ApiResult<AreaCellsDto>.Ok(dto);
        }

        /// <summary>The dialog's Delete button: Area.Delete on an allowed area.</summary>
        public static ApiResult DeleteArea(Map map, AreaChangeRequestDto request)
        {
            if (request == null)
            {
                return ApiResult.Fail("Request cannot be null.");
            }

            Area area = MapHelper.FindArea(map, request.Label, out string areaError);
            if (area == null)
            {
                return ApiResult.Fail(areaError);
            }

            string mutableError = MutableRefusal(area, "deleted");
            if (mutableError != null)
            {
                return ApiResult.Fail(mutableError);
            }

            int id = area.ID;
            string label = area.Label;
            string type = area.GetType().Name;
            int cells = area.ActiveCells.Count();
            area.Delete();
            LogApi.Info($"Deleted area {id} ('{label}', {type}) with {cells} cells");

            return ApiResult.Ok(
                new
                {
                    AreaId = id,
                    Label = label,
                    Type = type,
                    CellsCount = cells,
                }
            );
        }

        // The two area methods the dialog only offers on the areas it creates:
        // Home, Build roof and the other game areas have no Rename, Invert or
        // Delete button, so a route that offered them would do more than a
        // player can.
        private static string MutableRefusal(Area area, string action)
        {
            if (area.Mutable)
            {
                return null;
            }

            return $"\"{area.Label}\" ({area.GetType().Name}) is one of the game's own areas: "
                + "the Manage areas dialog only offers that for the allowed areas it creates, "
                + $"so it cannot be {action} here.";
        }

        private static List<ThingDef> SelectablePlants(Map map, Zone_Growing zone)
        {
            var settables = new List<IPlantToGrowSettable> { zone };
            return PlantUtility
                .ValidPlantTypesForGrowers(settables)
                .Where(def => Command_SetPlantToGrow.IsPlantAvailable(def, map))
                .OrderBy(def => def.label)
                .ThenBy(def => def.defName)
                .ToList();
        }

        // The game's own "NoGrowerCanPlant" dialog, as a warning on the answer:
        // a crop whose Plants requirement nobody in the colony reaches will be
        // sown by nobody, and the player learns that from a popup.
        private static string NoGrowerWarning(Map map, ThingDef plantDef)
        {
            if (plantDef.plant == null || plantDef.plant.sowMinSkill <= 0)
            {
                return null;
            }

            int needed = plantDef.plant.sowMinSkill;
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (
                    !pawn.Downed
                    && pawn.skills != null
                    && pawn.skills.GetSkill(SkillDefOf.Plants).Level >= needed
                    && pawn.workSettings != null
                    && pawn.workSettings.WorkIsActive(WorkTypeDefOf.Growing)
                )
                {
                    return null;
                }
            }

            if (
                ModsConfig.BiotechActive
                && MechanitorUtility.AnyPlayerMechCanDoWork(WorkTypeDefOf.Growing, needed, out _)
            )
            {
                return null;
            }

            return $"Nobody in the colony can plant {plantDef.label} yet: it needs Plants {needed} "
                + "and the Growing work switched on, which is the game's own warning when a player picks it.";
        }

        private static Zone_Growing FindGrowingZone(Map map, int zoneId, out string error)
        {
            Zone zone = map.zoneManager.AllZones.FirstOrDefault(z => z.ID == zoneId);
            if (zone == null)
            {
                error =
                    $"No zone on map {map.uniqueID} has id {zoneId}. "
                    + "GET /api/v1/map/zones lists the zones and their ids.";
                return null;
            }

            Zone_Growing growing = zone as Zone_Growing;
            if (growing == null)
            {
                error =
                    $"Zone {zoneId} (\"{zone.label}\") is a {zone.GetType().Name}, not a growing zone.";
                return null;
            }

            error = null;
            return growing;
        }
    }
}
