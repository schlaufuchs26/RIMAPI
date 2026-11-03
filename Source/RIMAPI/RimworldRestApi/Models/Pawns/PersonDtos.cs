using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // A humanlike pawn who is not a free colonist: the people a colony can
    // still gain (ticket #3137). `map/pawns` carries every pawn on the map but
    // nothing about which side it is on, so a caller could not tell the wild
    // woman eating from the stockpile from a colonist. Wild people are tamed,
    // prisoners are recruited through their interaction mode, guests and downed
    // raiders are arrested or captured.
    public class PersonDto
    {
        [ParameterDescription("The pawn's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }

        // wild, prisoner, slave, guest, hostile, factionless or neutral.
        [ParameterDescription("What the pawn is to the colony: colonist, prisoner, guest or hostile.")]
        public string Kind { get; set; }
        [ParameterDescription("The pawn's faction, as the game names it.")]
        public string Faction { get; set; }
        [ParameterDescription("Whether the pawn is downed.")]
        public bool Downed { get; set; }
        [ParameterDescription("The pawn's overall health, 0 to 1.")]
        public float Health { get; set; }

        // Tameable is TameUtility.CanTame, the game's own check.
        [ParameterDescription("Whether the colony can tame the animal.")]
        public bool Tameable { get; set; }

        // The prisoner fields are -1 (resistance, will) and false (recruitable)
        // for a pawn that is not a guest.
        [ParameterDescription("The prisoner's resistance left before recruitment.")]
        public float Resistance { get; set; }
        [ParameterDescription("The prisoner's will left before recruitment.")]
        public float Will { get; set; }
        [ParameterDescription("Whether the game allows recruiting the pawn.")]
        public bool Recruitable { get; set; }
        [ParameterDescription("The prisoner interaction mode the game has set.")]
        public string InteractionMode { get; set; }
        public PositionDto Position { get; set; }

        // A stranger who arrived to be talked into joining (the Anomaly creep
        // joiner behind the "Lone genius" letter, ticket #3275). Chattable is
        // the game's own offer: he still stands on his entry lord, so a colonist
        // can chat with him now; after the talk the offer letter decides.
        [ParameterDescription("Whether the pawn came with the creepjoiner event.")]
        public bool CreepJoiner { get; set; }
        [ParameterDescription("Whether the pawn can chat right now.")]
        public bool Chattable { get; set; }
    }

    // What a chat order did: the colonist who walks over to talk, or that no
    // colonist could take it now (ticket #3275).
    public class ChatResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The numeric id of the pawn it chatted with.")]
        public int ChatterId { get; set; }
        [ParameterDescription("The name of the pawn it chatted with.")]
        public string Chatter { get; set; }
        [ParameterDescription("The job that was queued, as the game names it.")]
        public string Job { get; set; }
        [ParameterDescription("Whether the job was queued.")]
        public bool Queued { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }

    // One colonist the game's own check turned down for an order, with the
    // check that stopped them (fox #3625). A skill gate reads as "needs
    // Animals 7"; a free handler tomorrow does not clear it, so the answer
    // must not read as a wait.
    public class HandlerRefusalDto
    {
        [ParameterDescription("The numeric id of the pawn that refused.")]
        public int Id { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The handler skill level the check used.")]
        public int Skill { get; set; }
        [ParameterDescription("The skill's label, as the game shows it.")]
        public string SkillLabel { get; set; }
        [ParameterDescription("Why the game refused the job.")]
        public string Reason { get; set; }
    }

    // What a tame order did: which handler took it, or that the designation
    // stands and no handler is free right now.
    public class TameResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The numeric id of the pawn doing the taming.")]
        public int HandlerId { get; set; }
        [ParameterDescription("The name of the pawn doing the taming.")]
        public string Handler { get; set; }
        [ParameterDescription("The job that was queued, as the game names it.")]
        public string Job { get; set; }
        [ParameterDescription("Whether the job was queued.")]
        public bool Queued { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
        // Every handler candidate the game's check turned down, best first
        // (fox #3625). Empty when a handler took the job or on a build older
        // than #3625.
        [ParameterDescription("One entry per pawn the game refused as handler.")]
        public List<HandlerRefusalDto> Refusals { get; set; } = new List<HandlerRefusalDto>();
    }

    // What a slaughter order did: which handler took it, or that the Slaughter
    // designation stands and no handler is free right now (fox ticket #3507).
    public class SlaughterResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The numeric id of the pawn doing the slaughtering.")]
        public int HandlerId { get; set; }
        [ParameterDescription("The name of the pawn doing the slaughtering.")]
        public string Handler { get; set; }
        [ParameterDescription("The job that was queued, as the game names it.")]
        public string Job { get; set; }
        [ParameterDescription("Whether the job was queued.")]
        public bool Queued { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
        // Every handler candidate the game's check turned down, best first
        // (fox #3625).
        [ParameterDescription("One entry per pawn the game refused as handler.")]
        public List<HandlerRefusalDto> Refusals { get; set; } = new List<HandlerRefusalDto>();
    }

    // What an arrest order did: the job the game got (Arrest or Capture), the
    // prisoner bed it walks to, and whether the arrestor was drafted for it.
    // The bed's room comes with it (ticket #3421): the game's own lookup takes
    // the first free prisoner bed, and a bed in a burning or oven-hot room is
    // where the prisoner cannot walk out.
    public class ArrestResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The numeric id of the pawn making the arrest.")]
        public int ArrestorId { get; set; }
        [ParameterDescription("The name of the pawn making the arrest.")]
        public string Arrestor { get; set; }
        [ParameterDescription("The job that was queued, as the game names it.")]
        public string Job { get; set; }
        [ParameterDescription("The numeric id of the bed the prisoner goes to.")]
        public int BedId { get; set; }
        [ParameterDescription("The name of the bed the prisoner goes to.")]
        public string Bed { get; set; }
        [ParameterDescription("Whether the arresting pawn was drafted.")]
        public bool Drafted { get; set; }
        [ParameterDescription("The numeric id of the room the bed stands in.")]
        public int BedRoomId { get; set; }
        [ParameterDescription("The room's label, as the game shows it.")]
        public string BedRoomLabel { get; set; }
        [ParameterDescription("The room's temperature in degrees Celsius.")]
        public float BedRoomTempC { get; set; }
        [ParameterDescription("Whether the room is on fire.")]
        public bool BedRoomHasFire { get; set; }
        // The bed's cell is under no roof (fox ticket #3622). A bed with no
        // room of its own carries "none" as its role label, which is not a
        // room name; the roof grid is what tells outdoors from a roleless room.
        [ParameterDescription("Whether the bed stands outdoors.")]
        public bool BedOutdoors { get; set; }
        // Why the bed is no prison cell, empty when it is one (fox ticket
        // #3621): a cool prisoner bed on open ground passes every temperature
        // check and still lets the prisoner escape in the same minute.
        [ParameterDescription("Why the bed has no reachable cell.")]
        public string BedNoCellReason { get; set; }
        // The bed the game's own lookup picked first, when the tool took a safer
        // one instead; zero when the game's choice was the one used.
        [ParameterDescription("The numeric id of the bed the game refused.")]
        public int RejectedBedId { get; set; }
        [ParameterDescription("The name of the bed the game refused.")]
        public string RejectedBed { get; set; }
        // The refused bed's own room (ticket #3623): the answer names where the
        // bed the game picked first stands, not only how hot it is.
        [ParameterDescription("The refused bed's room, as the game names it.")]
        public string RejectedBedRoomLabel { get; set; }
        [ParameterDescription("The refused bed's room temperature in degrees Celsius.")]
        public float RejectedBedTempC { get; set; }
        [ParameterDescription("Whether the refused bed's room is on fire.")]
        public bool RejectedBedRoomHasFire { get; set; }
        // Why the refused bed was no cell either (ticket #3621); empty when it
        // was a real cell and only unsafe.
        [ParameterDescription("Why the refused bed has no reachable cell.")]
        public string RejectedBedNoCellReason { get; set; }
    }

    // What a rescue order did: the job the game got, the bed it carries the
    // pawn to, and whether the pawn is a guest of the colony afterwards (a
    // rescued pod survivor becomes one, a rescued colonist stays a colonist).
    public class FeedResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The numeric id of the pawn doing the feeding.")]
        public int FeederId { get; set; }
        [ParameterDescription("The name of the pawn doing the feeding.")]
        public string Feeder { get; set; }
        [ParameterDescription("The job that was queued, as the game names it.")]
        public string Job { get; set; }
        // The meal the job carries and the bed the patient lies in (ticket
        // #3445): a patient who is not in a bed cannot be fed at all, so the
        // answer names the bed it happened in.
        [ParameterDescription("The numeric id of the meal that is fed.")]
        public int MealId { get; set; }
        [ParameterDescription("The name of the meal that is fed.")]
        public string Meal { get; set; }
        [ParameterDescription("The numeric id of the bed the patient lies in.")]
        public int BedId { get; set; }
        [ParameterDescription("The name of the bed the patient lies in.")]
        public string Bed { get; set; }
        [ParameterDescription("Whether the job was queued.")]
        public bool Queued { get; set; }
    }

    public class RescueResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The numeric id of the pawn doing the rescuing.")]
        public int RescuerId { get; set; }
        [ParameterDescription("The name of the pawn doing the rescuing.")]
        public string Rescuer { get; set; }
        [ParameterDescription("The job that was queued, as the game names it.")]
        public string Job { get; set; }
        [ParameterDescription("The numeric id of the bed the patient goes to.")]
        public int BedId { get; set; }
        [ParameterDescription("The name of the bed the patient goes to.")]
        public string Bed { get; set; }
        [ParameterDescription("Whether the bed is set aside for prisoners.")]
        public bool BedForPrisoners { get; set; }
        [ParameterDescription("Whether the rescued pawn is a guest.")]
        public bool Guest { get; set; }
        // The bed's room as it stands now (ticket #3383): a bed in a room above
        // 50 C or with a fire in it is no destination for a carried patient, so
        // the answer has to name what the room reads.
        [ParameterDescription("The numeric id of the room the bed stands in.")]
        public int BedRoomId { get; set; }
        [ParameterDescription("The room's label, as the game shows it.")]
        public string BedRoomLabel { get; set; }
        [ParameterDescription("The room's temperature in degrees Celsius.")]
        public float BedRoomTempC { get; set; }
        [ParameterDescription("Whether the room is on fire.")]
        public bool BedRoomHasFire { get; set; }
        // The bed's cell is under no roof (fox ticket #3622), the answer to a
        // role label the game prints as "none".
        [ParameterDescription("Whether the bed stands outdoors.")]
        public bool BedOutdoors { get; set; }
        // The bed the game's own lookup picked first, when the tool took a safer
        // one instead; zero when the game's choice was the one used.
        [ParameterDescription("The numeric id of the bed the game refused.")]
        public int RejectedBedId { get; set; }
        [ParameterDescription("The name of the bed the game refused.")]
        public string RejectedBed { get; set; }
        // The refused bed's own room (ticket #3623), as in ArrestResultDto.
        [ParameterDescription("The refused bed's room, as the game names it.")]
        public string RejectedBedRoomLabel { get; set; }
        [ParameterDescription("The refused bed's room temperature in degrees Celsius.")]
        public float RejectedBedTempC { get; set; }
        [ParameterDescription("Whether the refused bed's room is on fire.")]
        public bool RejectedBedRoomHasFire { get; set; }
    }

    // The Self-tend option after the write: the state it now has, and whether
    // this call is what changed it (ticket #3279).
    public class SelfTendResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("Whether self-tend is on now.")]
        public bool SelfTend { get; set; }
        [ParameterDescription("Whether the call actually changed the setting.")]
        public bool Changed { get; set; }
    }

    // The prisoner's interaction mode after the change, with the numbers a
    // recruit attempt works against.
    public class PrisonerModeResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The interaction mode the game now uses.")]
        public string Mode { get; set; }
        [ParameterDescription("The mode's label, as the game shows it.")]
        public string ModeLabel { get; set; }
        [ParameterDescription("The prisoner's resistance left before recruitment.")]
        public float Resistance { get; set; }
        [ParameterDescription("The prisoner's will left before recruitment.")]
        public float Will { get; set; }
        [ParameterDescription("Whether the game allows recruiting the pawn.")]
        public bool Recruitable { get; set; }
    }
}
