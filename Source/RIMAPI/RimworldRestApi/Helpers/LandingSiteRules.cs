using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Which world tile a colony lands on, given a wish list (fox ticket
    /// #3930). Picking a tile by hand is a lottery: a run clicks one and gets
    /// whatever biome, climate and neighbours come with it. This ranks the
    /// planet's tiles against what the run asked for and names the closest
    /// match, what it gave up and the next two candidates.
    ///
    /// Pure on purpose: the game-side search (LandingSitePick) reads the grid
    /// and hands the candidates in, so the ranking is a unit test with no game
    /// and no map. Every wish is a soft one; a penalty counts what the tile
    /// costs, and a score of 0 means every wish was met.
    ///
    /// The weights are a judgement, not a measurement, so they are written
    /// down here rather than tuned silently: a biome two steps down the list
    /// costs more than 4 °C outside the band, and a missing road costs more
    /// than a missing river, because a road is the trade route and a river is
    /// a convenience.
    /// </summary>
    public static class LandingSiteRules
    {
        /// <summary>The wish list of one founding, as the caller wrote it.</summary>
        public class Wish
        {
            /// <summary>Biome defNames in order of preference; empty means any.</summary>
            public List<string> Biomes = new List<string>();
            /// <summary>Hilliness names in order of preference (e.g. Mountainous); empty means any.</summary>
            public List<string> Hilliness = new List<string>();
            public float? TemperatureMin;
            public float? TemperatureMax;
            /// <summary>Growing season at least this many days a year; 0 means no wish.</summary>
            public int GrowingDaysMin;
            public bool Road;
            public bool River;
            /// <summary>At least this many other factions' settlements within SettlementRadius tiles; 0 means no wish.</summary>
            public int SettlementsMin;
            public int SettlementRadius;
        }

        /// <summary>One tile as the search read it.</summary>
        public class Candidate
        {
            public int Tile;
            public string Biome;
            public string Hilliness;
            public float Temperature;
            public int GrowingDays;
            public bool Road;
            public bool River;
            public int SettlementsNear;
            public bool Polluted;
        }

        /// <summary>One line of the wish: what was asked, what the tile has, and whether it was met.</summary>
        public class Criterion
        {
            public string Name;
            public string Wish;
            public string Got;
            public bool Met;
            public int Penalty;
        }

        public class Ranking
        {
            /// <summary>False when the wish list was empty: the caller asked for no site at all.</summary>
            public bool Wished;
            public Candidate Chosen;
            /// <summary>0 for the best candidate, 1 for the second best, and so on.</summary>
            public int Rank;
            public int Score;
            public List<Criterion> Criteria = new List<Criterion>();
            /// <summary>The next two candidates after the chosen one, best first.</summary>
            public List<Candidate> Alternatives = new List<Candidate>();
            /// <summary>How many tiles of the planet met every wish.</summary>
            public int FullMatches;
            /// <summary>One line per wish the chosen tile does not meet.</summary>
            public List<string> Unmet = new List<string>();
        }

        // Penalty weights, in one place because they are the whole judgement.
        private const int BiomeStep = 8;
        private const int BiomeMissing = 60;
        private const int HillinessStep = 5;
        private const int HillinessMissing = 25;
        private const float TemperaturePerDegree = 2f;
        private const int GrowingPerDay = 1;
        private const int RoadMissing = 30;
        private const int RiverMissing = 20;
        private const int SettlementMissing = 15;
        private const int Polluted = 40;

        /// <summary>Whether the wish asks for anything at all.</summary>
        public static bool HasWish(Wish wish)
        {
            return wish != null
                && (wish.Biomes.Count > 0
                    || wish.Hilliness.Count > 0
                    || wish.TemperatureMin.HasValue
                    || wish.TemperatureMax.HasValue
                    || wish.GrowingDaysMin > 0
                    || wish.Road
                    || wish.River
                    || wish.SettlementsMin > 0);
        }

        /// <summary>
        /// Ranks the candidates and takes the one at position <paramref name="vary"/>
        /// (0 = best). A vary past the end takes the last one, so a run asking
        /// for a different colony never gets an empty answer.
        /// </summary>
        public static Ranking Rank(IEnumerable<Candidate> candidates, Wish wish, int vary)
        {
            var ranking = new Ranking { Wished = HasWish(wish) };
            if (wish == null)
            {
                wish = new Wish();
            }

            var scored = new List<KeyValuePair<Candidate, List<Criterion>>>();
            foreach (Candidate candidate in candidates)
            {
                List<Criterion> criteria = Judge(candidate, wish);
                scored.Add(new KeyValuePair<Candidate, List<Criterion>>(candidate, criteria));
            }
            if (scored.Count == 0)
            {
                return ranking;
            }

            // A stable, explainable order: the lowest penalty wins, then the
            // longer growing season, then the lower tile id. Without the last
            // two a run would get an arbitrary tile among equals and could not
            // reproduce it from the same seed.
            List<KeyValuePair<Candidate, List<Criterion>>> ordered = scored
                .OrderBy(pair => pair.Value.Sum(c => c.Penalty))
                .ThenByDescending(pair => pair.Key.GrowingDays)
                .ThenBy(pair => pair.Key.Tile)
                .ToList();

            int index = Math.Max(0, Math.Min(vary, ordered.Count - 1));
            KeyValuePair<Candidate, List<Criterion>> picked = ordered[index];
            ranking.Chosen = picked.Key;
            ranking.Rank = index;
            ranking.Criteria = picked.Value;
            ranking.Score = picked.Value.Sum(c => c.Penalty);
            ranking.FullMatches = ordered.Count(pair => pair.Value.All(c => c.Met));
            // The next two best of the rest, in rank order: a run that took a
            // lesser tile because it asked for a different colony still sees
            // the better one it passed over.
            ranking.Alternatives = ordered
                .Where((pair, at) => at != index)
                .Take(2)
                .Select(pair => pair.Key)
                .ToList();
            foreach (Criterion criterion in picked.Value.Where(c => !c.Met))
            {
                ranking.Unmet.Add(UnmetLine(criterion));
            }
            return ranking;
        }

        /// <summary>The wish lines of one tile, in a fixed order so two runs read alike.</summary>
        public static List<Criterion> Judge(Candidate candidate, Wish wish)
        {
            var lines = new List<Criterion>();
            if (wish.Biomes.Count > 0)
            {
                int at = IndexOf(wish.Biomes, candidate.Biome);
                lines.Add(new Criterion
                {
                    Name = "biome",
                    Wish = string.Join(" > ", wish.Biomes),
                    Got = candidate.Biome,
                    Met = at >= 0,
                    Penalty = at >= 0 ? at * BiomeStep : BiomeMissing,
                });
            }
            if (wish.Hilliness.Count > 0)
            {
                int at = IndexOf(wish.Hilliness, candidate.Hilliness);
                lines.Add(new Criterion
                {
                    Name = "hilliness",
                    Wish = string.Join(" > ", wish.Hilliness),
                    Got = candidate.Hilliness,
                    Met = at >= 0,
                    Penalty = at >= 0 ? at * HillinessStep : HillinessMissing,
                });
            }
            if (wish.TemperatureMin.HasValue || wish.TemperatureMax.HasValue)
            {
                float low = wish.TemperatureMin ?? float.MinValue;
                float high = wish.TemperatureMax ?? float.MaxValue;
                float off = 0f;
                if (candidate.Temperature < low)
                {
                    off = low - candidate.Temperature;
                }
                else if (candidate.Temperature > high)
                {
                    off = candidate.Temperature - high;
                }
                lines.Add(new Criterion
                {
                    Name = "temperature",
                    Wish = TemperatureBand(wish),
                    Got = Temperature(candidate.Temperature),
                    Met = off == 0f,
                    Penalty = (int)Math.Round(off * TemperaturePerDegree),
                });
            }
            if (wish.GrowingDaysMin > 0)
            {
                int shortfall = Math.Max(0, wish.GrowingDaysMin - candidate.GrowingDays);
                lines.Add(new Criterion
                {
                    Name = "growing season",
                    Wish = "at least " + wish.GrowingDaysMin + " days a year",
                    Got = candidate.GrowingDays + " days",
                    Met = shortfall == 0,
                    Penalty = shortfall * GrowingPerDay,
                });
            }
            if (wish.Road)
            {
                lines.Add(new Criterion
                {
                    Name = "road",
                    Wish = "a road",
                    Got = candidate.Road ? "a road" : "no road",
                    Met = candidate.Road,
                    Penalty = candidate.Road ? 0 : RoadMissing,
                });
            }
            if (wish.River)
            {
                lines.Add(new Criterion
                {
                    Name = "river",
                    Wish = "a river",
                    Got = candidate.River ? "a river" : "no river",
                    Met = candidate.River,
                    Penalty = candidate.River ? 0 : RiverMissing,
                });
            }
            if (wish.SettlementsMin > 0)
            {
                int radius = wish.SettlementRadius > 0 ? wish.SettlementRadius : 6;
                int shortfall = Math.Max(0, wish.SettlementsMin - candidate.SettlementsNear);
                lines.Add(new Criterion
                {
                    Name = "neighbours",
                    Wish = "at least " + wish.SettlementsMin + " within " + radius + " tiles",
                    Got = candidate.SettlementsNear + " within " + radius + " tiles",
                    Met = shortfall == 0,
                    Penalty = shortfall * SettlementMissing,
                });
            }
            return lines;
        }

        /// <summary>
        /// The line a run reads when a wish was not met. It names the wish and
        /// the fact that stopped it, never a bare "failed".
        /// </summary>
        public static string UnmetLine(Criterion criterion)
        {
            return criterion.Name + ": asked for " + criterion.Wish + ", the tile has " + criterion.Got;
        }

        private static int IndexOf(List<string> names, string value)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private static string Temperature(float value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture) + " C";
        }

        private static string TemperatureBand(Wish wish)
        {
            if (wish.TemperatureMin.HasValue && wish.TemperatureMax.HasValue)
            {
                return Temperature(wish.TemperatureMin.Value) + " to " + Temperature(wish.TemperatureMax.Value);
            }
            if (wish.TemperatureMin.HasValue)
            {
                return "at least " + Temperature(wish.TemperatureMin.Value);
            }
            return "at most " + Temperature(wish.TemperatureMax.Value);
        }
    }
}
