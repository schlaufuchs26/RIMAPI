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
