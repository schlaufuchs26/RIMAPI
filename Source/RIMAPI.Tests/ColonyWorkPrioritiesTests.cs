using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The colony-wide work-priorities read (fox ticket #4158): the build path
    /// names who would build a plan, and one read per pawn made a colony of
    /// eight pay eight loopback calls on every placement. The answer is the
    /// single-pawn entry, repeated; a renamed property would silently send a
    /// field the fox never reads, so the wire names are pinned here.
    /// </summary>
    public class ColonyWorkPrioritiesTests
    {
        private static JObject Json(object value) =>
            JObject.Parse(
                JsonConvert.SerializeObject(
                    value,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new SnakeCaseContractResolver(),
                    }
                )
            );

        [Fact]
        public void TheColonyAnswerCarriesItsPawns()
        {
            var answer = new ColonistsWorkPrioritiesDto
            {
                TotalPawns = 1,
                Pawns = new List<ColonistWorkPrioritiesDto>
                {
                    new ColonistWorkPrioritiesDto
                    {
                        PawnId = 12,
                        PawnName = "Mangle",
                        ManualPriorities = true,
                        Initialized = true,
                        Readiness = "ready",
                        Work = new List<WorkPriorityEntryDto>
                        {
                            new WorkPriorityEntryDto
                            {
                                WorkType = "Construction",
                                Priority = 2,
                                Stored = 2,
                                Disabled = false,
                            },
                        },
                    },
                },
            };

            JObject json = Json(answer);

            Assert.Equal(1, (int)json["total_pawns"]);
            JObject pawn = (JObject)json["pawns"][0];
            Assert.Equal(12, (int)pawn["pawn_id"]);
            Assert.Equal("Mangle", (string)pawn["pawn_name"]);
            Assert.True((bool)pawn["manual_priorities"]);
            Assert.Equal("ready", (string)pawn["readiness"]);
            JObject work = (JObject)pawn["work"][0];
            Assert.Equal("Construction", (string)work["work_type"]);
            Assert.Equal(2, (int)work["priority"]);
            Assert.Equal(2, (int)work["stored"]);
            Assert.False((bool)work["disabled"]);
        }

        [Fact]
        public void AnEmptyColonyAnswersNoPawns()
        {
            JObject json = Json(new ColonistsWorkPrioritiesDto());

            Assert.Equal(0, (int)json["total_pawns"]);
            Assert.Empty((JArray)json["pawns"]);
        }
    }
}
