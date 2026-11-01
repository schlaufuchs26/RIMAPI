using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    // Why a powered building does nothing (fox ticket #3762): a stove four cells
    // from the nearest conduit has no net, so every power total is silent about
    // it while its bill reads "running".
    public class PowerFaultTests
    {
        private static PowerFault.Facts Stove()
        {
            return new PowerFault.Facts
            {
                Label = "electric stove",
                ConsumesPower = true,
                SwitchedOn = true,
                Connected = false,
                Powered = false,
                ConduitDistance = 4,
                ConduitX = 120,
                ConduitZ = 131,
            };
        }

        [Fact]
        public void APoweredBuildingHasNoFault()
        {
            var facts = Stove();
            facts.Connected = true;
            facts.Powered = true;

            Assert.Null(PowerFault.Describe(facts));
        }

        [Fact]
        public void ABuildingThatDrawsNothingHasNoFault()
        {
            var facts = Stove();
            facts.ConsumesPower = false;

            Assert.Null(PowerFault.Describe(facts));
        }

        [Fact]
        public void AnUnconnectedBuildingNamesTheNearestConduit()
        {
            string fault = PowerFault.Describe(Stove());

            Assert.Equal(
                "the electric stove is not connected to a power net: the nearest conduit stands 4 cells away at (120,131)",
                fault);
        }

        [Fact]
        public void AMapWithoutAConduitSaysSo()
        {
            var facts = Stove();
            facts.ConduitDistance = -1;

            Assert.Equal(
                "the electric stove is not connected to a power net: the map has no conduit at all",
                PowerFault.Describe(facts));
        }

        [Fact]
        public void ASwitchedOffBuildingIsNotAWiringFault()
        {
            var facts = Stove();
            facts.SwitchedOn = false;

            Assert.Equal("the electric stove is switched off", PowerFault.Describe(facts));
        }

        [Fact]
        public void AConnectedBuildingOnADeadNetNamesTheNet()
        {
            var facts = Stove();
            facts.Connected = true;
            facts.Powered = false;
            facts.NetProduction = 0f;
            facts.NetStored = 0f;

            Assert.Equal(
                "the electric stove is connected, but its net produces 0 W and stores 0 Wd",
                PowerFault.Describe(facts));
        }

        [Fact]
        public void ABenchWithoutPowerSaysSoInsteadOfRunning()
        {
            var facts = new BillStateReason.Facts
            {
                MissingIngredients = new List<string>(),
                PowerFault = PowerFault.Describe(Stove()),
            };

            var state = BillStateReason.Describe(facts);

            Assert.Equal(BillStateReason.NoPower, state.Name);
            Assert.Contains("nearest conduit stands 4 cells away", state.Reason);
        }

        [Fact]
        public void AMetTargetOutranksThePowerFault()
        {
            var facts = new BillStateReason.Facts
            {
                MissingIngredients = new List<string>(),
                TargetMet = true,
                PowerFault = PowerFault.Describe(Stove()),
            };

            Assert.Equal(BillStateReason.Satisfied, BillStateReason.Describe(facts).Name);
        }
    }
}
