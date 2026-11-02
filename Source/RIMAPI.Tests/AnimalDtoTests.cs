using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    // fox #4191: a tame attempt on a wild animal can end with the animal
    // manhunter, and the race's own chance decides it. The animals read offered
    // `tame` without that figure, so a run picked a wolf for wool the way colony
    // 18 picked the cassowary for meat.
    public class AnimalDtoTests
    {
        // The fox's Go client decodes this key by name; a rename here would make
        // every animal read "not reported" without failing anything else.
        [Fact]
        public void TheTameFailChanceUsesItsOwnSnakeCaseKey()
        {
            var animal = new AnimalDto
            {
                Id = 55410,
                Label = "wolf",
                Side = AnimalRules.Wild,
                Tameable = true,
                ManhunterOnTameFailChance = 0.5f,
            };

            var json = JsonConvert.SerializeObject(new { animals = new[] { animal } },
                new JsonSerializerSettings
                {
                    ContractResolver = new SnakeCaseContractResolver(),
                });

            var entry = (JObject)JObject.Parse(json)["animals"][0];
            Assert.Equal(0.5f, (float)entry["manhunter_on_tame_fail_chance"]);
            // The damage chance keeps its own key next to it, so a run that
            // hunts and one that tames read the figure they need.
            Assert.NotNull(entry["manhunter_on_damage_chance"]);
        }

        // A race the game refuses to tame still carries its number: the client
        // decides what to show, the read reports what the race says.
        [Fact]
        public void ARaceThatCannotBeTamedStillCarriesTheChance()
        {
            var animal = new AnimalDto { Id = 55412, Label = "warg", Tameable = false, ManhunterOnTameFailChance = 1f };

            Assert.Equal(1f, animal.ManhunterOnTameFailChance);
            Assert.False(animal.Tameable);
        }
    }
}
