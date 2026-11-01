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
                    // The setter only dirties the room's role; the prison-cell
                    // flag the game's bed lookup reads follows the room's
                    // shape, so the room has to be told (#3742).
                    BedPick.RefreshAfterBedTypeChange(bed);
                }
                return ApiResult<BedDto>.Ok(BuildingHelper.BedDtoOf(bed));
            }
            catch (Exception ex)
            {
                return ApiResult<BedDto>.Fail(ex.ToString());
            }
        }

        // A cooler's and a heater's target temperature (fox ticket #3744): the
        // setting lives on the building's own tab, and nothing in the tool read
        // or set it. Colony 15 built a powered cooler into a walled, roofed
        // room and the room stayed at 16 C, the PassiveCooler's floor, because
        // the machine still stood at its default 21 C. A call without a target
        // is the read; with one it sets the setting and answers what the game
        // holds afterwards, so a value the game drops reads as such.
        public ApiResult<TemperatureTargetDto> SetTemperatureTarget(int buildingId, float? target)
        {
            try
            {
                Building building = BuildingHelper.FindBuildingByID(buildingId);
                if (building == null)
                {
                    return ApiResult<TemperatureTargetDto>.Fail(
                        ThingHolderHelper.DescribeUnbuilt(
                            buildingId,
                            "only a built building on the map has a thermostat"
                        ) ?? $"Building not found: {buildingId}"
                    );
                }
                CompTempControl comp = building.TryGetComp<CompTempControl>();
                if (comp == null)
                {
                    return ApiResult<TemperatureTargetDto>.Fail(
                        $"{building.Label} (id {buildingId}) has no thermostat: a heater and a "
                            + $"cooler have one, a {building.def?.defName} does not"
                    );
                }

                float previous = comp.targetTemperature;
                if (target.HasValue)
                {
                    string problem = TemperatureTargetRules.Problem(target.Value);
                    if (problem != null)
                    {
                        return ApiResult<TemperatureTargetDto>.Fail(problem);
                    }
                    comp.targetTemperature = target.Value;
                }

                TemperatureTargetDto dto = Thermostat.Describe(building);
                dto.PreviousTarget = previous;
                if (target.HasValue && !TemperatureTargetRules.Took(target.Value, dto.Target))
                {
                    return ApiResult<TemperatureTargetDto>.Fail(
                        $"{building.Label} (id {buildingId}) did not take {target.Value} C: it reads "
                            + $"{dto.Target} C, so the game reset the setting"
                    );
                }
                return ApiResult<TemperatureTargetDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return ApiResult<TemperatureTargetDto>.Fail(ex.ToString());
            }
        }
    }
}
