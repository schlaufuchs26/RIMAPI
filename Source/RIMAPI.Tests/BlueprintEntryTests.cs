using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Models;
using RIMAPI.Models.Map;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The blueprint read carries the footprint of what a plan builds (ticket
    /// #3608): without it a 2x1 bench's plan covered one cell and the caller's
    /// map art drew it as its corner. These tests pin the shape the fox's
    /// rimworld tool reads, `size: {x, y, z}`, the same key the buildings read
    /// uses.
    /// </summary>
    public class BlueprintEntryTests
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new SnakeCaseContractResolver(),
        };

        [Fact]
        public void TheFootprintTravelsUnderTheSizeKey()
        {
            var json = JObject.Parse(JsonConvert.SerializeObject(new BlueprintEntryDto
            {
                DefName = "Bed",
                Kind = "blueprint",
                X = 133,
                Z = 122,
                Rotation = 2,
                Size = new PositionDto { X = 1, Y = 0, Z = 2 },
            }, Settings));

            var size = json["size"] as JObject;
            Assert.NotNull(size);
            Assert.Equal(1, (int)size["x"]);
            Assert.Equal(2, (int)size["z"]);
        }
    }
}
