using System;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    public class BuildingService : IBuildingService
    {
        public BuildingService() { }

        public ApiResult<BuildingDto> GetBuildingInfo(int buildingId)
        {
            Building building = BuildingHelper.FindBuildingByID(buildingId);
            BuildingDto result = null;

            if (building == null)
            {
                // An id from the stored read can name a stored or a held thing
                // instead of a built building; name it rather than a bare "not
                // found" (tickets #3200, #3295).
                return ApiResult<BuildingDto>.Fail(
                    ThingHolderHelper.DescribeUnbuilt(
                        buildingId,
                        "only a built building on the map can be read"
                    ) ?? $"Building not found: {buildingId}"
                );
            }

            // Turret Info
            if (building is Building_Turret)
            {
                result = BuildingHelper.GetTurretInfo(building);
            }

            // Generator Info
            if (building.TryGetComp<CompPowerPlant>() != null)
            {
                result = BuildingHelper.GetPowerGeneratorInfo(building);
            }

            // TODO: Add other buildings info

            result = BuildingHelper.BuildingToDto(building);
            return ApiResult<BuildingDto>.Ok(result);
        }

        public ApiResult SetBuildingPower(int buildingId, bool powerOn)
        {
            Building building = BuildingHelper.FindBuildingByID(buildingId);
            if (building == null)
            {
                // An id from the stored read can name a thing that is not a built
                // building on the map at all: a container holds it, or it is a
                // stored building that has to be installed first. Say which
                // instead of a bare "not found" (tickets #3200, #3235).
                return ApiResult.Fail(
                    ThingHolderHelper.DescribeUnbuilt(
                        buildingId,
                        "only a built building on the map has a power switch"
                    ) ?? $"Building not found: {buildingId}"
                );
            }

            bool success = BuildingHelper.SetBuildingPower(building, powerOn);
            if (!success)
            {
                return ApiResult.Fail($"Building {buildingId} has no flickable power component");
            }
            return ApiResult.Ok();
        }

        // A bed set to prisoners is what an arrest or a capture needs to land
        // (ticket #3183): the game's own job looks the bed up by that flag, and
        // nothing in the tool could set it, so a run could only watch a downed
        // raider lie there. This is the bed's own "Set for prisoners" toggle,
        // nothing else: the game clears a medical or slave setting itself when
        // the flag goes on, and the answer reports the state afterwards.
        public ApiResult<BedDto> SetBedForPrisoners(int buildingId, bool forPrisoners)
        {
            try
            {
                Building building = BuildingHelper.FindBuildingByID(buildingId);
                if (building == null)
                {
                    // A bed taken apart and stored keeps its id in the stored
                    // read; name it instead of a bare "not found" (#3295).
                    return ApiResult<BedDto>.Fail(
                        ThingHolderHelper.DescribeUnbuilt(
                            buildingId,
                            "only a built bed on the map takes a prisoner"
                        ) ?? $"Building not found: {buildingId}"
                    );
                }
                Building_Bed bed = building as Building_Bed;
                if (bed == null)
                {
                    return ApiResult<BedDto>.Fail(
                        $"{building.Label} (id {buildingId}) is not a bed: it is a {building.def.defName}");
                }
                if (bed.def?.building == null || !bed.def.building.bed_humanlike)
                {
                    return ApiResult<BedDto>.Fail(
                        $"{bed.Label} (id {buildingId}) is an animal bed: only a humanlike bed takes a prisoner");
                }
                if (bed.ForPrisoners != forPrisoners)
                {
                    bed.ForPrisoners = forPrisoners;
                }
                return ApiResult<BedDto>.Ok(BuildingHelper.BedDtoOf(bed));
            }
            catch (Exception ex)
            {
                return ApiResult<BedDto>.Fail(ex.ToString());
            }
        }
    }
}
