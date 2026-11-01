using System;
using System.Linq;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    public static class BuildingHelper
    {
        public static BuildingDto BuildingToDto(Building building)
        {
            return new BuildingDto
            {
                Id = building.thingIDNumber,
                Def = building.def.defName,
                Label = building.Label,
                Position = new PositionDto
                {
                    X = building.Position.x,
                    Y = building.Position.y,
                    Z = building.Position.z,
                },
                Type = building.GetType().Name,
                Bed = BedDtoOf(building),
            };
        }

        // BedDtoOf describes a humanlike bed's settings and answers null for
        // every other building, so a read can mark the beds that take a
        // prisoner (ticket #3183).
        public static BedDto BedDtoOf(Building building)
        {
            Building_Bed bed = building as Building_Bed;
            if (bed == null || bed.def?.building == null || !bed.def.building.bed_humanlike) return null;
            return new BedDto
            {
                ForPrisoners = bed.ForPrisoners,
                ForSlaves = bed.ForSlaves,
                Medical = bed.Medical,
                Owner = bed.OwnersForReading?.FirstOrDefault()?.LabelShort,
                Cell = BedCellOf(bed),
            };
        }

        // BedCellOf is the room the bed stands in, as a prison cell (fox ticket
        // #3743): the flag the game's bed lookup reads, the room's role label
        // and its cell count, and why the game would refuse the room. A bed
        // order that answers only "set for prisoners" leaves a run believing
        // the cell is ready, so the room's refusal has to ride along.
        public static BedCellDto BedCellOf(Building_Bed bed)
        {
            Map map = bed.Map;
            if (map == null) return null;
            Room room = BedPick.BedRoom(map, bed);
            CellRefusal refusal = bed.ForPrisoners ? BedPick.CellRefusalOf(map, bed) : null;
            return new BedCellDto
            {
                IsPrisonCell = room != null && room.IsPrisonCell,
                RoleLabel = BedPick.RoomLabel(room),
                CellsCount = room != null ? room.CellCount : 0,
                NoCellReason = refusal?.Reason ?? "",
                NoCellCause = refusal?.Cause ?? "",
            };
        }

        public static TurretInfoDto GetTurretInfo(Building building)
        {
            try
            {
                var turretProps = building.def.building;
                var turretGunDef = turretProps?.turretGunDef;

                // Get turret-specific components
                var compAmmo = building.TryGetComp<CompChangeableProjectile>();
                var compRefuelable = building.TryGetComp<CompRefuelable>();
                var compPower = building.TryGetComp<CompPowerTrader>();
                var compMannable = building.TryGetComp<CompMannable>();

                var baseDto = BuildingToDto(building);
                TurretInfoDto turretInfo = new TurretInfoDto
                {
                    Id = baseDto.Id,
                    Def = baseDto.Def,
                    Label = baseDto.Label,
                    Position = baseDto.Position,
                    Type = baseDto.Type,

                    // Basic turret properties
                    TurretGunDef = turretGunDef?.defName,
                    IsMortar = turretProps?.IsMortar ?? false,
                    IsManned = compMannable?.MannedNow ?? false,

                    // Combat statistics
                    BurstCooldownTime = turretProps?.turretBurstCooldownTime ?? -1f,
                    InitialCooldownTime = turretProps?.turretInitialCooldownTime ?? 0f,
                    CombatPower = turretProps?.combatPower ?? 0f,

                    // Ammo information
                    CurrentAmmo = compAmmo?.LoadedShell?.defName,

                    // Power requirements
                    RequiresPower = compPower != null,
                    PowerConsumption = compPower?.Props.PowerConsumption ?? 0f,
                    IsPowered = compPower?.PowerOn ?? true,

                    // Fuel requirements
                    RequiresFuel = compRefuelable != null,
                    CurrentFuel = compRefuelable?.Fuel ?? 0f,
                    FuelCapacity = compRefuelable?.Props.fuelCapacity ?? 0f,
                    FuelType = compRefuelable?.Props.fuelFilter?.AnyAllowedDef?.defName,

                    // Targeting information
                    CanTargetAcquired = turretProps?.playTargetAcquiredSound ?? false,
                    IsCombatDangerous = turretProps?.ai_combatDangerous ?? false,

                    // Weapon stats from the gun definition
                    WeaponStats = GetWeaponStats(turretGunDef),

                    // Current state
                    Health = building.HitPoints,
                    MaxHealth = building.MaxHitPoints,
                    IsWorking = building.IsWorking(),
                };

                return turretInfo;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting turret info: {ex.Message}", ex);
            }
        }

        public static PowerGeneratorInfoDto GetPowerGeneratorInfo(Building building)
        {
            try
            {
                CompPowerPlant powerPlant = building.TryGetComp<CompPowerPlant>();

                var baseDto = BuildingToDto(building);
                PowerGeneratorInfoDto info = new PowerGeneratorInfoDto
                {
                    Id = baseDto.Id,
                    Def = baseDto.Def,
                    Label = baseDto.Label,
                    Position = baseDto.Position,
                    Type = baseDto.Type,
                    PowerOutput = powerPlant.PowerOutput,
                    PowerOn = powerPlant.PowerOn,
                    TransmitsPower = powerPlant.Props.transmitsPower,
                    ShortCircuitInRain = powerPlant.Props.shortCircuitInRain,
                    IdlePowerDraw = powerPlant.Props.idlePowerDraw,
                };

                return info;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting power generator info: {ex.Message}", ex);
            }
        }

        public static WeaponStatsDto GetWeaponStats(ThingDef gunDef)
        {
            if (gunDef == null)
                return null;

            var verbProps = gunDef.Verbs?.FirstOrDefault();
            if (verbProps == null)
                return null;

            return new WeaponStatsDto
            {
                DamageDef = verbProps.defaultProjectile?.projectile?.damageDef?.defName,
                DamageAmount =
                    verbProps.defaultProjectile?.projectile?.GetDamageAmount(1f, null) ?? 0,
                ArmorPenetration =
                    verbProps.defaultProjectile?.projectile?.GetArmorPenetration() ?? 0f,
                ExplosionRadius = verbProps.defaultProjectile?.projectile?.explosionRadius ?? 0f,
                Range = verbProps.range,
                WarmupTime = verbProps.warmupTime,
                CooldownTime = verbProps.defaultCooldownTime,
                BurstShotCount = verbProps.burstShotCount,
                Accuracy = verbProps.accuracyTouch,
            };
        }

        public static bool SetBuildingPower(Building building, bool powerOn)
        {
            // Most power-consuming buildings use CompFlickable for on/off toggle
            var flickable = building.TryGetComp<CompFlickable>();
            if (flickable != null)
            {
                flickable.SwitchIsOn = powerOn;
                return true;
            }
            return false;
        }

        public static Building FindBuildingByID(int buildingId)
        {
            foreach (Map map in Find.Maps)
            {
                Building building = map.listerBuildings.allBuildingsColonist.FirstOrDefault(b =>
                    b.thingIDNumber == buildingId
                );
                if (building != null)
                    return building;
            }
            return null;
        }
    }
}
