using System.Collections.Generic;

namespace RIMAPI.Models
{
    // Combat is the one thing the tool could not do (ticket #3154): colonists
    // never fight by themselves, so a raid ended with people standing in the
    // open. These models carry what a decision needs: who is hostile, where
    // everyone stands, who is drafted and what each pawn is doing, so a run can
    // order and then read the order back.
    public class CombatantDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        // hostile, manhunter, colonist, prisoner, guest, neutral, animal.
        public string Kind { get; set; }
        public string Faction { get; set; }
        public bool Hostile { get; set; }
        public bool Downed { get; set; }
        public bool Dead { get; set; }
        public float Health { get; set; }
        public PositionDto Position { get; set; }
        // Weapon is the primary equipment's label and WeaponRange its verb's
        // range in cells; a pawn with no weapon has both empty/zero, and a
        // melee weapon has range 1.5.
        public string Weapon { get; set; }
        public float WeaponRange { get; set; }
        public bool Drafted { get; set; }
        public bool FireAtWill { get; set; }
        // Activity is the current job's defName (the game's own name for what
        // the pawn is doing), Target the thing or pawn that job acts on.
        public string Activity { get; set; }
        public int TargetId { get; set; }
        public string Target { get; set; }
        // Moving says the pawn walks right now; Fighting says its job is an
        // attack. A drafted idle pawn is neither and stands.
        public bool Moving { get; set; }
        public bool Fighting { get; set; }
        // LordJob is the game's own name for the group the pawn belongs to
        // (LordJob_AssaultColony for a raid, LordJob_DefendAndExpandHive for a
        // hive's insects), empty when it has no lord. Walking is no sign of a
        // fight on its own: a hive's insects wander inside their cave all day,
        // so the tool reads this to tell a raid from a group that stays put
        // (ticket #3294).
        public string LordJob { get; set; }
    }

    public class CombatStateDto
    {
        public int MapId { get; set; }
        public List<CombatantDto> Hostiles { get; set; }
        public List<CombatantDto> Colonists { get; set; }
        public int HostilesCount { get; set; }
        public int DraftedCount { get; set; }
        public bool FightRunning { get; set; }
    }

    public class DraftRequestDto
    {
        public List<int> PawnIds { get; set; }
        public bool Drafted { get; set; }
    }

    public class DraftResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        public bool Drafted { get; set; }
        // Note names a refusal: not a colonist, downed, or no drafter.
        public string Note { get; set; }
    }

    // OrderRequestDto is one order to one drafted colonist. Order is move,
    // attack, hold or fire_at_will. Move uses X/Z, attack TargetId, hold
    // nothing, fire_at_will FireAtWill.
    public class OrderRequestDto
    {
        public int PawnId { get; set; }
        public string Order { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public int TargetId { get; set; }
        public bool FireAtWill { get; set; }
        // MaxDanger is the worst a walk may be (ticket #3209): none, some or
        // deadly on the game's own Danger scale. Empty means some, the level
        // the reachability check used before the field existed. The scale is
        // temperature only, so fire and toxic ground are scanned separately.
        public string MaxDanger { get; set; }
    }

    public class OrderResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        public string Order { get; set; }
        public string Job { get; set; }
        public int TargetId { get; set; }
        public string Target { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public bool Drafted { get; set; }
        public bool FireAtWill { get; set; }
        // Position and Activity are the read-back: where the pawn stands and
        // what it is doing after the order went out, so the caller sees whether
        // the order took.
        public PositionDto Position { get; set; }
        public string Activity { get; set; }
        // Reachable and Reach are the reachability check the order made before
        // it was queued (ticket #3175): an unreachable cell fails as a job the
        // game drops, so the answer says whether the pawn can walk there and
        // how far it is. Reach is empty when no path length was measured.
        public bool Reachable { get; set; }
        public string Reach { get; set; }
        // PathDanger is the worst danger the walk really crosses, on the
        // game's scale (none, some, deadly), and PathHazards names what it is
        // ("fire (3 cells), toxic waste (8 cells)"). Both come from a scan of
        // the path cells, because the game's danger scale is temperature only
        // and a path through fire reads "none" there (ticket #3209).
        public string PathDanger { get; set; }
        public string PathHazards { get; set; }
        public string Note { get; set; }
    }
}
