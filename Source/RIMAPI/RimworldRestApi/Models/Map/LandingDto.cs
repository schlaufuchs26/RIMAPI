using System.Collections.Generic;

namespace RIMAPI.Models
{
    // What a founding's site wish picked (fox ticket #3930): the tile the
    // colony landed on, how it compares to each line of the wish, the wishes it
    // could not meet, and the next two candidates. The search fills it inside
    // the world generation, and GET /api/v1/world/landing hands it back
    // afterwards, because the start call itself answers before the world
    // exists.
    public class LandingChoiceDto
    {
        /// <summary>False when the founding asked for no site at all.</summary>
        public bool Wished { get; set; }
        public int Tile { get; set; }
        public string Biome { get; set; }
        public string Hilliness { get; set; }
        public float Temperature { get; set; }
        /// <summary>Days a year whose average temperature allows plant growth.</summary>
        public int GrowingDays { get; set; }
        public bool Road { get; set; }
        public bool River { get; set; }
        /// <summary>Other factions' settlements within the wished radius.</summary>
        public int SettlementsNear { get; set; }
        public bool Polluted { get; set; }
        /// <summary>Penalty of the chosen tile; 0 means every wish was met.</summary>
        public int Score { get; set; }
        /// <summary>0 for the best tile, 1 for the second best, and so on.</summary>
        public int Rank { get; set; }
        /// <summary>How many tiles of the planet met every wish.</summary>
        public int FullMatches { get; set; }
        public int TilesConsidered { get; set; }
        public List<LandingCriterionDto> Criteria { get; set; }
        public List<LandingCandidateDto> Alternatives { get; set; }
        /// <summary>One line per wish the chosen tile does not meet.</summary>
        public List<string> Unmet { get; set; }
        /// <summary>The scenario the colony was founded under, as the game holds it.</summary>
        public string Scenario { get; set; }
        public string ScenarioLabel { get; set; }
    }

    public class LandingCriterionDto
    {
        public string Name { get; set; }
        public string Wish { get; set; }
        public string Got { get; set; }
        public bool Met { get; set; }
        public int Penalty { get; set; }
    }

    public class LandingCandidateDto
    {
        public int Tile { get; set; }
        public string Biome { get; set; }
        public string Hilliness { get; set; }
        public float Temperature { get; set; }
        public int GrowingDays { get; set; }
        public bool Road { get; set; }
        public bool River { get; set; }
        public int SettlementsNear { get; set; }
        public int Score { get; set; }
    }

    // The scenarios the installed game offers (fox ticket #3930), so a caller
    // can name one without guessing a defName.
    public class ScenarioOptionDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
    }
}
