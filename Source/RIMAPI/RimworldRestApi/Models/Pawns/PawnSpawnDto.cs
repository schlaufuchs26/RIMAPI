using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class PawnSpawnRequestDto
    {
        // Definitions
        [ParameterDescription("The pawn kind's def name, e.g. \"Muffalo\".")]
        public string PawnKind { get; set; } = "Colonist"; // Default to Colonist
        [ParameterDescription("The faction to spawn the pawn in, by def name.")]
        public string Faction { get; set; } = "PlayerColony"; // Default to Player
        [ParameterDescription("The xenotype's def name, e.g. \"Baseline\".")]
        public string Xenotype { get; set; } // Biotech DLC

        // Biological details
        [ParameterDescription("The gender to give the spawned pawn.")]
        public string Gender { get; set; } // Male, Female, None
        [ParameterDescription("The spawned pawn's biological age in years.")]
        public float? BiologicalAge { get; set; }
        [ParameterDescription("The spawned pawn's chronological age in years.")]
        public float? ChronologicalAge { get; set; }

        // Other details
        [ParameterDescription("Whether the pawn may spawn dead.")]
        public bool AllowDead { get; set; } = false;
        [ParameterDescription("Whether the pawn may spawn downed.")]
        public bool AllowDowned { get; set; } = false;
        [ParameterDescription("Whether the game may invent relations for it.")]
        public bool CanGeneratePawnRelations { get; set; } = true;
        [ParameterDescription("Whether the pawn must be able to fight.")]
        public bool MustBeCapableOfViolence { get; set; } = false;
        [ParameterDescription("Whether the pawn may be gay.")]
        public bool AllowGay { get; set; } = false;
        [ParameterDescription("Whether the pawn may spawn pregnant.")]
        public bool AllowPregnant { get; set; } = false;
        [ParameterDescription("Whether the pawn may carry food.")]
        public bool AllowFood { get; set; } = false;
        [ParameterDescription("Whether the pawn may spawn with addictions.")]
        public bool AllowAddictions { get; set; } = false;
        [ParameterDescription("Whether the pawn counts as a colony inhabitant.")]
        public bool Inhabitant { get; set; } = false;

        // Optional override name
        [ParameterDescription("The spawned pawn's first name.")]
        public string FirstName { get; set; }
        [ParameterDescription("The spawned pawn's last name.")]
        public string LastName { get; set; }
        [ParameterDescription("The spawned pawn's nickname.")]
        public string NickName { get; set; }

        // Location
        public string MapId { get; set; }
        public PositionDto Position { get; set; } // Helper class for X,Y,Z
    }

    public class PawnSpawnDto
    {
        [ParameterDescription("The spawned pawn's numeric id.")]
        public int PawnId { get; set; }
        public string Name { get; set; }
    }
}

