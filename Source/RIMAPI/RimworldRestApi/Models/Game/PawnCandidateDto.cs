using System.Collections.Generic;

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
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        // Gender is "f" or "m": two bytes, and a colony of one sex is a
        // decision a run may want to make on purpose.
        public string Gender { get; set; }
        public List<PawnCandidateSkillDto> Skills { get; set; }
        public List<string> Traits { get; set; }
        public List<string> Incapable { get; set; }
        public List<string> Health { get; set; }
    }

    public class PawnCandidateSkillDto
    {
        public string Name { get; set; }
        public int Level { get; set; }
        // Passion is the game's own enum: 0 none, 1 minor, 2 major.
        public int Passion { get; set; }
    }

    // CandidateBatchDto is what GET /api/v1/game/start/candidates answers.
    // State is "none" (no batch was asked for), "generating" (the world and
    // the pawns are being built), "ready" or "failed" with Problem set: a
    // long event cannot answer its caller, so the batch is polled.
    public class CandidateBatchDto
    {
        public string State { get; set; }
        public string Seed { get; set; }
        public string Scenario { get; set; }
        public string ScenarioLabel { get; set; }
        // Count is how many candidates the batch holds; PawnCount is how many
        // colonists the scenario starts with, so a run knows how many ids its
        // pick needs.
        public int Count { get; set; }
        public int PawnCount { get; set; }
        public List<PawnCandidateDto> Candidates { get; set; }
        public string Problem { get; set; }
    }
}
