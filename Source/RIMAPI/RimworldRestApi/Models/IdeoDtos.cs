using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // A pawn in a role: the id every write takes and the name a reader needs.
    public class RolePawnDto
    {
        [ParameterDescription("The pawn's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // One role of the colony's ideoligion (ticket #3186). The role's label is
    // the name the game generated for it ("Pujari", "Guru"), which is what the
    // alert and the mood thought say; the def says what kind of role it is
    // (leader, moral guide, specialist) and survives a reform that renames it.
    public class IdeoRoleDto
    {
        [ParameterDescription("The role's def name.")]
        public string Def { get; set; }
        public string Label { get; set; }

        // Active is false for a role the ideoligion carries but does not use
        // (its meme is not selected): such a role is not empty, it is unused.
        [ParameterDescription("Whether the role is active in the ideoligion.")]
        public bool Active { get; set; }
        [ParameterDescription("Whether this is the leader's role.")]
        public bool LeaderRole { get; set; }

        // Multi is a role several pawns can hold at once; a single role takes
        // one pawn and Assign replaces the one who held it.
        [ParameterDescription("Whether more than one pawn may hold it.")]
        public bool Multi { get; set; }
        [ParameterDescription("Whether no pawn holds it.")]
        public bool Empty { get; set; }

        [ParameterDescription("The pawns that hold the role.")]
        public List<RolePawnDto> Assigned { get; set; }

        // Eligible names the colonists whose requirements the role's own check
        // meets, so an empty role comes with the candidates that could fill it.
        [ParameterDescription("How many pawns could take the role.")]
        public List<RolePawnDto> Eligible { get; set; }
    }

    // What GET /api/v1/ideo answers (ticket #3612): the colony's ideoligion
    // as the game holds it. Fluid is the reformable kind; Reformable is the
    // game's own verdict for this moment (a fluid ideo still needs its
    // development points before a reform is possible). Structure is the
    // structure meme's label, the word the ideoligion window shows.
    public class IdeoSummaryDto
    {
        [ParameterDescription("The ideoligion's name, as the game shows it.")]
        public string Ideo { get; set; }
        [ParameterDescription("The ideoligion's structure, e.g. theist.")]
        public string Structure { get; set; }
        [ParameterDescription("Whether the ideoligion is fluid.")]
        public bool Fluid { get; set; }
        [ParameterDescription("Whether the ideoligion may reform.")]
        public bool Reformable { get; set; }
        [ParameterDescription("The points the ideoligion has to spend.")]
        public int Points { get; set; }
        [ParameterDescription("How many points one reform costs.")]
        public int ReformPoints { get; set; }
        [ParameterDescription("How many times the ideoligion has reformed.")]
        public int ReformCount { get; set; }
        [ParameterDescription("How many pawns follow it.")]
        public int Believers { get; set; }
        [ParameterDescription("The memes that shape the ideoligion.")]
        public List<string> Memes { get; set; }
        [ParameterDescription("The precepts that rule the ideoligion.")]
        public List<string> Precepts { get; set; }

        // PreceptDetails is the same set with what each one buys and costs
        // (ticket #3927), read from the game's own precept defs. A founding
        // reports it so the pick is a decision with reasons instead of a list
        // of labels.
        [ParameterDescription("One entry per precept with its effects.")]
        public List<PreceptDetailDto> PreceptDetails { get; set; }
    }

    // One precept of the colony's ideoligion with its effect (ticket #3927).
    public class PreceptDetailDto
    {
        [ParameterDescription("The precept's def name.")]
        public string Def { get; set; }
        // Label is the "Issue: label" the ideoligion window shows.
        public string Label { get; set; }
        [ParameterDescription("The issue the precept rules on.")]
        public string Issue { get; set; }
        [ParameterDescription("How much the precept shapes the ideoligion.")]
        public string Impact { get; set; }
        public string Description { get; set; }
        // RequiredMemes names the memes a precept demands by defName; empty
        // means any ideo may carry it.
        [ParameterDescription("The memes the precept requires of the ideoligion.")]
        public List<string> RequiredMemes { get; set; }
        // Effects restates the def: stat factors, stat offsets, the mood
        // range of the thought its comps grant, what it refuses.
        [ParameterDescription("What the precept changes in play.")]
        public List<string> Effects { get; set; }
        // Ritual names the ritual the precept brings with its required roles,
        // empty when it brings none.
        [ParameterDescription("The ritual the precept names, when it names one.")]
        public string Ritual { get; set; }
    }

    // What GET /api/v1/ideo/roles answers: the colony's ideoligion and its
    // roles with the pawns that fill them.
    public class IdeoRolesDto
    {
        [ParameterDescription("The ideoligion's name, as the game shows it.")]
        public string Ideo { get; set; }
        [ParameterDescription("How many colonists the colony has.")]
        public int ColonistCount { get; set; }
        [ParameterDescription("How many roles no pawn holds.")]
        public int EmptyCount { get; set; }
        [ParameterDescription("One entry per role.")]
        public List<IdeoRoleDto> Roles { get; set; }
    }

    // One place a ritual is held at: a cell on the map, or a building such as
    // the altar or the ritual spot. Valid is false when the game has no target
    // for the ritual yet, which is what the Begin dialog asks the player for.
    public class RitualTargetDto
    {
        [ParameterDescription("Whether the target the ritual names is valid.")]
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
        [ParameterDescription("The obligation's id, as the game holds it.")]
        public int Id { get; set; }
        [ParameterDescription("The tick the obligation expires on.")]
        public bool Expires { get; set; }
        [ParameterDescription("How many ticks are left before it expires.")]
        public int TicksUntilExpiration { get; set; }
        [ParameterDescription("The target the obligation wants the ritual to use.")]
        public RitualTargetDto Target { get; set; }
    }

    // One role a ritual hands out, so a caller can tell why a ritual is not
    // ready: the leader's speech needs a pawn in the leader role, a funeral a
    // priest, and so on.
    public class RitualRoleDto
    {
        [ParameterDescription("The ritual role's def name.")]
        public string Id { get; set; }
        public string Label { get; set; }
        [ParameterDescription("Whether the role must be filled.")]
        public bool Required { get; set; }
        [ParameterDescription("How many pawns may take the role.")]
        public int MaxCount { get; set; }
    }

    // One ritual of the colony's ideoligion (ticket #3263). Rituals and the
    // leader's speech are the mood levers the game gives an ideoligion, and
    // neither was reachable through the tool: a colony at "Major break risk"
    // could watch its own festivals go unused.
    public class IdeoRitualDto
    {
        [ParameterDescription("The ritual's def name.")]
        public string Def { get; set; }
        public string Label { get; set; }

        // Anytime is a ritual the player may call at will; DateTriggered one the
        // ideoligion schedules (every 15 days, at a season change).
        [ParameterDescription("Whether the ritual may start at any time.")]
        public bool Anytime { get; set; }
        [ParameterDescription("Whether a date triggers the ritual.")]
        public bool DateTriggered { get; set; }

        // Ready is the game's own verdict for the target below, and Reason is
        // the sentence the Begin dialog would show instead of the button.
        [ParameterDescription("Whether the ritual can start now.")]
        public bool Ready { get; set; }
        [ParameterDescription("Why the ritual cannot start, when it cannot.")]
        public string Reason { get; set; }

        // The repeat penalty is the ritual's cost: performed too soon after the
        // last one it yields less mood, up to -95%, for up to 20 days.
        [ParameterDescription("Whether the repeat penalty applies now.")]
        public bool RepeatPenaltyActive { get; set; }
        [ParameterDescription("How much quality the repeat penalty takes.")]
        public float RepeatQualityPenalty { get; set; }
        [ParameterDescription("How long the repeat penalty lasts, in ticks.")]
        public string RepeatPenaltyTimeLeft { get; set; }

        // CooldownTicks is what is left of the ability cooldown the ritual
        // shares with its role's ability (the leader's speech), 0 when none.
        [ParameterDescription("How many ticks must pass before it may repeat.")]
        public int CooldownTicks { get; set; }

        [ParameterDescription("The target the ritual needs, as the game names it.")]
        public RitualTargetDto Target { get; set; }

        // TargetIsCandidate is true when the ritual's own target filter named
        // nothing (the Begin dialog asks the player to pick a place) and the
        // target above is the best place the ritual's obligation filter
        // accepts instead. Readiness is judged there, and passing that thing or
        // cell to a start is what makes the start work (ticket #3266).
        [ParameterDescription("Whether the target is a candidate pawn.")]
        public bool TargetIsCandidate { get; set; }

        // Candidates are the further places the obligation filter accepts, so a
        // run can pick another altar or the corpse it wants instead of guessing
        // a cell. Capped; an empty list means the game named none.
        [ParameterDescription("The pawns that could serve as target.")]
        public List<RitualTargetDto> Candidates { get; set; }

        // TargetKinds is what the game's target filter says the ritual needs
        // ("gather at an altar or ritual spot"), which is the only hint left
        // when no candidate exists.
        [ParameterDescription("The kinds of target the ritual accepts.")]
        public List<string> TargetKinds { get; set; }

        [ParameterDescription("The obligations that name this ritual.")]
        public List<RitualObligationDto> Obligations { get; set; }
        [ParameterDescription("The roles the ritual needs filled.")]
        public List<RitualRoleDto> Roles { get; set; }

        // Running is the ritual as it stands right now, null when none of this
        // ritual is going on. A ritual that runs cannot start again, and the
        // game's reason for that ("already in progress") says nothing about how
        // long it has left or whether its pawns still attend it (ticket #3364).
        [ParameterDescription("Whether the ritual is running now.")]
        public RitualRunningDto Running { get; set; }
    }

    // One pawn the running ritual owns: the job it is on and the duty the
    // ritual gave it. A ritual whose pawns are all back on ordinary work is
    // stuck, whatever the game's own "in progress" verdict says.
    public class RitualParticipantDto
    {
        [ParameterDescription("The participant's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }

        // Job is the pawn's current job def ("GiveSpeech", "SpectateCeremony",
        // "Sleep"); Duty is the ritual duty the lord handed it ("GiveSpeech",
        // "Spectate"), empty when the pawn holds none any more.
        [ParameterDescription("The job the participant does in the ritual.")]
        public string Job { get; set; }
        [ParameterDescription("The duty the participant holds in the ritual.")]
        public string Duty { get; set; }
    }

    // A ritual that is running right now (ticket #3364). The leader's speech
    // lasts 5000 ticks (two in-game hours) by the game's own def, so "it takes
    // ages" and "it never ends" look the same in a read that carries no
    // duration: this one carries the clock, the stage, and what every pawn of
    // the ritual is doing instead.
    public class RitualRunningDto
    {
        [ParameterDescription("The ritual's def name.")]
        public string Ritual { get; set; }
        [ParameterDescription("The ritual's def name.")]
        public string Def { get; set; }
        [ParameterDescription("The name of the pawn who organized it.")]
        public string Organizer { get; set; }
        [ParameterDescription("The numeric id of the organizer.")]
        public int OrganizerId { get; set; }

        // DurationTicks is the ritual's whole length (the def's range already
        // rolled), TicksLeft what is left of it, Progress 0..1.
        [ParameterDescription("How long the ritual takes, in ticks.")]
        public int DurationTicks { get; set; }
        [ParameterDescription("How many ticks are left.")]
        public int TicksLeft { get; set; }
        [ParameterDescription("How far the ritual has run, 0 to 1.")]
        public float Progress { get; set; }

        // StageIndex is 1-based, StageCount the ritual's number of stages.
        [ParameterDescription("Which stage the ritual is in.")]
        public int StageIndex { get; set; }
        [ParameterDescription("How many stages the ritual has.")]
        public int StageCount { get; set; }

        // Cancelled is the ritual's own flag: true once the game took a cancel,
        // so a caller reads its cancel back instead of assuming it.
        [ParameterDescription("Whether the ritual was cancelled.")]
        public bool Cancelled { get; set; }

        // Attended is true when at least one of the ritual's pawns still holds
        // one of the duties the ritual's stages hand out. When it is false the
        // ritual cannot finish on its own; `ritual/end` is the way out.
        [ParameterDescription("How many pawns attended.")]
        public bool Attended { get; set; }

        [ParameterDescription("The pawns that take part in the ritual.")]
        public List<RitualParticipantDto> Pawns { get; set; }
    }

    // What POST /api/v1/ideo/ritual/end answers: Ended is the ritual's own
    // cancel flag read back, not the fact that the call went out.
    public class RitualEndResultDto
    {
        [ParameterDescription("The ideoligion's name, as the game shows it.")]
        public string Ideo { get; set; }
        [ParameterDescription("The ritual's def name.")]
        public string Ritual { get; set; }
        [ParameterDescription("The ritual's def name.")]
        public string Def { get; set; }
        [ParameterDescription("Whether the call ended the ritual.")]
        public bool Ended { get; set; }
        [ParameterDescription("Why it ended, when the game gives a reason.")]
        public string Reason { get; set; }
        [ParameterDescription("Whether enough pawns attended.")]
        public bool WasAttended { get; set; }
        [ParameterDescription("The pawns that took part.")]
        public List<RitualParticipantDto> Pawns { get; set; }
    }

    // What GET /api/v1/ideo/rituals answers: the ideoligion's rituals with the
    // game's readiness verdict, and how many of them could start right now.
    public class IdeoRitualsDto
    {
        [ParameterDescription("The ideoligion's name, as the game shows it.")]
        public string Ideo { get; set; }
        [ParameterDescription("The game tick the answer was built on.")]
        public int Tick { get; set; }
        [ParameterDescription("How many rituals can start now.")]
        public int ReadyCount { get; set; }
        [ParameterDescription("One entry per ritual.")]
        public List<IdeoRitualDto> Rituals { get; set; }
    }

    // What POST /api/v1/ideo/ritual/start answers. Started is the game's own
    // answer read back (a LordJob_Ritual of that ritual stands afterwards), not
    // the fact that the call went out; Reason carries the game's refusal when it
    // did not start.
    public class RitualStartResultDto
    {
        [ParameterDescription("The ideoligion's name, as the game shows it.")]
        public string Ideo { get; set; }
        [ParameterDescription("The ritual's def name.")]
        public string Ritual { get; set; }
        [ParameterDescription("The ritual's def name.")]
        public string Def { get; set; }
        [ParameterDescription("Whether the call started the ritual.")]
        public bool Started { get; set; }
        [ParameterDescription("Why it did not start, when it did not.")]
        public string Reason { get; set; }
        [ParameterDescription("The name of the pawn who organizes it.")]
        public string Organizer { get; set; }
        [ParameterDescription("The numeric id of the organizer.")]
        public int OrganizerId { get; set; }
        [ParameterDescription("The target the ritual works on.")]
        public RitualTargetDto Target { get; set; }
        [ParameterDescription("The pawns that take part in the ritual.")]
        public List<RolePawnDto> Participants { get; set; }

        // TargetFromCandidate is true when the caller named no place and the
        // ritual's own target filter named none either, so the start took the
        // first place the ritual's obligation filter accepts; the same place
        // the `rituals` read offers as best candidate (ticket #3322).
        [ParameterDescription("Whether the target was picked from the candidates.")]
        public bool TargetFromCandidate { get; set; }

        // ObligationFulfilled is true when the ritual paid off one of the
        // ideoligion's due obligations; that is the mood-relevant part.
        [ParameterDescription("Whether a pending obligation was fulfilled.")]
        public bool ObligationFulfilled { get; set; }
    }

    // What POST /api/v1/ideo/role/assign answers: who holds the role now, and
    // which pawn the assignment pushed out of it (a single role holds one).
    public class RoleAssignResultDto
    {
        [ParameterDescription("The ideoligion's name, as the game shows it.")]
        public string Ideo { get; set; }
        [ParameterDescription("The role's def name.")]
        public string Role { get; set; }
        [ParameterDescription("The role's def name.")]
        public string RoleDef { get; set; }
        public int PawnId { get; set; }
        [ParameterDescription("The pawn that now holds the role.")]
        public string Pawn { get; set; }
        [ParameterDescription("The numeric id of the pawn who held it before.")]
        public int PreviousPawnId { get; set; }
        [ParameterDescription("The pawn who held it before.")]
        public string PreviousPawn { get; set; }
        [ParameterDescription("Whether the role was assigned.")]
        public List<RolePawnDto> Assigned { get; set; }
    }
}
