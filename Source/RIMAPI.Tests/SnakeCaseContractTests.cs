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
        public void EveryLetterFieldKeepsTheKeyTheToolReads()
        {
            // A wait that ends on a letter hands the run the whole letter, so
            // the body and the choices a dialog offers are part of the
            // contract the fox reads (ticket #3491). Renaming one has to be a
            // deliberate edit of this list.
            var expected = new[]
            {
                "body", "can_dismiss", "def", "id", "label", "new", "no_trait_option",
                "options", "passion_choices", "passion_gains", "pauses", "quest_id",
                "text", "tick", "trait_choices",
            };

            JObject json = Json(new LetterDto
            {
                Id = 3,
                Label = "Visitors",
                Text = "A group arrived.",
                Body = "A group arrived.\n\nThey will stay for a few days.",
                Def = "PositiveEvent",
                Tick = 900,
                CanDismiss = true,
                Options = new List<string> { "Accept", "Reject" },
            });

            Assert.Equal(expected, json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
            Assert.Equal("A group arrived.\n\nThey will stay for a few days.", (string)json["body"]);
            Assert.Equal(new[] { "Accept", "Reject" }, json["options"].Select(o => (string)o));
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
                "best_skill",
                "best_skill_pawn",
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
                "work_types",
            };

            JObject json = Json(new BillDto());

            Assert.Equal(expected, json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void EveryJobLogFieldKeepsTheKeyTheToolReads()
        {
            // The activity fold of the fox's wait report reads these keys; the
            // idle flag in particular is what replaced its own list of wander
            // job names (ticket #3392), so a rename here has to fail a test.
            var expected = new[]
            {
                "idle",
                "job",
                "label",
                "pawn",
                "pawn_id",
                "target",
                "tick",
            };

            JObject json = Json(new JobLogEntryDto());

            Assert.Equal(expected, json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void EveryBedFieldOfACarryKeepsTheKeyTheToolReads()
        {
            // The arrest and rescue answers name where the bed stands (ticket
            // #3421, #3383): the room, its temperature, a fire in it, and the
            // roof answer that tells "outdoors" from a roleless room (#3622).
            // The refused bed carries its own room label since #3623.
            var arrest = new[]
            {
                "arrestor",
                "arrestor_id",
                "bed",
                "bed_id",
                "bed_no_cell_reason",
                "bed_outdoors",
                "bed_room_has_fire",
                "bed_room_id",
                "bed_room_label",
                "bed_room_temp_c",
                "drafted",
                "job",
                "name",
                "pawn_id",
                "rejected_bed",
                "rejected_bed_id",
                "rejected_bed_no_cell_reason",
                "rejected_bed_room_has_fire",
                "rejected_bed_room_label",
                "rejected_bed_temp_c",
            };
            JObject json = Json(new ArrestResultDto());
            Assert.Equal(arrest, json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));

            var rescue = new[]
            {
                "bed",
                "bed_for_prisoners",
                "bed_id",
                "bed_outdoors",
                "bed_room_has_fire",
                "bed_room_id",
                "bed_room_label",
                "bed_room_temp_c",
                "guest",
                "job",
                "name",
                "pawn_id",
                "rejected_bed",
                "rejected_bed_id",
                "rejected_bed_room_has_fire",
                "rejected_bed_room_label",
                "rejected_bed_temp_c",
                "rescuer",
                "rescuer_id",
            };
            JObject rescueJson = Json(new RescueResultDto());
            Assert.Equal(
                rescue,
                rescueJson.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void EveryHediffFieldKeepsTheKeyTheToolReads()
        {
            // The untended-injury block reads these keys, the two infection
            // facts included (ticket #3449): a rename would leave the digest
            // with a zero rate and no infection flag, and it would silently
            // fall back on its own table again.
            var expected = new[]
            {
                "age_string",
                "age_ticks",
                "bleed_rate",
                "bleeding",
                "can_ever_kill",
                "combat_log_text",
                "cur_stage_index",
                "cur_stage_label",
                "def_name",
                "immunity",
                "is_currently_life_threatening",
                "is_infection",
                "is_lethal",
                "is_permanent",
                "is_tended",
                "label",
                "label_cap",
                "label_in_brackets",
                "load_id",
                "pain_factor",
                "pain_offset",
                "part_def_name",
                "part_label",
                "severity",
                "severity_label",
                "severity_per_day",
                "source_body_part_group_def_name",
                "source_def_name",
                "source_hediff_def_name",
                "source_label",
                "tendable_now",
                "tip_string_extra",
                "visible",
            };

            JObject json = Json(new HediffDto());

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
                new[] { "cell", "for_prisoners", "for_slaves", "medical", "owner" },
                Json(new BedDto()).Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
            // The cell block is what the bed order's answer reads to say
            // whether the room is a prison cell at all (fox ticket #3743).
            Assert.Equal(
                new[] { "cells_count", "is_prison_cell", "no_cell_reason", "role_label" },
                Json(new BedCellDto()).Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void EveryMessageFieldKeepsTheKeyTheToolReads()
        {
            // A message line the fox prints reads these keys: the text, its
            // kind, its tick, and the cell or thing it is about (ticket
            // #3336); the class and the label tell a notification from a
            // letter now that the archive answers both (ticket #3400). A
            // rename here would leave the wait report and the digest with
            // empty fields and no error.
            var expected = new[]
            {
                "class",
                "def",
                "has_target",
                "label",
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
        public void EveryTradeBeaconFieldKeepsTheKeyTheToolReads()
        {
            // The beacon read is a map fact the fox's `trade` prints with or
            // without a ship in orbit (fox ticket #3632), so the three keys are
            // what its client decodes. A rename here has to be a deliberate
            // edit of this list.
            var expected = new[] { "powered", "x", "z" };

            JObject json = Json(new TradeBeaconDto { X = 146, Z = 121, Powered = true });

            Assert.Equal(expected, json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
            Assert.True((bool)json["powered"]);
            Assert.Equal(146, (int)json["x"]);
            Assert.Equal(121, (int)json["z"]);
        }

        [Fact]
        public void EveryFuelFieldKeepsTheKeyTheToolReads()
        {
            // The fuel summary rides on the resources answer (fox ticket
            // #3678): the stock line names wood, and a stove whose tank is
            // empty is what a run has to act on. These are the keys the fox's
            // digest decodes.
            JObject json = Json(new FuelSummaryDto
            {
                Stocks = new List<FuelStockDto>
                {
                    new FuelStockDto { DefName = "WoodLog", Label = "wood", Count = 1200 },
                },
                Buildings = new List<FuelBuildingDto>
                {
                    new FuelBuildingDto
                    {
                        Id = 412,
                        DefName = "FueledStove",
                        Label = "fueled stove",
                        Fuel = 0f,
                        FuelCapacity = 30f,
                        FuelType = "WoodLog",
                        HasFuel = false,
                        RefuelAvailable = false,
                        RefuelForced = true,
                        RefuelBlocked = "No fuel to refuel (wood)",
                        Bills = 2,
                        MealBills = 2,
                    },
                },
            });

            Assert.Equal(
                new[] { "buildings", "stocks" },
                json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
            Assert.Equal(
                new[] { "count", "def_name", "label" },
                ((JObject)json["stocks"][0]).Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
            Assert.Equal(
                new[]
                {
                    "bills", "def_name", "fuel", "fuel_capacity", "fuel_type", "has_fuel", "id",
                    "label", "meal_bills", "refuel_available", "refuel_blocked", "refuel_forced",
                },
                ((JObject)json["buildings"][0]).Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
            Assert.False((bool)json["buildings"][0]["has_fuel"]);
            Assert.False((bool)json["buildings"][0]["refuel_available"]);
            Assert.True((bool)json["buildings"][0]["refuel_forced"]);
            Assert.Equal("No fuel to refuel (wood)", (string)json["buildings"][0]["refuel_blocked"]);
            Assert.Equal(2, (int)json["buildings"][0]["meal_bills"]);
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
                typeof(BedCellDto),
                typeof(TurretInfoDto),
                typeof(IntRangeDto),
                typeof(FloatRange),
                typeof(CreateBillRequest),
                typeof(UpdateBillRequest),
                typeof(FuelSummaryDto),
                typeof(FuelStockDto),
                typeof(FuelBuildingDto),
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
