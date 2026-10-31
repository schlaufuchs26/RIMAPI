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
    }

    // What an arrest order did: the job the game got (Arrest or Capture), the
    // prisoner bed it walks to, and whether the arrestor was drafted for it.
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
    }

    // What a rescue order did: the job the game got, the bed it carries the
    // pawn to, and whether the pawn is a guest of the colony afterwards (a
    // rescued pod survivor becomes one, a rescued colonist stays a colonist).
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
