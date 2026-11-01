using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The storage-capacity read (fox ticket #3648): a colony stood at 167 ripe
    /// rice plants with the store full, and every read named the plants while
    /// none named the room. The answer has to keep its JSON keys (the fox's
    /// tool reads them by name) and has to refuse a def it cannot answer for
    /// instead of reporting a capacity of zero, which reads like a full store.
    /// </summary>
    public class StorageCapacityTests
    {
        [Fact]
        public void TheAnswerKeepsTheKeysTheToolReads()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new SnakeCaseContractResolver(),
            };
            JObject json = JObject.Parse(
                JsonConvert.SerializeObject(
                    new StorageCapacityDto
                    {
                        DefName = "RawRice",
                        PlantDef = "Plant_Rice",
                        ThingDef = "RawRice",
                        Storages = 2,
                        Cells = 56,
                        FreeCells = 0,
                    },
                    settings
                )
            );

            Assert.Equal("RawRice", (string)json["thing_def"]);
            Assert.Equal("Plant_Rice", (string)json["plant_def"]);
            Assert.Equal(2, (int)json["storages"]);
            Assert.Equal(56, (int)json["cells"]);
            Assert.Equal(0, (int)json["free_cells"]);
        }

        [Fact]
        public void AnUnknownDefNamesTheInputAndBothSpellings()
        {
            string message = StorageCapacityText.UnknownDefMessage("RawRise");

            Assert.Contains("RawRise", message);
            Assert.Contains("RawRice", message);
            Assert.Contains("Plant_Rice", message);
        }

        [Fact]
        public void AThingThatIsNoItemIsNotAFullStore()
        {
            Assert.Equal(
                "Wall is not a storable item, so no storage takes it",
                StorageCapacityText.NotStorableMessage("Wall")
            );
        }
    }
}
