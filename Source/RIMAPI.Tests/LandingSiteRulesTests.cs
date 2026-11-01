using System.Collections.Generic;
using System.Linq;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // The ranking behind a founding's site wish (fox ticket #3930): the tile
    // closest to what the run asked for, the wishes it could not meet, and the
    // next two candidates.
    public class LandingSiteRulesTests
    {
        private static LandingSiteRules.Candidate Tile(
            int id, string biome = "TemperateForest", float temperature = 14f, int growingDays = 180,
            bool road = false, bool river = false, int settlements = 0, string hilliness = "Flat")
        {
            return new LandingSiteRules.Candidate
            {
                Tile = id,
                Biome = biome,
                Hilliness = hilliness,
                Temperature = temperature,
                GrowingDays = growingDays,
                Road = road,
                River = river,
                SettlementsNear = settlements,
            };
        }

        private static LandingSiteRules.Wish BiomeWish(params string[] biomes)
        {
            return new LandingSiteRules.Wish { Biomes = biomes.ToList() };
        }

        [Fact]
        public void AnEmptyWishIsNoWish()
        {
            Assert.False(LandingSiteRules.HasWish(new LandingSiteRules.Wish()));
            Assert.False(LandingSiteRules.HasWish(null));
        }

        [Fact]
        public void TheBiomeHigherOnTheListWins()
        {
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(1, "BorealForest"), Tile(2, "TemperateForest") },
                BiomeWish("TemperateForest", "BorealForest"),
                0);

            Assert.Equal(2, ranking.Chosen.Tile);
            Assert.Equal(0, ranking.Score);
            Assert.Empty(ranking.Unmet);
            // Both tiles meet the biome wish; the list only orders them.
            Assert.Equal(2, ranking.FullMatches);
        }

        [Fact]
        public void ASecondChoiceBiomeStillBeatsATileOutsideTheList()
        {
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(1, "Desert"), Tile(2, "BorealForest") },
                BiomeWish("TemperateForest", "BorealForest"),
                0);

            Assert.Equal(2, ranking.Chosen.Tile);
            // The chosen tile is a second choice, which still meets the wish.
            Assert.Empty(ranking.Unmet);
            Assert.Equal(1, ranking.FullMatches);
            Assert.False(LandingSiteRules.Judge(Tile(1, "Desert"), BiomeWish("TemperateForest", "BorealForest"))[0].Met);
        }

        [Fact]
        public void AMissingRoadCostsMoreThanAMissingRiver()
        {
            LandingSiteRules.Wish wish = new LandingSiteRules.Wish { Road = true, River = true };
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(1, road: true), Tile(2, river: true) },
                wish,
                0);

            // Both miss one wish; the road is the more expensive one to lose,
            // so the tile that has it wins even without a river.
            Assert.Equal(1, ranking.Chosen.Tile);
        }

        [Fact]
        public void TheTemperatureBandIsCountedInDegrees()
        {
            LandingSiteRules.Wish wish = new LandingSiteRules.Wish { TemperatureMin = 10f, TemperatureMax = 20f };
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(1, temperature: 4f), Tile(2, temperature: 12f) },
                wish,
                0);

            Assert.Equal(2, ranking.Chosen.Tile);
            Assert.Equal(0, ranking.Score);
            Assert.Equal("temperature", ranking.Criteria.Single(c => c.Name == "temperature").Name);
        }

        [Fact]
        public void AShortGrowingSeasonIsCountedInDays()
        {
            LandingSiteRules.Wish wish = new LandingSiteRules.Wish { GrowingDaysMin = 120 };
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(1, growingDays: 60), Tile(2, growingDays: 120) },
                wish,
                0);

            Assert.Equal(2, ranking.Chosen.Tile);
            Assert.Equal(0, ranking.Score);
            Assert.Equal(60, LandingSiteRules.Judge(Tile(1, growingDays: 60), wish)[0].Penalty);
        }

        [Fact]
        public void NeighboursAreCountedAgainstTheWish()
        {
            LandingSiteRules.Wish wish = new LandingSiteRules.Wish { SettlementsMin = 2, SettlementRadius = 6 };
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(1, settlements: 0), Tile(2, settlements: 3) },
                wish,
                0);

            Assert.Equal(2, ranking.Chosen.Tile);
            Assert.Contains("at least 2 within 6 tiles", ranking.Criteria[0].Wish);
        }

        [Fact]
        public void AHillinessWishPicksTheMountain()
        {
            LandingSiteRules.Wish wish = new LandingSiteRules.Wish { Hilliness = new List<string> { "Mountainous" } };
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(1), Tile(2, hilliness: "Mountainous") },
                wish,
                0);

            Assert.Equal(2, ranking.Chosen.Tile);
            Assert.Equal(0, ranking.Score);
        }

        [Fact]
        public void VaryTakesADifferentTileAndNamesWhatItPassedOver()
        {
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(1, "TemperateForest"), Tile(2, "BorealForest"), Tile(3, "Tundra") },
                BiomeWish("TemperateForest", "BorealForest", "Tundra"),
                1);

            Assert.Equal(2, ranking.Chosen.Tile);
            Assert.Equal(1, ranking.Rank);
            Assert.Equal(8, ranking.Score);
            // The best tile is named as the alternative the run gave up.
            Assert.Equal(1, ranking.Alternatives[0].Tile);
            Assert.Equal(3, ranking.Alternatives[1].Tile);
        }

        [Fact]
        public void AVaryPastTheEndTakesTheLastTile()
        {
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(1), Tile(2) },
                BiomeWish("TemperateForest"),
                9);

            Assert.Equal(2, ranking.Chosen.Tile);
            Assert.Equal(1, ranking.Alternatives.Single().Tile);
        }

        [Fact]
        public void AWishNoTileMeetsIsNamedAsDropped()
        {
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(1), Tile(2) },
                new LandingSiteRules.Wish { River = true },
                0);

            Assert.Equal(0, ranking.FullMatches);
            string line = Assert.Single(ranking.Unmet);
            Assert.Contains("river", line);
            Assert.Contains("no river", line);
        }

        [Fact]
        public void EqualsAreBrokenByGrowingSeasonThenTile()
        {
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new[] { Tile(9, growingDays: 100), Tile(4, growingDays: 200), Tile(7, growingDays: 200) },
                BiomeWish("TemperateForest"),
                0);

            Assert.Equal(4, ranking.Chosen.Tile);
            Assert.Equal(new[] { 7, 9 }, ranking.Alternatives.Select(c => c.Tile).ToArray());
        }

        [Fact]
        public void AnEmptyPlanetRanksNothing()
        {
            LandingSiteRules.Ranking ranking = LandingSiteRules.Rank(
                new LandingSiteRules.Candidate[0], BiomeWish("TemperateForest"), 0);

            Assert.Null(ranking.Chosen);
        }
    }
}
