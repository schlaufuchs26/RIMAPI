using System.Linq;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The building read answers with the turret or generator DTO when the
    /// building is one (fox ticket #3705). The service built that DTO and then
    /// overwrote it with the plain one, so IsPowered, PowerConsumption,
    /// PowerOutput and PowerOn never reached a caller: a power or turret check
    /// read a machine as an ordinary building and could not tell the difference.
    /// </summary>
    public class BuildingInfoTests
    {
        [Fact]
        public void A_turret_answers_with_the_turret_dto()
        {
            Assert.Equal(
                BuildingInfoKind.Turret,
                BuildingInfoRules.KindOf(isTurret: true, isPowerPlant: false));
        }

        [Fact]
        public void A_power_plant_answers_with_the_generator_dto()
        {
            Assert.Equal(
                BuildingInfoKind.PowerGenerator,
                BuildingInfoRules.KindOf(isTurret: false, isPowerPlant: true));
        }

        [Fact]
        public void Every_other_building_answers_with_the_plain_dto()
        {
            // A wall, a bed, a workbench: nothing the plain DTO cannot hold.
            Assert.Equal(
                BuildingInfoKind.Plain,
                BuildingInfoRules.KindOf(isTurret: false, isPowerPlant: false));
        }

        [Fact]
        public void A_turret_wins_over_a_power_plant()
        {
            // No def in the game is both; if one ever is, the longer answer is
            // the one that names the power state.
            Assert.Equal(
                BuildingInfoKind.Turret,
                BuildingInfoRules.KindOf(isTurret: true, isPowerPlant: true));
        }

        /// <summary>
        /// The read returns the specialised DTO instead of building the plain
        /// one afterwards (fox ticket #3705). This is the shape that was wrong,
        /// so it is stated against the service's own source: each arm has to
        /// answer, and the plain DTO only as the last resort.
        /// </summary>
        [Fact]
        public void The_building_read_answers_each_kind_instead_of_overwriting_it()
        {
            string root = SourceTree.RepoRoot();
            string body = SourceTree
                .Methods(SourceTree.Sources(root, "RimworldRestApi", "Services"), root)
                .Where(method => method.Name == "GetBuildingInfo")
                .Select(method => SourceTree.Squash(method.Body))
                .FirstOrDefault();

            Assert.NotNull(body);
            Assert.Contains("returnApiResult<BuildingDto>.Ok(BuildingHelper.GetTurretInfo(building))", body);
            Assert.Contains(
                "returnApiResult<BuildingDto>.Ok(BuildingHelper.GetPowerGeneratorInfo(building))",
                body
            );
            Assert.Contains("returnApiResult<BuildingDto>.Ok(BuildingHelper.BuildingToDto(building))", body);
        }
    }
}
