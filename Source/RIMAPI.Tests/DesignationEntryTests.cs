using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    // What an undesignate call removed, in the shape the designations read
    // sends (fox ticket #3508). The count alone cannot tell a swept forest from
    // the boulders that were meant, so the entries travel with it.
    public class DesignationEntryTests
    {
        [Fact]
        public void ACellDesignationCarriesNoThingFields()
        {
            var dto = DesignationEntry.Make("Mine", "Mine", false, 120, 90);

            Assert.Equal("Mine", dto.DefName);
            Assert.Equal("cell", dto.Target);
            Assert.Equal(120, dto.X);
            Assert.Equal(90, dto.Z);
            Assert.Equal(0, dto.ThingId);
            Assert.Null(dto.ThingDefName);
            Assert.Null(dto.ThingLabel);
        }

        [Fact]
        public void AThingDesignationNamesTheThingItHangsOn()
        {
            var dto = DesignationEntry.Make("CutPlant", "Chop", true, 134, 142,
                4711, "Plant_TreePine", "Pine tree");

            Assert.Equal("thing", dto.Target);
            Assert.Equal(4711, dto.ThingId);
            Assert.Equal("Plant_TreePine", dto.ThingDefName);
            Assert.Equal("Pine tree", dto.ThingLabel);
            Assert.Equal(134, dto.X);
            Assert.Equal(142, dto.Z);
        }

        [Fact]
        public void TheAnswerUsesTheSameSnakeCaseKeysAsTheRead()
        {
            // The fox reads these keys; a rename here breaks the client's
            // grouping of what it cleared, silently.
            var body = new
            {
                cells = 19881,
                removed = 2,
                removed_designations = new List<DesignationDto>
                {
                    DesignationEntry.Make("CutPlant", "Chop", true, 1, 2, 7, "Plant_TreePine", "Pine tree"),
                },
            };
            var json = JsonConvert.SerializeObject(body, new JsonSerializerSettings
            {
                ContractResolver = new SnakeCaseContractResolver(),
            });

            var root = JObject.Parse(json);
            Assert.Equal(2, (int)root["removed"]);
            var entry = (JObject)root["removed_designations"][0];
            Assert.Equal("CutPlant", (string)entry["def_name"]);
            Assert.Equal("thing", (string)entry["target"]);
            Assert.Equal(7, (int)entry["thing_id"]);
            Assert.Equal("Pine tree", (string)entry["thing_label"]);
            Assert.Equal(1, (int)entry["x"]);
            Assert.Equal(2, (int)entry["z"]);
        }
    }
}
