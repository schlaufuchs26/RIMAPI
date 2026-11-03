using System.Collections.Generic;
using RIMAPI.Core;

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
        [ParameterDescription("False when the founding asked for no site at all.")]
        public bool Wished { get; set; }
        [ParameterDescription("The world tile the choice picks.")]
        public int Tile { get; set; }
        [ParameterDescription("The biome of that tile.")]
        public string Biome { get; set; }
        [ParameterDescription("How hilly that tile is.")]
        public string Hilliness { get; set; }
        [ParameterDescription("The tile's average temperature, in Celsius.")]
        public float Temperature { get; set; }
        /// <summary>Days of the game's 60-day year in which plants grow.</summary>
        [ParameterDescription("Days of the game's 60-day year in which plants grow.")]
        public int GrowingDays { get; set; }
        [ParameterDescription("Whether a road crosses the tile.")]
        public bool Road { get; set; }
        [ParameterDescription("Whether a river crosses the tile.")]
        public bool River { get; set; }
        /// <summary>Other factions' settlements within the wished radius.</summary>
        [ParameterDescription("Other factions' settlements within the wished radius.")]
        public int SettlementsNear { get; set; }
        [ParameterDescription("Whether the tile carries pollution.")]
        public bool Polluted { get; set; }
        /// <summary>Penalty of the chosen tile; 0 means every wish was met.</summary>
        [ParameterDescription("Penalty of the chosen tile; 0 means every wish was met.")]
        public int Score { get; set; }
        /// <summary>0 for the best tile, 1 for the second best, and so on.</summary>
        [ParameterDescription("0 for the best tile, 1 for the second best, and so on.")]
        public int Rank { get; set; }
        /// <summary>How many tiles of the planet met every wish.</summary>
        [ParameterDescription("How many tiles of the planet met every wish.")]
        public int FullMatches { get; set; }
        [ParameterDescription("How many tiles the search looked at.")]
        public int TilesConsidered { get; set; }
        [ParameterDescription("The wishes the search weighed for this choice.")]
        public List<LandingCriterionDto> Criteria { get; set; }
        [ParameterDescription("The runner-up tiles the search kept.")]
        public List<LandingCandidateDto> Alternatives { get; set; }
        /// <summary>One line per wish the chosen tile does not meet.</summary>
        [ParameterDescription("One line per wish the chosen tile does not meet.")]
        public List<string> Unmet { get; set; }
        /// <summary>The scenario the colony was founded under, as the game holds it.</summary>
        [ParameterDescription("The scenario the colony was founded under, as the game holds it.")]
        public string Scenario { get; set; }
        [ParameterDescription("The scenario the landing belongs to, as the game names it.")]
        public string ScenarioLabel { get; set; }
    }

    public class LandingCriterionDto
    {
        public string Name { get; set; }
        [ParameterDescription("What the player asked for, e.g. a warm tile.")]
        public string Wish { get; set; }
        [ParameterDescription("What the tile actually offers.")]
        public string Got { get; set; }
        [ParameterDescription("Whether the tile meets the wish.")]
        public bool Met { get; set; }
        [ParameterDescription("How much the miss cost the tile's score.")]
        public int Penalty { get; set; }
    }

    public class LandingCandidateDto
    {
        [ParameterDescription("The candidate tile's id.")]
        public int Tile { get; set; }
        [ParameterDescription("The candidate's biome name.")]
        public string Biome { get; set; }
        [ParameterDescription("How hilly the candidate is.")]
        public string Hilliness { get; set; }
        [ParameterDescription("The candidate's average temperature, in Celsius.")]
        public float Temperature { get; set; }
        [ParameterDescription("How many days a year plants grow there.")]
        public int GrowingDays { get; set; }
        [ParameterDescription("Whether a road crosses it.")]
        public bool Road { get; set; }
        [ParameterDescription("Whether a river crosses it.")]
        public bool River { get; set; }
        [ParameterDescription("How many settlements stand near it.")]
        public int SettlementsNear { get; set; }
        [ParameterDescription("The score the search gave it.")]
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
