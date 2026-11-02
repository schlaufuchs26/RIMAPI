using System.Collections.Generic;

namespace RIMAPI.Models
{
    public class PawnDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public float Health { get; set; }
        public float Mood { get; set; }
        public float Hunger { get; set; }
        public PositionDto Position { get; set; }

        // AllowedArea is the area the player restricted this pawn to, and the
        // one answer that explains a colonist who walks home past a marked
        // tree and idles: the game never gives her work outside that area, and
        // nothing else in the reads said so (fox ticket #3404). Null means the
        // pawn is unrestricted, which is the game's default.
        public string AllowedArea { get; set; }
        public int AllowedAreaCells { get; set; }

        // CarriedBy names the pawn carrying this one, empty when nobody does
        // (fox ticket #3665). A carried pawn is detached from the map's thing
        // lists, so a read printed his last cell and his old job while a raider
        // walked him off the map, and every order on him answered "not found".
        public string CarriedBy { get; set; }
    }

    // AllowedAreaDto is what setting a pawn's allowed area answers (fox ticket
    // #3645): the pawn and the area it is restricted to afterwards, in the
    // same shape the colonists read sends. A null AllowedArea means
    // unrestricted, and Note says why the change did not stick.
    public class AllowedAreaDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
        public string AllowedArea { get; set; }
        public int AllowedAreaCells { get; set; }
        public string Note { get; set; }
    }

    // ReservedByDto is one pawn that holds a reservation on a colonist and the
    // job it holds him for (fox ticket #4172). The tend order could learn a held
    // patient only from its own refusal, so the read carries the same fact the
    // game's reservation manager knows.
    public class ReservedByDto
    {
        public int ClaimantId { get; set; }
        public string Claimant { get; set; }
        public string Job { get; set; }
    }

    public class PawnDetailedDto
    {
        public float BodySize { get; set; }
        public float Sleep { get; set; }
        public float Comfort { get; set; }
        public float Beauty { get; set; }
        public float Joy { get; set; }
        public float Energy { get; set; }
        public float DrugsDesire { get; set; }
        public float SurroundingBeauty { get; set; }
        public float FreshAir { get; set; }
        public WorkInfoDto WorkInfo { get; set; }
        public MoodInfoDto MoodInfo { get; set; }
        public PoliciesInfoDto PoliciesInfo { get; set; }
        public MedicalInfoDto MedicalInfo { get; set; }
        public SocialInfoDto SocialInfo { get; set; }

        // ReservedBy names the pawns that hold a reservation on this colonist
        // (fox ticket #4172). An empty list means nobody holds him; a bridge
        // from before the field sends nothing at all, and the caller must not
        // read that as a free pawn.
        public List<ReservedByDto> ReservedBy { get; set; }
    }

    public class ApiV1PawnDetailedDto
    {
        public PawnDto Colonist { get; set; }
        public float BodySize { get; set; }
        public float Sleep { get; set; }
        public float Comfort { get; set; }
        public float Beauty { get; set; }
        public float Joy { get; set; }
        public float Energy { get; set; }
        public float DrugsDesire { get; set; }
        public float SurroundingBeauty { get; set; }
        public float FreshAir { get; set; }
        public WorkInfoDto ColonistWorkInfo { get; set; }
        public MoodInfoDto MoodInfo { get; set; }
        public PoliciesInfoDto PoliciesInfo { get; set; }
        public MedicalInfoDto ColonistMedicalInfo { get; set; }
        public SocialInfoDto SocialInfo { get; set; }

        // ReservedBy names the pawns that hold a reservation on this colonist
        // (fox ticket #4172), so a digest or an order can see a held patient
        // before a call goes out instead of learning it from a refusal.
        public List<ReservedByDto> ReservedBy { get; set; }
    }


    public class PawnDetailedRequestDto
    {
        public PawnDto Pawn { get; set; }
        public PawnDetailedDto Detailes { get; set; }
    }
}
