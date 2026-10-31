using System.Linq;
using RIMAPI.Services;
using Verse;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The walk scan's gas table (ticket #3283): the game keeps four gases in
    /// one grid, and each costs a walker something else, so the table is the
    /// only place that decides what a gas is called and whether it can refuse
    /// a move.
    /// </summary>
    public class GasHazardTests
    {
        [Fact]
        public void Every_gas_the_game_has_is_named_once()
        {
            GasType[] named = GasHazard.All.Select(gas => gas.GasType).ToArray();

            Assert.Equal(
                new[] { GasType.BlindSmoke, GasType.ToxGas, GasType.RotStink, GasType.DeadlifeDust },
                named);
        }

        [Fact]
        public void The_harmless_gases_are_named_without_ever_refusing_a_walk()
        {
            // Blind smoke only costs accuracy and deadlife dust only matters
            // for the corpses it raises, so an order must never be refused for
            // walking through either.
            Assert.Equal(Danger.None, GasOf(GasType.BlindSmoke).Danger);
            Assert.Equal(Danger.None, GasOf(GasType.DeadlifeDust).Danger);
        }

        [Fact]
        public void Tox_gas_and_rot_stink_cost_some_danger()
        {
            Assert.Equal(Danger.Some, GasOf(GasType.ToxGas).Danger);
            Assert.Equal(Danger.Some, GasOf(GasType.RotStink).Danger);
        }

        [Fact]
        public void Rot_stink_is_skipped_for_a_pawn_that_cannot_catch_lung_rot()
        {
            GasHazard rotStink = GasOf(GasType.RotStink);

            Assert.False(rotStink.AppliesTo(pawnCanCatchLungRot: false, anomalyActive: false));
            Assert.True(rotStink.AppliesTo(pawnCanCatchLungRot: true, anomalyActive: false));
        }

        [Fact]
        public void Deadlife_dust_is_skipped_without_the_anomaly_dlc()
        {
            GasHazard deadlife = GasOf(GasType.DeadlifeDust);

            Assert.False(deadlife.AppliesTo(pawnCanCatchLungRot: true, anomalyActive: false));
            Assert.True(deadlife.AppliesTo(pawnCanCatchLungRot: true, anomalyActive: true));
        }

        [Fact]
        public void Tox_gas_and_blind_smoke_apply_to_every_pawn()
        {
            // A mechanoid still takes toxic damage and still shoots worse
            // through smoke, so neither gas is gated on the pawn.
            Assert.True(GasOf(GasType.ToxGas).AppliesTo(pawnCanCatchLungRot: false, anomalyActive: false));
            Assert.True(GasOf(GasType.BlindSmoke).AppliesTo(pawnCanCatchLungRot: false, anomalyActive: false));
        }

        [Fact]
        public void Each_label_reads_as_a_hazard_text_entry()
        {
            // The labels go into "rot stink (4 cells)", so they stay lower case
            // and carry no level of their own.
            Assert.Equal(
                new[] { "blind smoke", "tox gas", "rot stink", "deadlife dust" },
                GasHazard.All.Select(gas => gas.Label).ToArray());
        }

        private static GasHazard GasOf(GasType type)
        {
            return GasHazard.All.Single(gas => gas.GasType == type);
        }
    }
}
