using System;
using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    [ModelDescription("Standard API response wrapper")]
    public class ApiResponse<T>
    {
        [ParameterDescription("False when the game refused the call, true when it ran")]
        public bool Success { get; set; }

        [ParameterDescription("The route's payload, when the answer carries one")]
        public T Data { get; set; }

        [ParameterDescription("What the call did but the caller should know")]
        public string[] Warnings { get; set; } = Array.Empty<string>();

        [ParameterDescription("Why the call failed, one sentence per refusal")]
        public string Error { get; set; }
    }

    public class BuildingDto
    {
        [ParameterDescription("The building's numeric id.")]
        public int Id { get; set; }
        [ParameterDescription("The building's def name.")]
        public string Def { get; set; }
        public string Label { get; set; }
        public PositionDto Position { get; set; }
        [ParameterDescription("The building's rotation: 0, 90, 180 or 270.")]
        public int Rotation { get; set; }
        [ParameterDescription("How many cells the building covers.")]
        public PositionDto Size { get; set; }
        [ParameterDescription("The building's kind, e.g. wall, bed or turret.")]
        public string Type { get; set; }

        // Bed carries the settings of a humanlike bed and is null for every
        // other building (ticket #3183): without it a caller cannot tell a
        // prisoner bed from a colonist bed, which is the difference between an
        // arrest that lands and one that is refused for "no prisoner bed".
        [ParameterDescription("The bed's details, when the building is a bed.")]
        public BedDto Bed { get; set; }

        // Grave carries a casket's assignment and contents and is null for
        // every other building (fox ticket #3784). A grave built by `build`
        // comes up unassigned, but the game also lets a grave be assigned to
        // one colonist, and an assigned grave takes only that colonist's
        // corpse: three such graves stood empty while a dead colonist stayed
        // unburied, and no read said which grave was whose.
        [ParameterDescription("The grave's details, when the building is a grave.")]
        public GraveDto Grave { get; set; }
    }

    public class GraveDto
    {
        // AssignedPawn is the colonist the grave belongs to, empty when the
        // grave takes any corpse.
        [ParameterDescription("The name of the pawn the grave is assigned to.")]
        public string AssignedPawn { get; set; }
        [ParameterDescription("The numeric id of that pawn.")]
        public int AssignedPawnId { get; set; }

        // Holds is the corpse inside, with the game's own "(dead)" suffix, and
        // HoldsPawn the dead pawn's name on its own. Empty while the grave is
        // free.
        [ParameterDescription("What the grave holds, as the game names it.")]
        public string Holds { get; set; }
        [ParameterDescription("Whether a corpse lies in the grave.")]
        public string HoldsPawn { get; set; }

        // Filter is the grave's storage filter as the game summarises it
        // ("corpses"). An assigned grave ignores it, and the refusal names it
        // when the filter is what said no.
        [ParameterDescription("The filter the grave accepts corpses by.")]
        public string Filter { get; set; }
    }

    public class BedDto
    {
        [ParameterDescription("Whether the bed is set aside for prisoners.")]
        public bool ForPrisoners { get; set; }
        [ParameterDescription("Whether the bed is set aside for slaves.")]
        public bool ForSlaves { get; set; }
        [ParameterDescription("Whether the bed is set to medical use.")]
        public bool Medical { get; set; }
        [ParameterDescription("The name of the pawn the bed belongs to.")]
        public string Owner { get; set; }

        // OwnerId is that owner's pawn id, 0 for a free bed (fox ticket #3771):
        // a name alone cannot be handed back to `set`, which names a pawn by id
        // or by name and needs the id to tell two colonists apart.
        [ParameterDescription("The numeric id of that pawn.")]
        public int OwnerId { get; set; }

        // Capacity is how many colonists the bed takes
        // (CompAssignableToPawn.MaxAssignedPawnsCount): 2 for a double bed. A
        // read that prints only Owner makes a half-filled double bed look full
        // or a full one look free (fox ticket #3874).
        [ParameterDescription("How many pawns may sleep in the bed.")]
        public int Capacity { get; set; }

        // Owners is every colonist the bed belongs to, in the game's order.
        // Owner and OwnerId stay the first of them for the reads that print one
        // name.
        [ParameterDescription("The pawns the bed is assigned to.")]
        public List<BedOwnerSlotDto> Owners { get; set; }

        // Cell is the room the bed stands in, as a prison cell (fox ticket
        // #3743). The toggle alone answers "set for prisoners" while the game
        // still refuses the room, so a run learned about a bed at the map edge
        // or in an open room only when an arrest refused later. Null when the
        // bed is on no map.
        [ParameterDescription("Where the bed stands and whether it is reachable.")]
        public BedCellDto Cell { get; set; }
    }

    // BedOwnerDto is what one bed assignment answers (fox ticket #3771): the
    // bed as it stands afterwards, the pawn the caller named, and the bed that
    // pawn left. Without the previous bed a run cannot tell an assignment that
    // moved a colonist out of the barracks from one that only added a second
    // bed to him.
    public class BedOwnerDto
    {
        [ParameterDescription("The bed's numeric id.")]
        public BedDto Bed { get; set; }
        public int PawnId { get; set; }
        [ParameterDescription("The pawn the bed was assigned to.")]
        public string Pawn { get; set; }

        // PreviousBed is the bed the pawn owned before this call, empty when he
        // owned none or already owned this one.
        [ParameterDescription("The numeric id of the bed the pawn had before.")]
        public int PreviousBedId { get; set; }
        [ParameterDescription("The bed the pawn had before.")]
        public string PreviousBed { get; set; }

        // Changed is false when the bed already read what the call asked for.
        [ParameterDescription("Whether the call actually changed the assignment.")]
        public bool Changed { get; set; }

        // Cleared is every colonist a clear call took off the bed, in the
        // game's order: a double bed holds two (fox ticket #3874). Pawn and
        // PawnId stay the first of them for a caller that reads one name.
        [ParameterDescription("Whether the bed lost its owner.")]
        public List<BedOwnerSlotDto> Cleared { get; set; }
    }

    public class BedOwnerSlotDto
    {
        // Name and Id of one colonist a bed belongs to (fox ticket #3874): a
        // double bed names both, and the assignment rule needs the ids to tell
        // a second colonist from the owner already on the bed.
        public string Name { get; set; }
        [ParameterDescription("The bed's numeric id.")]
        public int Id { get; set; }
    }

    public class BedCellDto
    {
        // IsPrisonCell is the game's own flag (Room.IsPrisonCell), the one its
        // bed lookup reads.
        [ParameterDescription("Whether the cell counts as a prison cell.")]
        public bool IsPrisonCell { get; set; }
        [ParameterDescription("The cell's role, as the game names it.")]
        public string RoleLabel { get; set; }
        [ParameterDescription("How many cells the bed's room holds.")]
        public int CellsCount { get; set; }

        // NoCellReason says why the game would refuse this room as a cell,
        // empty when it would not. Filled only for a bed set to prisoners: a
        // colonist bed is not meant to be one.
        [ParameterDescription("Why no cell was chosen, when none was.")]
        public string NoCellReason { get; set; }

        // NoCellCause is the same refusal as a label ("map edge", "too large",
        // "not enclosed", "no room"), for the one-line marker next to a bed id
        // in the things list (fox ticket #3750). The sentence is 80 characters
        // and makes that list unreadable.
        [ParameterDescription("What caused that refusal.")]
        public string NoCellCause { get; set; }
    }

    public class PowerGeneratorInfoDto : BuildingDto
    {
        [ParameterDescription("How much power the generator makes, in watts.")]
        public float PowerOutput { get; set; }
        [ParameterDescription("Whether the generator is running.")]
        public bool PowerOn { get; set; }
        [ParameterDescription("Whether the generator feeds the power grid.")]
        public bool TransmitsPower { get; set; }
        [ParameterDescription("Whether rain can short the generator.")]
        public bool ShortCircuitInRain { get; set; }
        [ParameterDescription("How much power it draws when idle, in watts.")]
        public float IdlePowerDraw { get; set; }
    }

    public class TurretInfoDto : BuildingDto
    {
        // Turret Properties
        [ParameterDescription("The turret gun's def name.")]
        public string TurretGunDef { get; set; }
        [ParameterDescription("Whether the turret is a mortar.")]
        public bool IsMortar { get; set; }
        [ParameterDescription("Whether a pawn is manning the turret.")]
        public bool IsManned { get; set; }

        // Combat Stats
        [ParameterDescription("How long the turret aims before firing, in seconds.")]
        public FloatRange BurstWarmupTime { get; set; }
        [ParameterDescription("How long it waits between bursts, in seconds.")]
        public float BurstCooldownTime { get; set; }
        [ParameterDescription("The cooldown after it is built, in seconds.")]
        public float InitialCooldownTime { get; set; }
        [ParameterDescription("The turret's combat power rating.")]
        public float CombatPower { get; set; }

        // Ammo
        [ParameterDescription("How much ammo the turret holds.")]
        public string CurrentAmmo { get; set; }
        [ParameterDescription("The ammo types the turret can load.")]
        public List<string> AvailableAmmoTypes { get; set; }
        [ParameterDescription("How much ammo the turret can hold.")]
        public int MaxAmmoCapacity { get; set; }

        // Power
        [ParameterDescription("Whether the turret needs power to fire.")]
        public bool RequiresPower { get; set; }
        [ParameterDescription("How much power it draws, in watts.")]
        public float PowerConsumption { get; set; }
        [ParameterDescription("Whether the turret has power.")]
        public bool IsPowered { get; set; }

        // Fuel
        [ParameterDescription("Whether the turret burns fuel.")]
        public bool RequiresFuel { get; set; }
        [ParameterDescription("How much fuel it holds.")]
        public float CurrentFuel { get; set; }
        [ParameterDescription("How much fuel it can hold.")]
        public float FuelCapacity { get; set; }
        [ParameterDescription("The fuel's def name.")]
        public string FuelType { get; set; }

        // Targeting
        [ParameterDescription("Whether the turret has a target.")]
        public bool CanTargetAcquired { get; set; }
        [ParameterDescription("Whether the turret counts as a combat threat.")]
        public bool IsCombatDangerous { get; set; }

        // Weapon Stats
        [ParameterDescription("The turret gun's numbers.")]
        public WeaponStatsDto WeaponStats { get; set; }

        // State
        [ParameterDescription("The turret's current hit points.")]
        public int Health { get; set; }
        [ParameterDescription("The turret's hit points when unhurt.")]
        public int MaxHealth { get; set; }
        [ParameterDescription("Whether the turret works right now.")]
        public bool IsWorking { get; set; }
    }

    public class WeaponStatsDto
    {
        [ParameterDescription("The damage type the weapon deals.")]
        public string DamageDef { get; set; }
        [ParameterDescription("How much damage one hit does.")]
        public int DamageAmount { get; set; }
        [ParameterDescription("How much armour the hit ignores, in percent.")]
        public float ArmorPenetration { get; set; }
        [ParameterDescription("How far an exploding hit reaches, in cells.")]
        public float ExplosionRadius { get; set; }
        [ParameterDescription("How far the weapon reaches, in cells.")]
        public float Range { get; set; }
        [ParameterDescription("How long it aims before firing, in seconds.")]
        public float WarmupTime { get; set; }
        [ParameterDescription("How long it waits between shots, in seconds.")]
        public float CooldownTime { get; set; }
        [ParameterDescription("How many shots one burst fires.")]
        public int BurstShotCount { get; set; }
        [ParameterDescription("How likely a shot hits, 0 to 1.")]
        public float Accuracy { get; set; }
    }

    public class FloatRange
    {
        [ParameterDescription("The lowest value the range allows.")]
        public float Min { get; set; }
        [ParameterDescription("The highest value the range allows.")]
        public float Max { get; set; }

        public FloatRange() { }

        public FloatRange(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
