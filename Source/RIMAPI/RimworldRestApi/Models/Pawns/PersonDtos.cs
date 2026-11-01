using System.Collections.Generic;

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
        public int Id { get; set; }
        public string Name { get; set; }

        // wild, prisoner, slave, guest, hostile, factionless or neutral.
        public string Kind { get; set; }
        public string Faction { get; set; }
        public bool Downed { get; set; }
        public float Health { get; set; }

        // Tameable is TameUtility.CanTame, the game's own check.
        public bool Tameable { get; set; }

        // The prisoner fields are -1 (resistance, will) and false (recruitable)
        // for a pawn that is not a guest.
        public float Resistance { get; set; }
        public float Will { get; set; }
        public bool Recruitable { get; set; }
        public string InteractionMode { get; set; }
        public PositionDto Position { get; set; }

        // A stranger who arrived to be talked into joining (the Anomaly creep
        // joiner behind the "Lone genius" letter, ticket #3275). Chattable is
        // the game's own offer: he still stands on his entry lord, so a colonist
        // can chat with him now; after the talk the offer letter decides.
        public bool CreepJoiner { get; set; }
        public bool Chattable { get; set; }
    }

    // What a chat order did: the colonist who walks over to talk, or that no
    // colonist could take it now (ticket #3275).
    public class ChatResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        public int ChatterId { get; set; }
        public string Chatter { get; set; }
        public string Job { get; set; }
        public bool Queued { get; set; }
        public string Note { get; set; }
    }

    // One colonist the game's own check turned down for an order, with the
    // check that stopped them (fox #3625). A skill gate reads as "needs
    // Animals 7"; a free handler tomorrow does not clear it, so the answer
    // must not read as a wait.
    public class HandlerRefusalDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Skill { get; set; }
        public string SkillLabel { get; set; }
        public string Reason { get; set; }
    }

    // What a tame order did: which handler took it, or that the designation
    // stands and no handler is free right now.
    public class TameResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        public int HandlerId { get; set; }
        public string Handler { get; set; }
        public string Job { get; set; }
        public bool Queued { get; set; }
        public string Note { get; set; }
        // Every handler candidate the game's check turned down, best first
        // (fox #3625). Empty when a handler took the job or on a build older
        // than #3625.
        public List<HandlerRefusalDto> Refusals { get; set; } = new List<HandlerRefusalDto>();
    }

    // What a slaughter order did: which handler took it, or that the Slaughter
    // designation stands and no handler is free right now (fox ticket #3507).
    public class SlaughterResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        public int HandlerId { get; set; }
        public string Handler { get; set; }
        public string Job { get; set; }
        public bool Queued { get; set; }
        public string Note { get; set; }
        // Every handler candidate the game's check turned down, best first
        // (fox #3625).
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
        public int ArrestorId { get; set; }
        public string Arrestor { get; set; }
        public string Job { get; set; }
        public int BedId { get; set; }
        public string Bed { get; set; }
        public bool Drafted { get; set; }
        public int BedRoomId { get; set; }
        public string BedRoomLabel { get; set; }
        public float BedRoomTempC { get; set; }
        public bool BedRoomHasFire { get; set; }
        // The bed's cell is under no roof (fox ticket #3622). A bed with no
        // room of its own carries "none" as its role label, which is not a
        // room name; the roof grid is what tells outdoors from a roleless room.
        public bool BedOutdoors { get; set; }
        // Why the bed is no prison cell, empty when it is one (fox ticket
        // #3621): a cool prisoner bed on open ground passes every temperature
        // check and still lets the prisoner escape in the same minute.
        public string BedNoCellReason { get; set; }
        // The bed the game's own lookup picked first, when the tool took a safer
        // one instead; zero when the game's choice was the one used.
        public int RejectedBedId { get; set; }
        public string RejectedBed { get; set; }
        // The refused bed's own room (ticket #3623): the answer names where the
        // bed the game picked first stands, not only how hot it is.
        public string RejectedBedRoomLabel { get; set; }
        public float RejectedBedTempC { get; set; }
        public bool RejectedBedRoomHasFire { get; set; }
        // Why the refused bed was no cell either (ticket #3621); empty when it
        // was a real cell and only unsafe.
        public string RejectedBedNoCellReason { get; set; }
    }

    // What a rescue order did: the job the game got, the bed it carries the
    // pawn to, and whether the pawn is a guest of the colony afterwards (a
    // rescued pod survivor becomes one, a rescued colonist stays a colonist).
    public class FeedResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        public int FeederId { get; set; }
        public string Feeder { get; set; }
        public string Job { get; set; }
        // The meal the job carries and the bed the patient lies in (ticket
        // #3445): a patient who is not in a bed cannot be fed at all, so the
        // answer names the bed it happened in.
        public int MealId { get; set; }
        public string Meal { get; set; }
        public int BedId { get; set; }
        public string Bed { get; set; }
        public bool Queued { get; set; }
    }

    public class RescueResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        public int RescuerId { get; set; }
        public string Rescuer { get; set; }
        public string Job { get; set; }
        public int BedId { get; set; }
        public string Bed { get; set; }
        public bool BedForPrisoners { get; set; }
        public bool Guest { get; set; }
        // The bed's room as it stands now (ticket #3383): a bed in a room above
        // 50 C or with a fire in it is no destination for a carried patient, so
        // the answer has to name what the room reads.
        public int BedRoomId { get; set; }
        public string BedRoomLabel { get; set; }
        public float BedRoomTempC { get; set; }
        public bool BedRoomHasFire { get; set; }
        // The bed's cell is under no roof (fox ticket #3622), the answer to a
        // role label the game prints as "none".
        public bool BedOutdoors { get; set; }
        // The bed the game's own lookup picked first, when the tool took a safer
        // one instead; zero when the game's choice was the one used.
        public int RejectedBedId { get; set; }
        public string RejectedBed { get; set; }
        // The refused bed's own room (ticket #3623), as in ArrestResultDto.
        public string RejectedBedRoomLabel { get; set; }
        public float RejectedBedTempC { get; set; }
        public bool RejectedBedRoomHasFire { get; set; }
    }

    // The Self-tend option after the write: the state it now has, and whether
    // this call is what changed it (ticket #3279).
    public class SelfTendResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        public bool SelfTend { get; set; }
        public bool Changed { get; set; }
    }

    // The prisoner's interaction mode after the change, with the numbers a
    // recruit attempt works against.
    public class PrisonerModeResultDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        public string Mode { get; set; }
        public string ModeLabel { get; set; }
        public float Resistance { get; set; }
        public float Will { get; set; }
        public bool Recruitable { get; set; }
    }
}
