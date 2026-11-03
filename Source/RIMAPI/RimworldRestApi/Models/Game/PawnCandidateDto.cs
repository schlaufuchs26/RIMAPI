using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    // One colonist the game would hand a new colony (fox ticket #3924). The
    // setup pages let a human reroll the starting trio until it looks right;
    // this is the same lever for a run: a batch of candidates it can read and
    // then pick from. The summary carries what a pick is decided on: age, the
    // skills with their passion, the traits, the incapabilities the game
    // derives from backstory, traits and genes, and any bad health condition.
    public class PawnCandidateDto
    {
        // Id is 1-based and names the candidate in the batch answer, so a run
        // writes the same number back in `picks`.
        [ParameterDescription("Id is 1-based and names the candidate in the batch answer, so a run writes the same number back in `picks`.")]
        public int Id { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The candidate's age in years.")]
        public int Age { get; set; }
        // Gender is "f" or "m": two bytes, and a colony of one sex is a
        // decision a run may want to make on purpose.
        [ParameterDescription("Gender is \"f\" or \"m\": two bytes, and a colony of one sex is a decision a run may want to make on purpose.")]
        public string Gender { get; set; }
        [ParameterDescription("The candidate's skills, one entry each.")]
        public List<PawnCandidateSkillDto> Skills { get; set; }
        [ParameterDescription("The candidate's traits, one entry each.")]
        public List<string> Traits { get; set; }
        [ParameterDescription("The work the candidate cannot do.")]
        public List<string> Incapable { get; set; }
        [ParameterDescription("The candidate's health, as the game reports it.")]
        public List<string> Health { get; set; }
    }

    public class PawnCandidateSkillDto
    {
        public string Name { get; set; }
        [ParameterDescription("The skill's level, 0 to 20.")]
        public int Level { get; set; }
        // Passion is the game's own enum: 0 none, 1 minor, 2 major.
        [ParameterDescription("Passion is the game's own enum: 0 none, 1 minor, 2 major.")]
        public int Passion { get; set; }
    }

    // CandidateBatchDto is what GET /api/v1/game/start/candidates answers.
    // State is "none" (no batch was asked for), "generating" (the world and
    // the pawns are being built), "ready" or "failed" with Problem set: a
    // long event cannot answer its caller, so the batch is polled.
    public class CandidateBatchDto
    {
        [ParameterDescription("Where the batch stands: generating, ready or failed.")]
        public string State { get; set; }
        [ParameterDescription("The seed the candidates are generated from.")]
        public string Seed { get; set; }
        [ParameterDescription("The scenario's def name.")]
        public string Scenario { get; set; }
        [ParameterDescription("The scenario's name, as the game shows it.")]
        public string ScenarioLabel { get; set; }
        // Count is how many candidates the batch holds; PawnCount is how many
        // colonists the scenario starts with, so a run knows how many ids its
        // pick needs.
        public int Count { get; set; }
        [ParameterDescription("How many candidates the batch holds.")]
        public int PawnCount { get; set; }
        [ParameterDescription("The generated pawns the batch offers as its pick.")]
        public List<PawnCandidateDto> Candidates { get; set; }
        [ParameterDescription("What went wrong, when something did.")]
        public string Problem { get; set; }
    }
}
