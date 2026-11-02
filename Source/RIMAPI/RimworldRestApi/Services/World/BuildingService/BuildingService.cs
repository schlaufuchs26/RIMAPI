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
    public class BuildingService : IBuildingService
    {
        public BuildingService() { }

        public ApiResult<BuildingDto> GetBuildingInfo(int buildingId)
        {
            Building building = BuildingHelper.FindBuildingByID(buildingId);

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

            // A turret and a power plant carry state the plain DTO has no field
            // for, so the specialised answer is the answer: building the plain
            // DTO afterwards used to overwrite it, and a caller read a machine
            // as if it had no power at all (fox ticket #3705).
            switch (
                BuildingInfoRules.KindOf(
                    building is Building_Turret,
                    building.TryGetComp<CompPowerPlant>() != null
                )
            )
            {
                case BuildingInfoKind.Turret:
                    return ApiResult<BuildingDto>.Ok(BuildingHelper.GetTurretInfo(building));
                case BuildingInfoKind.PowerGenerator:
                    return ApiResult<BuildingDto>.Ok(
                        BuildingHelper.GetPowerGeneratorInfo(building)
                    );
                default:
                    return ApiResult<BuildingDto>.Ok(BuildingHelper.BuildingToDto(building));
            }
        }

        // A building's power switch (stock route, fox ticket #3898): the answer
        // carries the state the game holds afterwards, because "the call
        // succeeded" is not "the machine now runs". A heater switched off and a
        // heater on a dead net both read powered=false, and only the first is
        // fixed by this call.
        public ApiResult<PoweredBuildingDto> SetBuildingPower(int buildingId, bool powerOn)
        {
            Building building = BuildingHelper.FindBuildingByID(buildingId);
            if (building == null)
            {
                // An id from the stored read can name a thing that is not a built
                // building on the map at all: a container holds it, or it is a
                // stored building that has to be installed first. Say which
                // instead of a bare "not found" (tickets #3200, #3235).
                return ApiResult<PoweredBuildingDto>.Fail(
                    ThingHolderHelper.DescribeUnbuilt(
                        buildingId,
                        "only a built building on the map has a power switch"
                    ) ?? $"Building not found: {buildingId}"
                );
            }

            bool success = BuildingHelper.SetBuildingPower(building, powerOn);
            if (!success)
            {
                return ApiResult<PoweredBuildingDto>.Fail(
                    $"{building.Label} (id {buildingId}) has no flickable power component: "
                        + "only a machine with a switch of its own can be flipped"
                );
            }
            return ApiResult<PoweredBuildingDto>.Ok(PowerGrid.Describe(building));
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

        // A bed's owner is the game's own bed assignment (fox ticket #3771):
        // colony 16 had three free bedroom beds while their intended owners
        // slept in the barracks, and the only way out was to deconstruct the
        // barracks beds. This is the bed tab's "Assign" choice: a colonist
        // takes a free humanlike bed and the game moves him off the bed he had,
        // and pawnId 0 clears the owner, which is how a colonist comes off a
        // bed without taking the bed apart.
        public ApiResult<BedOwnerDto> SetBedOwner(int buildingId, int pawnId)
        {
            try
            {
                Building building = BuildingHelper.FindBuildingByID(buildingId);
                if (building == null)
                {
                    return ApiResult<BedOwnerDto>.Fail(
                        ThingHolderHelper.DescribeUnbuilt(
                            buildingId,
                            "only a built bed on the map takes an owner"
                        ) ?? $"Building not found: {buildingId}"
                    );
                }
                Building_Bed bed = building as Building_Bed;
                if (bed == null)
                {
                    return ApiResult<BedOwnerDto>.Fail(
                        $"{building.Label} (id {buildingId}) is not a bed: it is a {building.def.defName}");
                }
                if (pawnId <= 0) return ClearBedOwner(bed);

                Pawn pawn = PawnHelper.FindPawnById(pawnId);
                if (pawn == null)
                {
                    return ApiResult<BedOwnerDto>.Fail($"Pawn with ID={pawnId} not found");
                }

                Building_Bed previous = pawn.ownership != null ? pawn.ownership.OwnedBed : null;
                List<Pawn> owners = bed.OwnersForReading != null
                    ? new List<Pawn>(bed.OwnersForReading)
                    : new List<Pawn>();
                BedOwnerRules.State state = new BedOwnerRules.State
                {
                    BedIsHumanlike = bed.def?.building != null && bed.def.building.bed_humanlike,
                    BedForPrisoners = bed.ForPrisoners,
                    BedForSlaves = bed.ForSlaves,
                    Owners = owners
                        .Select(o => new BedOwnerRules.Owner { Name = o.LabelShort, Id = o.thingIDNumber })
                        .ToList(),
                    Capacity = BuildingHelper.BedCapacity(bed),
                    Pawn = pawn.LabelShort,
                    PawnId = pawn.thingIDNumber,
                    PawnIsColonist = pawn.IsColonist,
                    PawnKind = PawnKindOf(pawn),
                };
                string problem = BedOwnerRules.Problem(state);
                if (problem != null)
                {
                    return ApiResult<BedOwnerDto>.Fail($"{bed.Label} (id {buildingId}): {problem}");
                }

                bool changed = !state.Holds(pawn.thingIDNumber);
                if (changed)
                {
                    // The bed tab's own path: it assigns the pawn and the game
                    // moves him off the bed he owned before.
                    bed.CompAssignableToPawn.TryAssignPawn(pawn);
                }

                BedDto after = BuildingHelper.BedDtoOf(bed);
                bool holds = after != null && after.Owners != null
                    && after.Owners.Any(o => o.Id == pawn.thingIDNumber);
                if (!holds)
                {
                    // Say what the game reads instead of an acknowledgement: a
                    // bed that refuses its owner is the whole reason this call
                    // exists.
                    string reads = after == null || after.Owners == null || after.Owners.Count == 0
                        ? "no owner"
                        : "owner " + string.Join(", ", after.Owners.Select(o => $"{o.Name} (id {o.Id})"));
                    return ApiResult<BedOwnerDto>.Fail(
                        $"{bed.Label} (id {buildingId}) did not take {pawn.LabelShort} as its owner: it reads {reads}");
                }
                bool moved = previous != null && previous != bed;
                return ApiResult<BedOwnerDto>.Ok(
                    new BedOwnerDto
                    {
                        Bed = after,
                        PawnId = pawn.thingIDNumber,
                        Pawn = pawn.LabelShort,
                        PreviousBedId = moved ? previous.thingIDNumber : 0,
                        PreviousBed = moved ? previous.LabelShort : "",
                        Changed = changed,
                    }
                );
            }
            catch (Exception ex)
            {
                return ApiResult<BedOwnerDto>.Fail(ex.ToString());
            }
        }

        // ClearBedOwner takes every owner off a bed (a double bed holds two) and
        // answers what the game reads afterwards.
        private static ApiResult<BedOwnerDto> ClearBedOwner(Building_Bed bed)
        {
            List<Pawn> owners = bed.OwnersForReading != null
                ? new List<Pawn>(bed.OwnersForReading)
                : new List<Pawn>();
            foreach (Pawn owner in owners)
            {
                bed.CompAssignableToPawn.TryUnassignPawn(owner);
            }

            BedDto after = BuildingHelper.BedDtoOf(bed);
            if (after != null && after.Owners != null && after.Owners.Count > 0)
            {
                return ApiResult<BedOwnerDto>.Fail(
                    $"{bed.Label} (id {bed.thingIDNumber}) still belongs to " +
                    string.Join(", ", after.Owners.Select(o => $"{o.Name} (id {o.Id})")) +
                    ": the game did not clear it");
            }
            return ApiResult<BedOwnerDto>.Ok(
                new BedOwnerDto
                {
                    Bed = after,
                    PawnId = owners.Count > 0 ? owners[0].thingIDNumber : 0,
                    Pawn = owners.Count > 0 ? owners[0].LabelShort : "",
                    Changed = owners.Count > 0,
                    Cleared = owners
                        .Select(p => new BedOwnerSlotDto { Name = p.LabelShort, Id = p.thingIDNumber })
                        .ToList(),
                }
            );
        }

        // PawnKindOf says what a pawn is instead of a colonist, for the refusal
        // that keeps a prisoner, a guest or an animal out of a colonist's bed.
        private static string PawnKindOf(Pawn pawn)
        {
            if (pawn.RaceProps != null && pawn.RaceProps.Animal) return "an animal";
            if (pawn.IsPrisoner) return "a prisoner";
            if (pawn.IsSlave) return "a slave";
            if (pawn.IsWildMan()) return "a wild man";
            if (pawn.guest != null && pawn.guest.HostFaction == Faction.OfPlayer) return "a guest";
            return "not a colonist";
        }

        // A bed's own medical setting (fox ticket #4235): the bed tab's
        // "Medical" gizmo, the one flag that makes a bed a hospital bed. The
        // building read answered it as BedDto.Medical and nothing set it, so a
        // run that found the hospital full of normal beds could not make one.
        // The medicine policy beside it belongs to the pawn
        // (playerSettings.medCare, POST /api/v1/pawn/medicine-policy): the game
        // holds no per-bed medicine policy, so this flag is the whole of a
        // bed's own medical setting.
        public ApiResult<BedMedicalDto> SetBedMedical(int buildingId, bool? medical)
        {
            try
            {
                Building building = BuildingHelper.FindBuildingByID(buildingId);
                if (building == null)
                {
                    return ApiResult<BedMedicalDto>.Fail(
                        ThingHolderHelper.DescribeUnbuilt(
                            buildingId,
                            "only a built bed on the map has a medical setting"
                        ) ?? $"Building not found: {buildingId}"
                    );
                }
                Building_Bed bed = building as Building_Bed;
                if (bed == null)
                {
                    return ApiResult<BedMedicalDto>.Fail(
                        BedMedicalRules.NotABed(
                            building.Label,
                            buildingId,
                            building.def?.defName ?? "building"
                        )
                    );
                }

                bool canBeMedical = bed.def?.building != null && bed.def.building.bed_canBeMedical;
                bool previous = bed.Medical;
                if (medical.HasValue)
                {
                    if (!BedMedicalRules.CanTake(canBeMedical, medical.Value))
                    {
                        return ApiResult<BedMedicalDto>.Fail(
                            BedMedicalRules.NoMedicalSetting(
                                bed.Label,
                                buildingId,
                                bed.def?.defName ?? "bed"
                            )
                        );
                    }
                    // The bed tab's own toggle.
                    bed.Medical = medical.Value;
                }

                BedMedicalDto dto = new BedMedicalDto
                {
                    Id = bed.thingIDNumber,
                    Label = bed.Label,
                    DefName = bed.def?.defName,
                    CanBeMedical = canBeMedical,
                    Medical = bed.Medical,
                    PreviousMedical = previous,
                    Changed = bed.Medical != previous,
                };
                if (medical.HasValue && !BedMedicalRules.Took(medical.Value, dto.Medical))
                {
                    return ApiResult<BedMedicalDto>.Fail(
                        BedMedicalRules.DidNotTake(
                            bed.Label,
                            buildingId,
                            medical.Value,
                            dto.Medical
                        )
                    );
                }
                return ApiResult<BedMedicalDto>.Ok(dto);
            }
            catch (Exception ex)
            {
                return ApiResult<BedMedicalDto>.Fail(ex.ToString());
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
