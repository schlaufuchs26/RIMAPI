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
    }


    public class PawnDetailedRequestDto
    {
        public PawnDto Pawn { get; set; }
        public PawnDetailedDto Detailes { get; set; }
    }
}
