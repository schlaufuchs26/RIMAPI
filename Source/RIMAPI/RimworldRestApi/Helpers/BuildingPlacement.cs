using RIMAPI.Core;
using RIMAPI.Models.Map;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The two halves of moving a building (fox ticket #4316, parity wave 7):
    /// Designator_Uninstall marks a minifiable building for uninstalling, and
    /// Designator_Install puts a minified one back down as an install blueprint.
    /// Both are the game's own calls; the refusals are the designators' own
    /// checks, so a building the UI refuses to uninstall is refused here too.
    /// </summary>
    public static class BuildingPlacement
    {
        /// <summary>
        /// Designator_Uninstall.DesignateThing: a minifiable building of the
        /// player's faction (or one the colony can claim) takes the Uninstall
        /// designation; a building with no work at all comes down at once,
        /// which is what the game does when its WorkToBuild is 0 or it is a
        /// frame.
        /// </summary>
        public static ApiResult<UninstallResultDto> Uninstall(Building building)
        {
            if (building == null)
            {
                return ApiResult<UninstallResultDto>.Fail(
                    "The id names no built building on the map."
                );
            }

            Map map = building.Map;
            string defName = building.def.defName;
            string label = building.LabelShort;

            string refusal = UninstallRefusal(building, map);
            if (refusal != null)
            {
                return ApiResult<UninstallResultDto>.Fail(refusal);
            }

            var result = new UninstallResultDto
            {
                BuildingId = building.thingIDNumber,
                DefName = defName,
                Label = label,
            };

            bool instant =
                building.GetStatValue(StatDefOf.WorkToBuild) == 0f || building.def.IsFrame;
            if (instant)
            {
                building.Uninstall();
                result.Mode = "uninstalled";
                result.Note =
                    "The game uninstalls this building at once (no work to build), so it is now a minified item where it stood.";
                return ApiResult<UninstallResultDto>.Ok(result);
            }

            map.designationManager.AddDesignation(
                new Designation(building, DesignationDefOf.Uninstall)
            );
            result.Mode = "designated";
            result.Note = "A constructor takes it down; the item stays where the building stood.";
            return ApiResult<UninstallResultDto>.Ok(result);
        }

        /// <summary>
        /// The designator's own gate, in its own order, so the refusal names
        /// what stops the uninstall instead of a bare failure.
        /// </summary>
        public static string UninstallRefusal(Building building, Map map)
        {
            if (building.def.category != ThingCategory.Building)
            {
                return $"{building.LabelShort} is not a building.";
            }

            if (!building.def.Minifiable)
            {
                return $"The game cannot uninstall {building.LabelShort} ({building.def.defName}): the def is not minifiable, and only minifiable buildings come apart.";
            }

            if (building.Faction != Faction.OfPlayer && !building.def.building.alwaysUninstallable)
            {
                if (building.Faction != null)
                {
                    return $"The game does not uninstall {building.LabelShort}: it belongs to {building.Faction.Name}, not to the colony.";
                }

                if (!building.ClaimableBy(Faction.OfPlayer))
                {
                    return $"The game does not uninstall {building.LabelShort}: the colony cannot claim it.";
                }
            }

            if (map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null)
            {
                return $"{building.LabelShort} is already marked for uninstalling.";
            }

            if (map.designationManager.DesignationOn(building, DesignationDefOf.Deconstruct) != null)
            {
                return $"{building.LabelShort} is marked for deconstruction; the game refuses an uninstall on it.";
            }

            return null;
        }

        /// <summary>
        /// Designator_Install.DesignateSingleCell for a minified item: the game's
        /// placement check, then the install blueprint that a constructor
        /// builds back into the building.
        /// </summary>
        public static ApiResult<InstallResultDto> Install(
            MinifiedThing minified,
            Map map,
            IntVec3 cell,
            int? rotation
        )
        {
            if (minified == null)
            {
                return ApiResult<InstallResultDto>.Fail(
                    "The id names no minified item; uninstall a building first, which leaves the item it becomes."
                );
            }

            Thing inner = minified.InnerThing;
            if (inner == null)
            {
                return ApiResult<InstallResultDto>.Fail(
                    $"{minified.LabelShort} holds no building to install."
                );
            }

            ThingDef def = inner.def;
            Rot4 rot = def.rotatable && rotation.HasValue ? new Rot4(rotation.Value) : Rot4.North;

            if (!cell.InBounds(map))
            {
                return ApiResult<InstallResultDto>.Fail(
                    $"The cell ({cell.x}, {cell.z}) is outside map {map.uniqueID}."
                );
            }

            AcceptanceReport report = GenConstruct.CanPlaceBlueprintAt(
                def,
                cell,
                rot,
                map,
                godMode: false,
                thingToIgnore: minified,
                thing: inner
            );
            if (!report.Accepted)
            {
                return ApiResult<InstallResultDto>.Fail(
                    $"The game does not let {def.label} go there: {report.Reason ?? "the placement check refused the cell"}."
                );
            }

            InstallBlueprintUtility.CancelBlueprintsFor(minified);
            Blueprint_Install blueprint = GenConstruct.PlaceBlueprintForInstall(
                minified,
                cell,
                map,
                rot,
                Faction.OfPlayer
            );

            return ApiResult<InstallResultDto>.Ok(
                new InstallResultDto
                {
                    BlueprintId = blueprint.thingIDNumber,
                    ThingId = minified.thingIDNumber,
                    DefName = def.defName,
                    Label = def.label,
                    X = cell.x,
                    Z = cell.z,
                    Rotation = rot.AsInt,
                    Note =
                        "A constructor installs it; the item leaves the stockpile when the work starts.",
                }
            );
        }
    }
}
