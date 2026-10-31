using System.Collections.Generic;

namespace RIMAPI.Models
{
    // A pawn in a role: the id every write takes and the name a reader needs.
    public class RolePawnDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // One role of the colony's ideoligion (ticket #3186). The role's label is
    // the name the game generated for it ("Pujari", "Guru"), which is what the
    // alert and the mood thought say; the def says what kind of role it is
    // (leader, moral guide, specialist) and survives a reform that renames it.
    public class IdeoRoleDto
    {
        public string Def { get; set; }
        public string Label { get; set; }

        // Active is false for a role the ideoligion carries but does not use
        // (its meme is not selected): such a role is not empty, it is unused.
        public bool Active { get; set; }
        public bool LeaderRole { get; set; }

        // Multi is a role several pawns can hold at once; a single role takes
        // one pawn and Assign replaces the one who held it.
        public bool Multi { get; set; }
        public bool Empty { get; set; }

        public List<RolePawnDto> Assigned { get; set; }

        // Eligible names the colonists whose requirements the role's own check
        // meets, so an empty role comes with the candidates that could fill it.
        public List<RolePawnDto> Eligible { get; set; }
    }

    // What GET /api/v1/ideo/roles answers: the colony's ideoligion and its
    // roles with the pawns that fill them.
    public class IdeoRolesDto
    {
        public string Ideo { get; set; }
        public int ColonistCount { get; set; }
        public int EmptyCount { get; set; }
        public List<IdeoRoleDto> Roles { get; set; }
    }

    // One place a ritual is held at: a cell on the map, or a building such as
    // the altar or the ritual spot. Valid is false when the game has no target
    // for the ritual yet, which is what the Begin dialog asks the player for.
    public class RitualTargetDto
    {
        public bool Valid { get; set; }
        public int ThingId { get; set; }
        public string Label { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
    }

    // One obligation the ideoligion carries for a ritual: the game's own "this
    // ritual is due" note, with the target it was created for and how long it
    // stays open.
    public class RitualObligationDto
    {
        public int Id { get; set; }
        public bool Expires { get; set; }
        public int TicksUntilExpiration { get; set; }
        public RitualTargetDto Target { get; set; }
    }

    // One role a ritual hands out, so a caller can tell why a ritual is not
    // ready: the leader's speech needs a pawn in the leader role, a funeral a
    // priest, and so on.
    public class RitualRoleDto
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public bool Required { get; set; }
        public int MaxCount { get; set; }
    }

    // One ritual of the colony's ideoligion (ticket #3263). Rituals and the
    // leader's speech are the mood levers the game gives an ideoligion, and
    // neither was reachable through the tool: a colony at "Major break risk"
    // could watch its own festivals go unused.
    public class IdeoRitualDto
    {
        public string Def { get; set; }
        public string Label { get; set; }

        // Anytime is a ritual the player may call at will; DateTriggered one the
        // ideoligion schedules (every 15 days, at a season change).
        public bool Anytime { get; set; }
        public bool DateTriggered { get; set; }

        // Ready is the game's own verdict for the target below, and Reason is
        // the sentence the Begin dialog would show instead of the button.
        public bool Ready { get; set; }
        public string Reason { get; set; }

        // The repeat penalty is the ritual's cost: performed too soon after the
        // last one it yields less mood, up to -95%, for up to 20 days.
        public bool RepeatPenaltyActive { get; set; }
        public float RepeatQualityPenalty { get; set; }
        public string RepeatPenaltyTimeLeft { get; set; }

        // CooldownTicks is what is left of the ability cooldown the ritual
        // shares with its role's ability (the leader's speech), 0 when none.
        public int CooldownTicks { get; set; }

        public RitualTargetDto Target { get; set; }

        // TargetIsCandidate is true when the ritual's own target filter named
        // nothing (the Begin dialog asks the player to pick a place) and the
        // target above is the best place the ritual's obligation filter
        // accepts instead. Readiness is judged there, and passing that thing or
        // cell to a start is what makes the start work (ticket #3266).
        public bool TargetIsCandidate { get; set; }

        // Candidates are the further places the obligation filter accepts, so a
        // run can pick another altar or the corpse it wants instead of guessing
        // a cell. Capped; an empty list means the game named none.
        public List<RitualTargetDto> Candidates { get; set; }

        // TargetKinds is what the game's target filter says the ritual needs
        // ("gather at an altar or ritual spot"), which is the only hint left
        // when no candidate exists.
        public List<string> TargetKinds { get; set; }

        public List<RitualObligationDto> Obligations { get; set; }
        public List<RitualRoleDto> Roles { get; set; }

        // Running is the ritual as it stands right now, null when none of this
        // ritual is going on. A ritual that runs cannot start again, and the
        // game's reason for that ("already in progress") says nothing about how
        // long it has left or whether its pawns still attend it (ticket #3364).
        public RitualRunningDto Running { get; set; }
    }

    // One pawn the running ritual owns: the job it is on and the duty the
    // ritual gave it. A ritual whose pawns are all back on ordinary work is
    // stuck, whatever the game's own "in progress" verdict says.
    public class RitualParticipantDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Job is the pawn's current job def ("GiveSpeech", "SpectateCeremony",
        // "Sleep"); Duty is the ritual duty the lord handed it ("GiveSpeech",
        // "Spectate"), empty when the pawn holds none any more.
        public string Job { get; set; }
        public string Duty { get; set; }
    }

    // A ritual that is running right now (ticket #3364). The leader's speech
    // lasts 5000 ticks (two in-game hours) by the game's own def, so "it takes
    // ages" and "it never ends" look the same in a read that carries no
    // duration: this one carries the clock, the stage, and what every pawn of
    // the ritual is doing instead.
    public class RitualRunningDto
    {
        public string Ritual { get; set; }
        public string Def { get; set; }
        public string Organizer { get; set; }
        public int OrganizerId { get; set; }

        // DurationTicks is the ritual's whole length (the def's range already
        // rolled), TicksLeft what is left of it, Progress 0..1.
        public int DurationTicks { get; set; }
        public int TicksLeft { get; set; }
        public float Progress { get; set; }

        // StageIndex is 1-based, StageCount the ritual's number of stages.
        public int StageIndex { get; set; }
        public int StageCount { get; set; }

        // Cancelled is the ritual's own flag: true once the game took a cancel,
        // so a caller reads its cancel back instead of assuming it.
        public bool Cancelled { get; set; }

        // Attended is true when at least one of the ritual's pawns still holds
        // one of the duties the ritual's stages hand out. When it is false the
        // ritual cannot finish on its own; `ritual/end` is the way out.
        public bool Attended { get; set; }

        public List<RitualParticipantDto> Pawns { get; set; }
    }

    // What POST /api/v1/ideo/ritual/end answers: Ended is the ritual's own
    // cancel flag read back, not the fact that the call went out.
    public class RitualEndResultDto
    {
        public string Ideo { get; set; }
        public string Ritual { get; set; }
        public string Def { get; set; }
        public bool Ended { get; set; }
        public string Reason { get; set; }
        public bool WasAttended { get; set; }
        public List<RitualParticipantDto> Pawns { get; set; }
    }

    // What GET /api/v1/ideo/rituals answers: the ideoligion's rituals with the
    // game's readiness verdict, and how many of them could start right now.
    public class IdeoRitualsDto
    {
        public string Ideo { get; set; }
        public int Tick { get; set; }
        public int ReadyCount { get; set; }
        public List<IdeoRitualDto> Rituals { get; set; }
    }

    // What POST /api/v1/ideo/ritual/start answers. Started is the game's own
    // answer read back (a LordJob_Ritual of that ritual stands afterwards), not
    // the fact that the call went out; Reason carries the game's refusal when it
    // did not start.
    public class RitualStartResultDto
    {
        public string Ideo { get; set; }
        public string Ritual { get; set; }
        public string Def { get; set; }
        public bool Started { get; set; }
        public string Reason { get; set; }
        public string Organizer { get; set; }
        public int OrganizerId { get; set; }
        public RitualTargetDto Target { get; set; }
        public List<RolePawnDto> Participants { get; set; }

        // TargetFromCandidate is true when the caller named no place and the
        // ritual's own target filter named none either, so the start took the
        // first place the ritual's obligation filter accepts; the same place
        // the `rituals` read offers as best candidate (ticket #3322).
        public bool TargetFromCandidate { get; set; }

        // ObligationFulfilled is true when the ritual paid off one of the
        // ideoligion's due obligations; that is the mood-relevant part.
        public bool ObligationFulfilled { get; set; }
    }

    // What POST /api/v1/ideo/role/assign answers: who holds the role now, and
    // which pawn the assignment pushed out of it (a single role holds one).
    public class RoleAssignResultDto
    {
        public string Ideo { get; set; }
        public string Role { get; set; }
        public string RoleDef { get; set; }
        public int PawnId { get; set; }
        public string Pawn { get; set; }
        public int PreviousPawnId { get; set; }
        public string PreviousPawn { get; set; }
        public List<RolePawnDto> Assigned { get; set; }
    }
}
