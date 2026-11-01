using System;
using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    [ModelDescription("Standard API response wrapper")]
    public class ApiResponse<T>
    {
        [ParameterDescription("Indicates if the request was successful")]
        public bool Success { get; set; }

        [ParameterDescription("The actual response data")]
        public T Data { get; set; }

        [ParameterDescription("Any warnings that occurred during processing")]
        public string[] Warnings { get; set; } = Array.Empty<string>();

        [ParameterDescription("Error message if success is false")]
        public string Error { get; set; }
    }

    public class BuildingDto
    {
        public int Id { get; set; }
        public string Def { get; set; }
        public string Label { get; set; }
        public PositionDto Position { get; set; }
        public int Rotation { get; set; }
        public PositionDto Size { get; set; }
        public string Type { get; set; }

        // Bed carries the settings of a humanlike bed and is null for every
        // other building (ticket #3183): without it a caller cannot tell a
        // prisoner bed from a colonist bed, which is the difference between an
        // arrest that lands and one that is refused for "no prisoner bed".
        public BedDto Bed { get; set; }

        // Grave carries a casket's assignment and contents and is null for
        // every other building (fox ticket #3784). A grave built by `build`
        // comes up unassigned, but the game also lets a grave be assigned to
        // one colonist, and an assigned grave takes only that colonist's
        // corpse: three such graves stood empty while a dead colonist stayed
        // unburied, and no read said which grave was whose.
        public GraveDto Grave { get; set; }
    }

    public class GraveDto
    {
        // AssignedPawn is the colonist the grave belongs to, empty when the
        // grave takes any corpse.
        public string AssignedPawn { get; set; }
        public int AssignedPawnId { get; set; }

        // Holds is the corpse inside, with the game's own "(dead)" suffix, and
        // HoldsPawn the dead pawn's name on its own. Empty while the grave is
        // free.
        public string Holds { get; set; }
        public string HoldsPawn { get; set; }

        // Filter is the grave's storage filter as the game summarises it
        // ("corpses"). An assigned grave ignores it, and the refusal names it
        // when the filter is what said no.
        public string Filter { get; set; }
    }

    public class BedDto
    {
        public bool ForPrisoners { get; set; }
        public bool ForSlaves { get; set; }
        public bool Medical { get; set; }
        public string Owner { get; set; }

        // OwnerId is that owner's pawn id, 0 for a free bed (fox ticket #3771):
        // a name alone cannot be handed back to `set`, which names a pawn by id
        // or by name and needs the id to tell two colonists apart.
        public int OwnerId { get; set; }

        // Cell is the room the bed stands in, as a prison cell (fox ticket
        // #3743). The toggle alone answers "set for prisoners" while the game
        // still refuses the room, so a run learned about a bed at the map edge
        // or in an open room only when an arrest refused later. Null when the
        // bed is on no map.
        public BedCellDto Cell { get; set; }
    }

    // BedOwnerDto is what one bed assignment answers (fox ticket #3771): the
    // bed as it stands afterwards, the pawn the caller named, and the bed that
    // pawn left. Without the previous bed a run cannot tell an assignment that
    // moved a colonist out of the barracks from one that only added a second
    // bed to him.
    public class BedOwnerDto
    {
        public BedDto Bed { get; set; }
        public int PawnId { get; set; }
        public string Pawn { get; set; }

        // PreviousBed is the bed the pawn owned before this call, empty when he
        // owned none or already owned this one.
        public int PreviousBedId { get; set; }
        public string PreviousBed { get; set; }

        // Changed is false when the bed already read what the call asked for.
        public bool Changed { get; set; }
    }

    public class BedCellDto
    {
        // IsPrisonCell is the game's own flag (Room.IsPrisonCell), the one its
        // bed lookup reads.
        public bool IsPrisonCell { get; set; }
        public string RoleLabel { get; set; }
        public int CellsCount { get; set; }

        // NoCellReason says why the game would refuse this room as a cell,
        // empty when it would not. Filled only for a bed set to prisoners: a
        // colonist bed is not meant to be one.
        public string NoCellReason { get; set; }

        // NoCellCause is the same refusal as a label ("map edge", "too large",
        // "not enclosed", "no room"), for the one-line marker next to a bed id
        // in the things list (fox ticket #3750). The sentence is 80 characters
        // and makes that list unreadable.
        public string NoCellCause { get; set; }
    }

    public class PowerGeneratorInfoDto : BuildingDto
    {
        public float PowerOutput { get; set; }
        public bool PowerOn { get; set; }
        public bool TransmitsPower { get; set; }
        public bool ShortCircuitInRain { get; set; }
        public float IdlePowerDraw { get; set; }
    }

    public class TurretInfoDto : BuildingDto
    {
        // Turret Properties
        public string TurretGunDef { get; set; }
        public bool IsMortar { get; set; }
        public bool IsManned { get; set; }

        // Combat Stats
        public FloatRange BurstWarmupTime { get; set; }
        public float BurstCooldownTime { get; set; }
        public float InitialCooldownTime { get; set; }
        public float CombatPower { get; set; }

        // Ammo
        public string CurrentAmmo { get; set; }
        public List<string> AvailableAmmoTypes { get; set; }
        public int MaxAmmoCapacity { get; set; }

        // Power
        public bool RequiresPower { get; set; }
        public float PowerConsumption { get; set; }
        public bool IsPowered { get; set; }

        // Fuel
        public bool RequiresFuel { get; set; }
        public float CurrentFuel { get; set; }
        public float FuelCapacity { get; set; }
        public string FuelType { get; set; }

        // Targeting
        public bool CanTargetAcquired { get; set; }
        public bool IsCombatDangerous { get; set; }

        // Weapon Stats
        public WeaponStatsDto WeaponStats { get; set; }

        // State
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public bool IsWorking { get; set; }
    }

    public class WeaponStatsDto
    {
        public string DamageDef { get; set; }
        public int DamageAmount { get; set; }
        public float ArmorPenetration { get; set; }
        public float ExplosionRadius { get; set; }
        public float Range { get; set; }
        public float WarmupTime { get; set; }
        public float CooldownTime { get; set; }
        public int BurstShotCount { get; set; }
        public float Accuracy { get; set; }
    }

    public class FloatRange
    {
        public float Min { get; set; }
        public float Max { get; set; }

        public FloatRange() { }

        public FloatRange(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
