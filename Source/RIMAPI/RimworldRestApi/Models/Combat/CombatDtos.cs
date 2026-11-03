using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // Combat is the one thing the tool could not do (ticket #3154): colonists
    // never fight by themselves, so a raid ended with people standing in the
    // open. These models carry what a decision needs: who is hostile, where
    // everyone stands, who is drafted and what each pawn is doing, so a run can
    // order and then read the order back.
    public class CombatantDto
    {
        [ParameterDescription("The pawn's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }
        // hostile, manhunter, colonist, prisoner, guest, neutral, animal.
        [ParameterDescription("What the pawn is: colonist, raider, animal or other.")]
        public string Kind { get; set; }
        [ParameterDescription("The pawn's faction, as the game names it.")]
        public string Faction { get; set; }
        [ParameterDescription("Whether the pawn is hostile to the colony.")]
        public bool Hostile { get; set; }
        [ParameterDescription("Whether the pawn is downed.")]
        public bool Downed { get; set; }
        [ParameterDescription("Whether the pawn is dead.")]
        public bool Dead { get; set; }
        [ParameterDescription("The pawn's overall health, 0 to 1.")]
        public float Health { get; set; }
        public PositionDto Position { get; set; }
        // Weapon is the primary equipment's label and WeaponRange its verb's
        // range in cells; a pawn with no weapon has both empty/zero, and a
        // melee weapon has range 1.5.
        [ParameterDescription("The weapon the pawn carries, as the game names it.")]
        public string Weapon { get; set; }
        [ParameterDescription("How far the weapon reaches, in map cells.")]
        public float WeaponRange { get; set; }
        // DamagePerHit is what one attack of the pawn's primary verb takes off
        // a target and DamageKind says whether that is a shot or a blow
        // ("ranged" or "melee"); MeleeDamage is what the same pawn does with
        // the weapon's stock or its own teeth. A warg bit a colonist to death
        // while the read named only "unarmed", so a run could not see that
        // trading blows was a loss (fox ticket #3451).
        [ParameterDescription("How much damage one hit of the weapon does.")]
        public float DamagePerHit { get; set; }
        [ParameterDescription("The damage type the weapon deals, e.g. Cut.")]
        public string DamageKind { get; set; }
        [ParameterDescription("The damage the pawn does in melee.")]
        public float MeleeDamage { get; set; }
        // HitPoints and MaxHitPoints are the body's hit points, the unit the
        // damage above is measured in; Health keeps the percentage.
        [ParameterDescription("The pawn's current hit points.")]
        public int HitPoints { get; set; }
        [ParameterDescription("The pawn's hit points when unhurt.")]
        public int MaxHitPoints { get; set; }
        // MoveSpeed is the pawn's movement in cells per second
        // (StatDefOf.MoveSpeed). A tick is 1/60 s, so this is what decides
        // whether walking away breaks contact with a melee attacker.
        [ParameterDescription("How fast the pawn moves, in cells per second.")]
        public float MoveSpeed { get; set; }
        [ParameterDescription("Whether the pawn is drafted.")]
        public bool Drafted { get; set; }
        [ParameterDescription("Whether the pawn fires without an order.")]
        public bool FireAtWill { get; set; }
        // Activity is the current job's defName (the game's own name for what
        // the pawn is doing), Target the thing or pawn that job acts on.
        [ParameterDescription("What the pawn is doing, as the game names it.")]
        public string Activity { get; set; }
        [ParameterDescription("The numeric id of the pawn's current target.")]
        public int TargetId { get; set; }
        [ParameterDescription("The name of the pawn's current target.")]
        public string Target { get; set; }
        // Moving says the pawn walks right now; Fighting says its job is an
        // attack. A drafted idle pawn is neither and stands.
        [ParameterDescription("Whether the pawn is moving now.")]
        public bool Moving { get; set; }
        [ParameterDescription("Whether the pawn is fighting now.")]
        public bool Fighting { get; set; }
        // LordJob is the game's own name for the group the pawn belongs to
        // (LordJob_AssaultColony for a raid, LordJob_DefendAndExpandHive for a
        // hive's insects), empty when it has no lord. Walking is no sign of a
        // fight on its own: a hive's insects wander inside their cave all day,
        // so the tool reads this to tell a raid from a group that stays put
        // (ticket #3294).
        [ParameterDescription("The lord job that steers the pawn, e.g. a raid.")]
        public string LordJob { get; set; }
        // AttackBlocked names why an ordered attack is not landing: a drafted
        // pawn whose job is an attack stood an hour next to a mad raccoon while
        // the animal stayed at full health, and the read could not tell a miss
        // from an order that never fired (fox #3737). Empty when the attack can
        // land or the pawn has no attack job.
        [ParameterDescription("Why the pawn cannot attack, when something blocks it.")]
        public string AttackBlocked { get; set; }
    }

    public class CombatStateDto
    {
        public int MapId { get; set; }
        [ParameterDescription("The hostile pawns on the map.")]
        public List<CombatantDto> Hostiles { get; set; }
        [ParameterDescription("The colony's pawns on the map.")]
        public List<CombatantDto> Colonists { get; set; }
        [ParameterDescription("How many hostile pawns the map holds.")]
        public int HostilesCount { get; set; }
        [ParameterDescription("How many colonists are drafted.")]
        public int DraftedCount { get; set; }
        [ParameterDescription("Whether a fight is running on the map.")]
        public bool FightRunning { get; set; }
    }

    public class DraftRequestDto
    {
        [ParameterDescription("The pawns to draft or undraft, by numeric id.")]
        public List<int> PawnIds { get; set; }
        [ParameterDescription("Whether the pawns should be drafted.")]
        public bool Drafted { get; set; }
    }

    public class DraftResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("Whether the pawn is drafted now.")]
        public bool Drafted { get; set; }
        // Note names a refusal: not a colonist, downed, or no drafter.
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }

    // OrderRequestDto is one order to one drafted colonist. Order is move,
    // attack, hold or fire_at_will. Move uses X/Z, attack TargetId, hold
    // nothing, fire_at_will FireAtWill.
    public class OrderRequestDto
    {
        public int PawnId { get; set; }
        [ParameterDescription("The order to give: attack, move, hold or similar.")]
        public string Order { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        [ParameterDescription("The numeric id of the pawn or thing to attack.")]
        public int TargetId { get; set; }
        [ParameterDescription("Whether the pawn may fire without a new order.")]
        public bool FireAtWill { get; set; }
        // MaxDanger is the worst a walk may be (ticket #3209): none, some or
        // deadly on the game's own Danger scale. Empty means some, the level
        // the reachability check used before the field existed. The scale is
        // temperature only, so fire and toxic ground are scanned separately.
        [ParameterDescription("The most danger the path may cross: safe, low, medium or high.")]
        public string MaxDanger { get; set; }
    }

    public class OrderResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The order the game received, as it names it.")]
        public string Order { get; set; }
        [ParameterDescription("The job the game queued for it.")]
        public string Job { get; set; }
        [ParameterDescription("The numeric id of the pawn or thing it attacks.")]
        public int TargetId { get; set; }
        [ParameterDescription("The name of the pawn or thing it attacks.")]
        public string Target { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        [ParameterDescription("Whether the pawn is drafted.")]
        public bool Drafted { get; set; }
        [ParameterDescription("Whether the pawn may fire without a new order.")]
        public bool FireAtWill { get; set; }
        // Position and Activity are the read-back: where the pawn stands and
        // what it is doing after the order went out, so the caller sees whether
        // the order took.
        public PositionDto Position { get; set; }
        [ParameterDescription("What the pawn is doing, as the game names it.")]
        public string Activity { get; set; }
        // Reachable and Reach are the reachability check the order made before
        // it was queued (ticket #3175): an unreachable cell fails as a job the
        // game drops, so the answer says whether the pawn can walk there and
        // how far it is. Reach is empty when no path length was measured.
        [ParameterDescription("Whether the target can be reached.")]
        public bool Reachable { get; set; }
        [ParameterDescription("How the pawn reaches the target, when it can.")]
        public string Reach { get; set; }
        // PathDanger is the worst danger the walk really crosses, on the
        // game's scale (none, some, deadly), and PathHazards names what it is
        // ("fire (3 cells), toxic waste (8 cells)"). Both come from a scan of
        // the path cells, because the game's danger scale is temperature only
        // and a path through fire reads "none" there (ticket #3209).
        [ParameterDescription("The most danger the chosen path crosses.")]
        public string PathDanger { get; set; }
        [ParameterDescription("The hazards the chosen path crosses.")]
        public string PathHazards { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }
}
