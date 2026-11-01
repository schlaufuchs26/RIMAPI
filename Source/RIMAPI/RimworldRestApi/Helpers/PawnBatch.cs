using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Models;
using RimWorld;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The candidate batch behind a founding (fox ticket #3924). The game's
    /// setup screen lets a human reroll the starting colonists until the trio
    /// looks right; a run needs the same lever, because a colony founded on
    /// whatever luck handed it can have no cook, no doctor and two pawns who
    /// cannot do dumb labour.
    ///
    /// The batch is drawn from the world seed alone (PawnBatchRules.SeedOf), so
    /// the same seed builds the same candidates twice. That is what lets the
    /// start call rebuild the list and install the picked pawns as objects,
    /// without carrying pawns between two requests.
    /// </summary>
    public static class PawnBatch
    {
        private static CandidateBatchDto last;

        /// <summary>
        /// What the last founding installed: the picked candidates as
        /// "#7 Mia (34), #11 Tam (31)", or the reason the picks were not
        /// installed. Empty when the founding named no picks. The game state
        /// carries both, so a founding answer can show which colonists it got.
        /// </summary>
        public static string PickedNote = "";

        public static string PickProblem = "";

        public static CandidateBatchDto Last => last;

        public static bool Generating => last != null && last.State == "generating";

        /// <summary>
        /// Forgets the batch. A world that is replaced or left behind makes its
        /// candidates stale, and a run that reads one would pick ids that no
        /// later founding can reproduce.
        /// </summary>
        public static void Reset()
        {
            last = null;
        }

        /// <summary>
        /// Marks the batch as running. The long event cannot answer its caller,
        /// so the answer is polled; this is what the first poll reads.
        /// </summary>
        public static void Begin(NewGameStartRequestDto request, int count)
        {
            last = new CandidateBatchDto
            {
                State = "generating",
                Seed = SeedString(request),
                Count = count,
                Candidates = new List<PawnCandidateDto>(),
            };
        }

        /// <summary>
        /// Draws the candidates. Call it inside the long event, after
        /// GamePlayHelper.InitGameFromConfiguration built the world: the player
        /// faction and the ideoligion exist then, and a candidate is only worth
        /// anything if it is generated under the colony's own faction.
        /// </summary>
        public static void Generate(NewGameStartRequestDto request, int count)
        {
            try
            {
                string seed = SeedString(request);
                List<PawnCandidateDto> candidates = new List<PawnCandidateDto>();
                // A fixed RNG state around the batch: the pawn stream depends on
                // the world seed and nothing else, so the start call rebuilds
                // the same candidates from the same seed.
                Rand.PushState(PawnBatchRules.SeedOf(seed));
                try
                {
                    CanonicalizeSolidNames();
                    for (int i = 0; i < count; i++)
                    {
                        candidates.Add(Describe(DrawPawn(seed, i + 1), i + 1));
                    }
                }
                finally
                {
                    Rand.PopState();
                }
                last = new CandidateBatchDto
                {
                    State = "ready",
                    Seed = seed,
                    Scenario = LandingSitePick.ScenarioDefName ?? "",
                    ScenarioLabel = LandingSitePick.ScenarioLabel ?? "",
                    Count = candidates.Count,
                    PawnCount = Find.GameInitData?.startingPawnCount ?? 0,
                    Candidates = candidates,
                };
            }
            catch (Exception ex)
            {
                last = new CandidateBatchDto
                {
                    State = "failed",
                    Seed = SeedString(request),
                    Problem = ex.GetType().Name + ": " + ex.Message,
                    Candidates = new List<PawnCandidateDto>(),
                };
            }
        }

        /// <summary>
        /// Replaces the scenario's own random colonists with the picked ones.
        /// Call it after Find.Scenario.PostIdeoChosen(), which is what generates
        /// the vanilla trio and sets the count the scenario starts with.
        ///
        /// The candidates are rebuilt from the same seed, so the ids the batch
        /// answer printed still name the same pawns, and only the highest picked
        /// id has to be drawn. A count that does not match the scenario is
        /// reported and leaves the vanilla trio: a colony is never founded with
        /// half a pick.
        /// </summary>
        public static void Install(NewGameStartRequestDto request)
        {
            PickedNote = "";
            PickProblem = "";
            List<int> picks = request.StartingPawns;
            if (picks == null || picks.Count == 0)
                return;
            GameInitData data = Find.GameInitData;
            int wanted = data?.startingPawnCount ?? 0;
            string problem = PawnBatchRules.PicksProblem(picks, 0);
            if (problem == "")
            {
                if (picks.Count != wanted)
                    problem = $"the scenario starts {wanted} colonists but the pick names {picks.Count}.";
            }
            if (problem != "")
            {
                PickProblem = problem;
                Log.Warning("[RIMAPI] newcolony picks not installed: " + problem);
                return;
            }
            try
            {
                List<Pawn> chosen = new List<Pawn>();
                List<string> notes = new List<string>();
                int highest = picks.Max();
                string seed = SeedString(request);
                Rand.PushState(PawnBatchRules.SeedOf(seed));
                try
                {
                    CanonicalizeSolidNames();
                    for (int id = 1; id <= highest; id++)
                    {
                        Pawn pawn = DrawPawn(seed, id);
                        if (!picks.Contains(id))
                            continue;
                        chosen.Add(pawn);
                        notes.Add($"#{id} {pawn.LabelShort} ({pawn.ageTracker.AgeBiologicalYears})");
                    }
                }
                finally
                {
                    Rand.PopState();
                }
                data.startingAndOptionalPawns.Clear();
                data.startingAndOptionalPawns.AddRange(chosen);
                data.startingPawnCount = chosen.Count;
                PickedNote = string.Join(", ", notes);
            }
            catch (Exception ex)
            {
                PickProblem = ex.GetType().Name + ": " + ex.Message;
                Log.Warning("[RIMAPI] newcolony picks failed: " + PickProblem);
            }
        }

        /// <summary>
        /// One candidate pawn, drawn from its own RNG state (PawnSeed), so the
        /// pawn at an id does not move when a name redraw changes how many
        /// values the pawn before it consumed.
        /// </summary>
        private static Pawn DrawPawn(string seed, int id)
        {
            Rand.PushState(PawnBatchRules.PawnSeed(seed, id));
            try
            {
                return StartingPawnUtility.NewGeneratedStartingPawn();
            }
            finally
            {
                Rand.PopState();
            }
        }

        /// <summary>
        /// Puts the game's solid name lists back in one canonical order before a
        /// draw. The name generator shuffles those static lists in place, so a
        /// second batch starts from the order the first one left and picks other
        /// names; sorting first makes the same seed give the same names.
        /// </summary>
        private static void CanonicalizeSolidNames()
        {
            foreach (GenderPossibility gp in Enum.GetValues(typeof(GenderPossibility)))
            {
                List<NameTriple> names = PawnNameDatabaseSolid.GetListForGender(gp);
                if (names == null || names.Count == 0)
                    continue;
                List<NameTriple> sorted = names.OrderBy(n => n.ToString(), StringComparer.Ordinal).ToList();
                names.Clear();
                names.AddRange(sorted);
            }
        }

        /// <summary>
        /// The seed the batch is drawn from: the world seed the game actually
        /// builds the planet with, which the start call lowercases.
        /// </summary>
        private static string SeedString(NewGameStartRequestDto request)
        {
            string seed = (request?.WorldSeed ?? "").ToLowerInvariant();
            return seed == "" ? "fox" : seed;
        }

        /// <summary>
        /// One candidate as the answer prints it. The filters are the ones a
        /// pick needs: the skills that carry a level or a passion, the traits
        /// the character card shows, the incapabilities the game derives from
        /// backstory, traits and genes, and the bad health conditions.
        /// </summary>
        private static PawnCandidateDto Describe(Pawn pawn, int id)
        {
            return new PawnCandidateDto
            {
                Id = id,
                Name = pawn.LabelShort,
                Age = pawn.ageTracker?.AgeBiologicalYears ?? 0,
                Gender = pawn.gender == Gender.Female ? "f" : "m",
                Skills = Skills(pawn),
                Traits = Traits(pawn),
                Incapable = PawnHelper.GetIncapableOfWorkTags(pawn) ?? new List<string>(),
                Health = Health(pawn),
            };
        }

        private static List<PawnCandidateSkillDto> Skills(Pawn pawn)
        {
            List<PawnCandidateSkillDto> skills = new List<PawnCandidateSkillDto>();
            if (pawn.skills?.skills == null)
                return skills;
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                if (skill?.def == null || skill.TotallyDisabled)
                    continue;
                int passion = (int)skill.passion;
                if (!PawnBatchRules.WorthShowing(skill.Level, passion))
                    continue;
                skills.Add(new PawnCandidateSkillDto
                {
                    // LabelCap is the game's own capitalised skill label
                    // ("Social"), the form its skill list shows.
                    Name = skill.def.LabelCap,
                    Level = skill.Level,
                    Passion = passion,
                });
            }
            return skills;
        }

        private static List<string> Traits(Pawn pawn)
        {
            List<string> traits = new List<string>();
            if (pawn.story?.traits?.allTraits == null)
                return traits;
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (trait?.def == null || trait.Suppressed)
                    continue;
                traits.Add(trait.Label);
            }
            return traits;
        }

        private static List<string> Health(Pawn pawn)
        {
            List<string> health = new List<string>();
            if (pawn.health?.hediffSet?.hediffs == null)
                return health;
            HashSet<string> seen = new HashSet<string>();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff?.def == null || !hediff.def.isBad)
                    continue;
                // Two asthma conditions (one per lung) read as one line: a
                // pick cares that the pawn has asthma, not how many copies.
                if (seen.Add(hediff.Label))
                    health.Add(hediff.Label);
            }
            return health;
        }
    }
}
