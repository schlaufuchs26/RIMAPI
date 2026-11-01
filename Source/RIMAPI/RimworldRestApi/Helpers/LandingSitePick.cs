using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Models;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// The game-side half of the site wish (fox ticket #3930): reads the world
    /// grid the founding just generated, hands every landable tile to
    /// <see cref="LandingSiteRules"/>, and keeps the ranking so
    /// GET /api/v1/world/landing can answer after the colony is up. The start
    /// call returns before the world exists, so the choice cannot travel in its
    /// answer.
    /// </summary>
    public static class LandingSitePick
    {
        /// <summary>The choice of the last founding; null until one asked for a site.</summary>
        public static LandingChoiceDto Last { get; set; }

        /// <summary>
        /// The scenario the running colony was founded under. Kept here
        /// because a Scenario holds no readable def of its own, and a state
        /// read should still be able to name Lost Tribe.
        /// </summary>
        public static string ScenarioDefName { get; set; }
        public static string ScenarioLabel { get; set; }

        // The hilliness words a caller may write, on top of the enum's own
        // names (Flat, SmallHills, LargeHills, Mountainous). Keys are folded
        // the way Normalize folds them: no spaces, lower case.
        private static readonly Dictionary<string, string> HillinessWords = new Dictionary<string, string>
        {
            { "flat", "Flat" },
            { "smallhills", "SmallHills" },
            { "largehills", "LargeHills" },
            { "mountain", "Mountainous" },
            { "mountains", "Mountainous" },
            { "mountainous", "Mountainous" },
        };

        /// <summary>
        /// Why the request cannot be honoured, or "" when it can. Checked before
        /// the world is generated: a long event cannot answer a caller, so an
        /// unknown biome or scenario has to be refused on the way in.
        /// </summary>
        public static string Problem(NewGameStartRequestDto request)
        {
            if (request == null)
            {
                return "Request body is missing.";
            }
            string scenario = ScenarioProblem(request.Scenario);
            if (scenario != "")
            {
                return scenario;
            }
            if (!LandingSiteRules.HasWish(WishOf(request)))
            {
                return "";
            }
            if (!string.IsNullOrEmpty(request.StartingTile) && request.StartingTile != "auto")
            {
                return "A site wish and an explicit tile contradict each other: the wish picks the tile, so pass one of them.";
            }
            foreach (string name in request.SiteBiomes ?? new List<string>())
            {
                if (ResolveBiome(name) == null)
                {
                    return "Unknown biome '" + name + "'. The game's biomes are: " + BiomeNames() + ".";
                }
            }
            foreach (string name in request.SiteHilliness ?? new List<string>())
            {
                if (ResolveHilliness(name) == null)
                {
                    return "Unknown hilliness '" + name + "'. Use flat, small hills, large hills or mountainous.";
                }
            }
            if (request.SiteTemperatureMin.HasValue && request.SiteTemperatureMax.HasValue
                && request.SiteTemperatureMin.Value > request.SiteTemperatureMax.Value)
            {
                return "The temperature band is empty: its minimum is above its maximum.";
            }
            return "";
        }

        /// <summary>The scenario def the request names, or the game's default when it names none.</summary>
        public static ScenarioDef ResolveScenario(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return ScenarioDefOf.Crashlanded;
            }
            return DefDatabase<ScenarioDef>.GetNamedSilentFail(name)
                ?? DefDatabase<ScenarioDef>.AllDefsListForReading.FirstOrDefault(
                    def => Normalize(def.label) == Normalize(name));
        }

        /// <summary>The scenarios the installed game offers, for a caller that must pick one.</summary>
        public static List<ScenarioOptionDto> ScenarioOptions()
        {
            return DefDatabase<ScenarioDef>.AllDefsListForReading
                .Select(def => new ScenarioOptionDto
                {
                    DefName = def.defName,
                    Label = def.label,
                    Description = def.description,
                })
                .OrderBy(option => option.Label)
                .ToList();
        }

        /// <summary>The wish list the request describes, with the names resolved to defNames.</summary>
        public static LandingSiteRules.Wish WishOf(NewGameStartRequestDto request)
        {
            var wish = new LandingSiteRules.Wish
            {
                TemperatureMin = request.SiteTemperatureMin,
                TemperatureMax = request.SiteTemperatureMax,
                GrowingDaysMin = request.SiteGrowingDaysMin,
                Road = request.SiteRoad,
                River = request.SiteRiver,
                SettlementsMin = request.SiteSettlementsMin,
                SettlementRadius = request.SiteSettlementRadius,
            };
            foreach (string name in request.SiteBiomes ?? new List<string>())
            {
                BiomeDef def = ResolveBiome(name);
                if (def != null)
                {
                    wish.Biomes.Add(def.defName);
                }
            }
            foreach (string name in request.SiteHilliness ?? new List<string>())
            {
                string resolved = ResolveHilliness(name);
                if (resolved != null)
                {
                    wish.Hilliness.Add(resolved);
                }
            }
            return wish;
        }

        /// <summary>
        /// Picks the tile and keeps the ranking. Must run inside the world
        /// generation, after the world exists and before the map is built.
        /// </summary>
        public static LandingChoiceDto Choose(NewGameStartRequestDto request)
        {
            var choice = new LandingChoiceDto
            {
                Criteria = new List<LandingCriterionDto>(),
                Alternatives = new List<LandingCandidateDto>(),
                Unmet = new List<string>(),
                Tile = -1,
                Scenario = ScenarioDefName,
                ScenarioLabel = ScenarioLabel,
            };
            LandingSiteRules.Wish wish = WishOf(request);
            if (!LandingSiteRules.HasWish(wish))
            {
                Last = choice;
                return choice;
            }

            WorldGrid grid = Find.WorldGrid;
            int radius = wish.SettlementRadius > 0 ? wish.SettlementRadius : 6;
            List<Settlement> others = null;
            if (wish.SettlementsMin > 0)
            {
                others = Find.WorldObjects.Settlements
                    .Where(s => s.Faction != null && !s.Faction.IsPlayer)
                    .ToList();
            }

            var candidates = new List<LandingSiteRules.Candidate>(grid.TilesCount);
            for (int tileId = 0; tileId < grid.TilesCount; tileId++)
            {
                var tile = grid[tileId];
                BiomeDef biome = tile.PrimaryBiome;
                // A tile the game would not let a colony land on is no
                // candidate: an impassable biome (ocean) or a mountain wall
                // reads as a match on paper and fails at the landing.
                if (biome == null || biome.impassable || tile.hilliness == Hilliness.Impassable)
                {
                    continue;
                }
                var candidate = new LandingSiteRules.Candidate
                {
                    Tile = tileId,
                    Biome = biome.defName,
                    Hilliness = tile.hilliness.ToString(),
                    Temperature = tile.temperature,
                    GrowingDays = GrowingDays(tileId),
                    Road = tile.Roads != null && tile.Roads.Count > 0,
                    River = tile.Rivers != null && tile.Rivers.Count > 0,
                    Polluted = ModsConfig.BiotechActive && tile.pollution > 0f,
                };
                if (others != null)
                {
                    candidate.SettlementsNear = others.Count(s => grid.ApproxDistanceInTiles(tileId, s.Tile) <= radius);
                }
                candidates.Add(candidate);
            }

            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(candidates, wish, request.SiteVary);
            choice.Wished = ranking.Wished;
            choice.TilesConsidered = candidates.Count;
            choice.FullMatches = ranking.FullMatches;
            if (ranking.Chosen != null)
            {
                choice.Tile = ranking.Chosen.Tile;
                choice.Biome = ranking.Chosen.Biome;
                choice.Hilliness = ranking.Chosen.Hilliness;
                choice.Temperature = ranking.Chosen.Temperature;
                choice.GrowingDays = ranking.Chosen.GrowingDays;
                choice.Road = ranking.Chosen.Road;
                choice.River = ranking.Chosen.River;
                choice.SettlementsNear = ranking.Chosen.SettlementsNear;
                choice.Polluted = ranking.Chosen.Polluted;
                choice.Score = ranking.Score;
                choice.Rank = ranking.Rank;
                choice.Unmet = ranking.Unmet;
                choice.Criteria = ranking.Criteria.Select(line => new LandingCriterionDto
                {
                    Name = line.Name,
                    Wish = line.Wish,
                    Got = line.Got,
                    Met = line.Met,
                    Penalty = line.Penalty,
                }).ToList();
                choice.Alternatives = ranking.Alternatives.Select(candidate => new LandingCandidateDto
                {
                    Tile = candidate.Tile,
                    Biome = candidate.Biome,
                    Hilliness = candidate.Hilliness,
                    Temperature = candidate.Temperature,
                    GrowingDays = candidate.GrowingDays,
                    Road = candidate.Road,
                    River = candidate.River,
                    SettlementsNear = candidate.SettlementsNear,
                    Score = LandingSiteRules.Judge(candidate, wish).Sum(line => line.Penalty),
                }).ToList();
            }
            Last = choice;
            return choice;
        }

        /// <summary>
        /// The length of the tile's growing season in the game's own unit: days
        /// of the 60-day year in which plants grow. The answer is read from
        /// GrowingQuadrumsDescription, which is the line the world info shows
        /// ("40/60 days (1st of Aprimay - 11th of Septober)"), so a wish and the
        /// tile panel cannot disagree. A tile whose season the game describes
        /// in words instead of numbers reads 60 for all year and 0 for never.
        /// </summary>
        public static int GrowingDays(int tileId)
        {
            string description = Zone_Growing.GrowingQuadrumsDescription(tileId);
            if (string.IsNullOrEmpty(description))
            {
                return 0;
            }
            if (description.StartsWith("all year", StringComparison.OrdinalIgnoreCase))
            {
                return 60;
            }
            if (description.StartsWith("never", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }
            int slash = description.IndexOf('/');
            if (slash > 0 && int.TryParse(description.Substring(0, slash).Trim(), out int days))
            {
                return days;
            }
            return 0;
        }

        private static string ScenarioProblem(string name)
        {
            if (string.IsNullOrEmpty(name) || ResolveScenario(name) != null)
            {
                return "";
            }
            string offered = string.Join(", ", DefDatabase<ScenarioDef>.AllDefsListForReading
                .Select(def => def.defName)
                .OrderBy(n => n));
            return "Unknown scenario '" + name + "'. The installed game offers: " + offered + ".";
        }

        private static BiomeDef ResolveBiome(string name)
        {
            string want = Normalize(name);
            if (want == "")
            {
                return null;
            }
            return DefDatabase<BiomeDef>.AllDefsListForReading.FirstOrDefault(
                def => Normalize(def.defName) == want || Normalize(def.label) == want);
        }

        private static string BiomeNames()
        {
            return string.Join(", ", DefDatabase<BiomeDef>.AllDefsListForReading
                .Select(def => def.defName + " (" + def.label + ")")
                .OrderBy(n => n));
        }

        private static string ResolveHilliness(string name)
        {
            string want = Normalize(name);
            if (want == "")
            {
                return null;
            }
            string word;
            if (HillinessWords.TryGetValue(want, out word))
            {
                return word;
            }
            foreach (string known in Enum.GetNames(typeof(Hilliness)))
            {
                if (Normalize(known) == want)
                {
                    return known;
                }
            }
            return null;
        }

        /// <summary>Folds the spellings a caller may use onto one form.</summary>
        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            return new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        }
    }
}
