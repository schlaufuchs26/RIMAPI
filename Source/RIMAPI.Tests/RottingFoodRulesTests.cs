using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The rotting-food read (fox ticket #3672). Colony 14 lost 59 horse meat
    /// overnight in a room a PassiveCooler held at 16 C, and the first word
    /// was the game's own message after the loss. The rule has to name exactly
    /// the food that is spoiling: perishable, edible, in a room above 0 C.
    /// </summary>
    public class RottingFoodRulesTests
    {
        private static RottingFoodRules.FoodState Meat(float roomTemperature)
        {
            return new RottingFoodRules.FoodState
            {
                NutritionGiving = true,
                Rottable = true,
                HasRoom = true,
                RoomTemperature = roomTemperature,
            };
        }

        [Fact]
        public void FoodInAWarmRoomIsAtRisk()
        {
            // The measured case: a PassiveCooler holds a "freezer" at 16 C.
            Assert.True(RottingFoodRules.IsAtRisk(Meat(16f)));
        }

        [Fact]
        public void FoodInAFreezerIsNotAtRisk()
        {
            Assert.False(RottingFoodRules.IsAtRisk(Meat(-4f)));
        }

        [Fact]
        public void FreezingIsTheLine()
        {
            // The game's rot rate is zero at 0 C, so the warning starts above it.
            Assert.False(RottingFoodRules.IsAtRisk(Meat(0f)));
            Assert.True(RottingFoodRules.IsAtRisk(Meat(0.5f)));
        }

        [Fact]
        public void ANonPerishableIsNotAtRisk()
        {
            // Pemmican has no rot comp; warning about it would send a run to
            // cool a room for food that keeps anyway.
            var pemmican = Meat(16f);
            pemmican.Rottable = false;
            Assert.False(RottingFoodRules.IsAtRisk(pemmican));
        }

        [Fact]
        public void ACorpseIsNotFood()
        {
            var corpse = Meat(16f);
            corpse.Corpse = true;
            Assert.False(RottingFoodRules.IsAtRisk(corpse));
        }

        [Fact]
        public void FoodWithNoRoomIsLeftOut()
        {
            // A caravan or an unspawned stack has no room temperature to report.
            var caravan = Meat(16f);
            caravan.HasRoom = false;
            Assert.False(RottingFoodRules.IsAtRisk(caravan));
        }

        [Fact]
        public void HoursAreAlwaysAFiniteNumber()
        {
            // Infinity never reaches a JSON reader: it becomes "not soon".
            Assert.Equal(0f, RottingFoodRules.ClampHours(-1f));
            Assert.Equal(0f, RottingFoodRules.ClampHours(float.NaN));
            Assert.Equal(
                RottingFoodRules.MaxHoursUntilRot,
                RottingFoodRules.ClampHours(float.PositiveInfinity)
            );
            Assert.Equal(9.5f, RottingFoodRules.ClampHours(9.5f));
        }

        [Fact]
        public void TheAnswerKeepsTheKeysTheToolReads()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new SnakeCaseContractResolver(),
            };
            JObject json = JObject.Parse(
                JsonConvert.SerializeObject(
                    new MapRottingFoodDto
                    {
                        TotalStacks = 1,
                        TotalUnits = 59,
                        WarmestTemperature = 16f,
                        Items = new System.Collections.Generic.List<RottingFoodDto>
                        {
                            new RottingFoodDto
                            {
                                ThingId = 4711,
                                DefName = "Meat_Horse",
                                Label = "horse meat",
                                StackCount = 59,
                                HoursUntilRot = 9.5f,
                                RoomId = 7,
                                RoomLabel = "freezer",
                                RoomTemperature = 16f,
                                Position = new PositionDto { X = 133, Z = 139 },
                            },
                        },
                        // The machine that holds the room warm (fox ticket
                        // #3745): colony 15's cooler still stood at its
                        // default 21 C, so the room never chilled below it.
                        Thermostats = new System.Collections.Generic.List<RoomThermostatDto>
                        {
                            new RoomThermostatDto
                            {
                                RoomId = 7,
                                BuildingId = 4831,
                                Label = "cooler",
                                DefName = "Cooler",
                                Target = 21f,
                                HasPower = true,
                                Powered = true,
                            },
                        },
                    },
                    settings
                )
            );

            Assert.Equal(1, (int)json["total_stacks"]);
            Assert.Equal(59, (int)json["total_units"]);
            JObject item = (JObject)json["items"][0];
            Assert.Equal("Meat_Horse", (string)item["def_name"]);
            Assert.Equal(59, (int)item["stack_count"]);
            Assert.Equal(9.5f, (float)item["hours_until_rot"]);
            Assert.Equal(7, (int)item["room_id"]);
            Assert.Equal("freezer", (string)item["room_label"]);
            Assert.Equal(16f, (float)item["room_temperature"]);
            Assert.Equal(133, (int)item["position"]["x"]);

            // The tool names the machine's target beside the warm room, so a
            // rename of these keys would leave the rot line reading like a room
            // without a machine (fox ticket #3745).
            JObject thermostat = (JObject)json["thermostats"][0];
            Assert.Equal(7, (int)thermostat["room_id"]);
            Assert.Equal(4831, (int)thermostat["building_id"]);
            Assert.Equal("cooler", (string)thermostat["label"]);
            Assert.Equal(21f, (float)thermostat["target"]);
            Assert.True((bool)thermostat["has_power"]);
            Assert.True((bool)thermostat["powered"]);
        }
    }
}
