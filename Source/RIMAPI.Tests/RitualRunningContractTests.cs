using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The running-ritual block (ticket #3364): the leader's speech lasts two
    /// in-game hours by its own def, and the game's verdict for it is one
    /// sentence ("already in progress"), so the read has to carry the clock, the
    /// stage and the pawns' own jobs. These tests pin the keys the fox's
    /// `rituals` line reads and the attendance rule the entry's verdict rests on.
    /// </summary>
    public class RitualRunningContractTests
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new SnakeCaseContractResolver(),
        };

        private static JObject Json(object value) =>
            JObject.Parse(JsonConvert.SerializeObject(value, Settings));

        [Fact]
        public void TheRunningBlockKeepsTheKeysTheToolReads()
        {
            var expected = new[]
            {
                "attended",
                "cancelled",
                "def",
                "duration_ticks",
                "organizer",
                "organizer_id",
                "pawns",
                "progress",
                "ritual",
                "stage_count",
                "stage_index",
                "ticks_left",
            };

            JObject json = Json(new RitualRunningDto());

            Assert.Equal(expected, json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void AParticipantCarriesItsJobAndItsRitualDuty()
        {
            // The two fields are the point of the block: a pawn whose job is
            // back on Sleep and whose duty is empty has left the ceremony, and
            // that is what a stuck ritual looks like.
            JObject json = Json(new RitualParticipantDto
            {
                Id = 693,
                Name = "Maskinnen",
                Job = "Sleep",
                Duty = "",
            });

            Assert.Equal(
                new[] { "duty", "id", "job", "name" },
                json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
            Assert.Equal("Sleep", (string)json["job"]);
        }

        [Fact]
        public void TheEndAnswerKeepsTheKeysTheToolReads()
        {
            var expected = new[]
            {
                "def",
                "ended",
                "ideo",
                "pawns",
                "reason",
                "ritual",
                "was_attended",
            };

            JObject json = Json(new RitualEndResultDto());

            Assert.Equal(expected, json.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public void TheRunningBlockHangsOffTheRitualEntry()
        {
            // The tool reads it as ritual.running, so the nested key is part of
            // the contract, not an implementation detail.
            JObject json = Json(new IdeoRitualDto
            {
                Def = "LeaderSpeech",
                Running = new RitualRunningDto { Ritual = "Leader speech", TicksLeft = 4200 },
            });

            Assert.Equal("Leader speech", (string)json["running"]["ritual"]);
            Assert.Equal(4200, (int)json["running"]["ticks_left"]);
        }

        [Fact]
        public void ARitualWithoutARunningJobCarriesNoBlock()
        {
            JObject json = Json(new IdeoRitualDto { Def = "LeaderSpeech" });

            Assert.Equal(JTokenType.Null, json["running"].Type);
        }

        [Fact]
        public void AttendedMeansOnePawnStillHoldsARitualDuty()
        {
            var ritualDuties = new List<string> { "Spectate", "GiveSpeech" };

            Assert.True(RitualDutyHelper.AnyoneOnDuty(
                new[] { "Spectate", "GiveSpeech" }, ritualDuties));
            Assert.True(RitualDutyHelper.AnyoneOnDuty(
                new[] { "", "GiveSpeech" }, ritualDuties));
            Assert.False(RitualDutyHelper.AnyoneOnDuty(
                new[] { "Sleep", "Eat" }, ritualDuties));
            Assert.False(RitualDutyHelper.AnyoneOnDuty(
                new[] { "", "" }, ritualDuties));
        }

        [Fact]
        public void AnEmptyDutySetIsNotJudged()
        {
            // A third-party ritual whose stages name no duty: the read then
            // reports the pawns' jobs and claims nothing about attendance.
            Assert.True(RitualDutyHelper.AnyoneOnDuty(new[] { "Sleep" }, new string[0]));
            Assert.True(RitualDutyHelper.AnyoneOnDuty(null, new[] { "Spectate" }));
        }
    }
}
