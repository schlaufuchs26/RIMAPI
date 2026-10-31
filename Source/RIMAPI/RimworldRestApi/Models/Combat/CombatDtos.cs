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
        public string Note { get; set; }
    }
}
