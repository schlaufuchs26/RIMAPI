using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class PawnDto
    {
        [ParameterDescription("The pawn's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The pawn's gender, as the game records it.")]
        public string Gender { get; set; }
        [ParameterDescription("The pawn's biological age in years.")]
        public int Age { get; set; }
        [ParameterDescription("The pawn's overall health, 0 to 1.")]
        public float Health { get; set; }
        [ParameterDescription("The pawn's current mood, 0 to 1.")]
        public float Mood { get; set; }
        [ParameterDescription("The pawn's food level, 0 to 1.")]
        public float Hunger { get; set; }
        public PositionDto Position { get; set; }

        // AllowedArea is the area the player restricted this pawn to, and the
        // one answer that explains a colonist who walks home past a marked
        // tree and idles: the game never gives her work outside that area, and
        // nothing else in the reads said so (fox ticket #3404). Null means the
        // pawn is unrestricted, which is the game's default.
        [ParameterDescription("The allowed area the pawn is restricted to.")]
        public string AllowedArea { get; set; }
        [ParameterDescription("The cells that make up the allowed area.")]
        public int AllowedAreaCells { get; set; }

        // CarriedBy names the pawn carrying this one, empty when nobody does
        // (fox ticket #3665). A carried pawn is detached from the map's thing
        // lists, so a read printed his last cell and his old job while a raider
        // walked him off the map, and every order on him answered "not found".
        [ParameterDescription("The pawn carrying this one, when it is downed.")]
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
        [ParameterDescription("The allowed area's name, as the area list shows it.")]
        public string AllowedArea { get; set; }
        [ParameterDescription("The cells that make up the allowed area.")]
        public int AllowedAreaCells { get; set; }
        [ParameterDescription("The player's own note on the area.")]
        public string Note { get; set; }
    }

    // ReservedByDto is one pawn that holds a reservation on a colonist and the
    // job it holds him for (fox ticket #4172). The tend order could learn a held
    // patient only from its own refusal, so the read carries the same fact the
    // game's reservation manager knows.
    public class ReservedByDto
    {
        [ParameterDescription("The numeric id of the pawn holding the reservation.")]
        public int ClaimantId { get; set; }
        [ParameterDescription("The name of the pawn holding the reservation.")]
        public string Claimant { get; set; }
        [ParameterDescription("The job the reservation is held for.")]
        public string Job { get; set; }
    }

    public class PawnDetailedDto
    {
        [ParameterDescription("The pawn's body size multiplier.")]
        public float BodySize { get; set; }
        [ParameterDescription("The pawn's rest need, 0 to 1.")]
        public float Sleep { get; set; }
        [ParameterDescription("The pawn's comfort need, 0 to 1.")]
        public float Comfort { get; set; }
        [ParameterDescription("How beautiful the pawn's surroundings are.")]
        public float Beauty { get; set; }
        [ParameterDescription("The pawn's joy need, 0 to 1.")]
        public float Joy { get; set; }
        [ParameterDescription("The pawn's energy need, 0 to 1.")]
        public float Energy { get; set; }
        [ParameterDescription("The pawn's chemical desire, 0 to 1.")]
        public float DrugsDesire { get; set; }
        [ParameterDescription("The beauty of the room the pawn is in.")]
        public float SurroundingBeauty { get; set; }
        [ParameterDescription("Whether the pawn breathes fresh air.")]
        public float FreshAir { get; set; }
        [ParameterDescription("The pawn's skills, traits and current job.")]
        public WorkInfoDto WorkInfo { get; set; }
        [ParameterDescription("The pawn's mood and the thoughts behind it.")]
        public MoodInfoDto MoodInfo { get; set; }
        [ParameterDescription("The pawn's food policy and hostility response.")]
        public PoliciesInfoDto PoliciesInfo { get; set; }
        [ParameterDescription("The pawn's health, conditions and bed.")]
        public MedicalInfoDto MedicalInfo { get; set; }
        [ParameterDescription("The pawn's relations and children.")]
        public SocialInfoDto SocialInfo { get; set; }

        // ReservedBy names the pawns that hold a reservation on this colonist
        // (fox ticket #4172). An empty list means nobody holds him; a bridge
        // from before the field sends nothing at all, and the caller must not
        // read that as a free pawn.
        [ParameterDescription("The reservation on the pawn, when one is held.")]
        public List<ReservedByDto> ReservedBy { get; set; }
    }

    public class ApiV1PawnDetailedDto
    {
        [ParameterDescription("The pawn's basic card.")]
        public PawnDto Colonist { get; set; }
        [ParameterDescription("The pawn's body size multiplier.")]
        public float BodySize { get; set; }
        [ParameterDescription("The pawn's rest need, 0 to 1.")]
        public float Sleep { get; set; }
        [ParameterDescription("The pawn's comfort need, 0 to 1.")]
        public float Comfort { get; set; }
        [ParameterDescription("How beautiful the pawn's surroundings are.")]
        public float Beauty { get; set; }
        [ParameterDescription("The pawn's joy need, 0 to 1.")]
        public float Joy { get; set; }
        [ParameterDescription("The pawn's energy need, 0 to 1.")]
        public float Energy { get; set; }
        [ParameterDescription("The pawn's chemical desire, 0 to 1.")]
        public float DrugsDesire { get; set; }
        [ParameterDescription("The beauty of the room the pawn is in.")]
        public float SurroundingBeauty { get; set; }
        [ParameterDescription("Whether the pawn breathes fresh air.")]
        public float FreshAir { get; set; }
        [ParameterDescription("The pawn's skills, traits and current job.")]
        public WorkInfoDto ColonistWorkInfo { get; set; }
        [ParameterDescription("The pawn's mood and the thoughts behind it.")]
        public MoodInfoDto MoodInfo { get; set; }
        [ParameterDescription("The pawn's food policy and hostility response.")]
        public PoliciesInfoDto PoliciesInfo { get; set; }
        [ParameterDescription("The pawn's health, conditions and bed.")]
        public MedicalInfoDto ColonistMedicalInfo { get; set; }
        [ParameterDescription("The pawn's relations and children.")]
        public SocialInfoDto SocialInfo { get; set; }

        // ReservedBy names the pawns that hold a reservation on this colonist
        // (fox ticket #4172), so a digest or an order can see a held patient
        // before a call goes out instead of learning it from a refusal.
        [ParameterDescription("The reservation on the pawn, when one is held.")]
        public List<ReservedByDto> ReservedBy { get; set; }
    }


    public class PawnDetailedRequestDto
    {
        [ParameterDescription("The pawn id or name to read.")]
        public PawnDto Pawn { get; set; }
        [ParameterDescription("The detail sections to include, comma-separated.")]
        public PawnDetailedDto Detailes { get; set; }
    }
}
