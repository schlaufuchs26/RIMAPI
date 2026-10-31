using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The snake_case contract (ticket #3316): every API answer and every
    /// request body gets its field names from <see cref="SnakeCaseContractResolver"/>,
    /// so a renamed DTO property silently renames the JSON key the caller reads.
    /// These tests pin the names the fox's rimworld tool depends on.
    /// </summary>
    public class SnakeCaseContractTests
    {
        // The settings mirror ResponseBuilder's: the resolver shapes the names,
        // nothing else about the shape matters to these assertions.
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new SnakeCaseContractResolver(),
        };

        private static JObject Json(object value) =>
            JObject.Parse(JsonConvert.SerializeObject(value, Settings));

        [Fact]
        public void TheNamesDocumentedInTheConventionsBecomeSnakeCase()
        {
            JObject json = Json(new DocumentedNames
            {
                MapId = 1,
                IsDrafted = true,
                PlantDef = "Plant_Potato",
                X = 120,
                Y = 0,
                Z = 130,
            });

            Assert.Equal(
                new[] { "is_drafted", "map_id", "plant_def", "x", "y", "z" },
                json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
            Assert.Equal(120, (int)json["x"]);
        }

        [Fact]
        public void ASingleLetterNameIsJustLowercased()
        {
            JObject json = Json(new PositionDto { X = 4, Y = 0, Z = 7 });

            Assert.Equal(new[] { "x", "y", "z" }, json.Properties().Select(p => p.Name));
            Assert.Equal(7, (int)json["z"]);
        }

        [Fact]
        public void EveryCapitalLetterStartsANewWord()
        {
            // The rule is one underscore per capital, so an acronym breaks up
            // (HP becomes h_p) and a camel word reads right (MapId becomes
            // map_id). A DTO that needs an acronym as one key gets an explicit
            // [JsonProperty] instead of relying on this.
            var probe = new ProbeResolver();

            Assert.Equal("map_id", probe.Key("MapId"));
            Assert.Equal("h_p", probe.Key("HP"));
            Assert.Equal("plant_def", probe.Key("PlantDef"));
        }

        [Fact]
        public void DigitsStayGluedToTheWordBeforeThem()
        {
            var probe = new ProbeResolver();

            Assert.Equal("map_id2", probe.Key("MapId2"));
            Assert.Equal("value2_count", probe.Key("Value2Count"));
        }

        [Fact]
        public void AnEmptyNameStaysEmpty()
        {
            var probe = new ProbeResolver();

            Assert.Equal(string.Empty, probe.Key(string.Empty));
            Assert.Null(probe.Key(null));
        }

        [Fact]
        public void EveryBillFieldKeepsTheKeyTheToolReads()
        {
            // A bill line the fox prints reads these keys. Adding or renaming a
            // field in BillDto updates this list on purpose: the point is that
            // a rename cannot reach the tool unnoticed.
            var expected = new[]
            {
                "allowed_materials",
                "allowed_skill_range",
                "available_materials",
                "hp_range",
                "include_equipped",
                "include_tainted",
                "ingredient_search_radius",
                "limit_to_allowed_stuff",
                "load_id",
                "min_skill",
                "missing_ingredients",
                "pause_when_satisfied",
                "paused",
                "pawn_restriction_id",
                "player_custom_name",
                "products_in_colony",
                "quality_range",
                "recipe_def_name",
                "recipe_label",
                "repeat_count",
                "repeat_mode",
                "skilled_colonists",
                "slot_group_id",
                "state",
                "state_reason",
                "store_mode",
                "suspended",
                "target_count",
                "unpause_when_you_have",
                "work_skill",
            };

            JObject json = Json(new BillDto());

            Assert.Equal(expected, json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void ANestedRangeKeepsItsOwnKeys()
        {
            JObject json = Json(new BillDto
            {
                QualityRange = new IntRangeDto(3, 5),
                HpRange = new FloatRange(0.5f, 1f),
            });

            Assert.Equal(3, (int)json["quality_range"]["min"]);
            Assert.Equal(5, (int)json["quality_range"]["max"]);
            Assert.Equal(0.5f, (float)json["hp_range"]["min"]);
            Assert.Equal(1f, (float)json["hp_range"]["max"]);
        }

        [Fact]
        public void ARequestBodyIsReadBackFromTheSameSnakeCaseKeys()
        {
            // The tool creates a bill with snake_case keys; the same resolver
            // reads the body, so the two directions have to agree.
            const string body =
                "{\"recipe_def_name\":\"Make_MealSimple\",\"target_count\":5,"
                + "\"quality_range\":{\"min\":1,\"max\":3},\"hp_range\":{\"min\":0.6,\"max\":1.0}}";

            var request = JsonConvert.DeserializeObject<CreateBillRequest>(body, Settings);

            Assert.Equal("Make_MealSimple", request.RecipeDefName);
            Assert.Equal(5, request.TargetCount);
            Assert.Equal(1, request.QualityRange.Min);
            Assert.Equal(3, request.QualityRange.Max);
            Assert.Equal(0.6f, request.HpRange.Min);
        }

        [Fact]
        public void EveryBuildingFieldKeepsTheKeyTheToolReads()
        {
            // A building line reads these keys, bed included: without bed a
            // prisoner bed and a colonist bed look the same to the caller.
            var expected = new[]
            {
                "bed",
                "def",
                "id",
                "label",
                "position",
                "rotation",
                "size",
                "type",
            };

            JObject json = Json(new BuildingDto());

            Assert.Equal(expected, json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
            Assert.Equal(
                new[] { "for_prisoners", "for_slaves", "medical", "owner" },
                Json(new BedDto()).Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void EveryMessageFieldKeepsTheKeyTheToolReads()
        {
            // A message line the fox prints reads these keys: the text, its
            // kind, its tick, and the cell or thing it is about (ticket
            // #3336). A rename here would leave the wait report and the digest
            // with empty fields and no error.
            var expected = new[]
            {
                "def",
                "has_target",
                "map_id",
                "target_def",
                "target_label",
                "text",
                "tick",
                "x",
                "y",
                "z",
            };

            JObject json = Json(new MessageDto());

            Assert.Equal(expected, json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void NoLinkedDtoFieldResolvesToAKeyWithACapitalLetter()
        {
            // A guard for the files linked in the csproj: whatever a future DTO
            // field is called, the key it produces is lowercase and starts with
            // a letter, never with the separator.
            var probe = new ProbeResolver();
            var dtoTypes = new[]
            {
                typeof(PositionDto),
                typeof(BillDto),
                typeof(BuildingDto),
                typeof(BedDto),
                typeof(TurretInfoDto),
                typeof(IntRangeDto),
                typeof(FloatRange),
                typeof(CreateBillRequest),
                typeof(UpdateBillRequest),
            };

            var bad = new List<string>();
            foreach (Type type in dtoTypes)
            {
                foreach (var property in type.GetProperties())
                {
                    string key = probe.Key(property.Name);
                    if (key != key.ToLowerInvariant() || key.StartsWith("_"))
                        bad.Add(type.Name + "." + property.Name + " -> " + key);
                }
            }

            Assert.Empty(bad);
        }

        /// <summary>
        /// The table of names in CLAUDE.md, as a class: these pairs are the
        /// contract the docs promise to callers.
        /// </summary>
        private class DocumentedNames
        {
            public int MapId { get; set; }

            public bool IsDrafted { get; set; }

            public string PlantDef { get; set; }

            public int X { get; set; }

            public int Y { get; set; }

            public int Z { get; set; }
        }

        /// <summary>
        /// Reaches the protected name mapping, so a test can ask for one key
        /// without building a DTO around it.
        /// </summary>
        private sealed class ProbeResolver : SnakeCaseContractResolver
        {
            public string Key(string propertyName) => ResolvePropertyName(propertyName);
        }
    }
}
