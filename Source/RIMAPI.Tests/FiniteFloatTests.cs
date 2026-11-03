using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// A float the game has not set (ticket #4315): `/api/v1/def/all` sent
    /// `mental_state_defs[].recovery_mtb_days` as the string "NaN", because
    /// Newtonsoft writes a NaN float that way. The field is nullable now, so
    /// the value travels as null and the schema says number-or-null.
    /// </summary>
    public class FiniteFloatTests
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new SnakeCaseContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
        };

        [Fact]
        public void AValueTheGameSetTravelsAsTheNumber()
        {
            Assert.Equal(2.5f, FiniteFloat.OrNull(2.5f));
        }

        [Fact]
        public void NaNTravelsAsNull()
        {
            // The measured case: a mental state def with no recovery time.
            Assert.Null(FiniteFloat.OrNull(float.NaN));
        }

        [Theory]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void AnInfinityTravelsAsNull(float value)
        {
            Assert.Null(FiniteFloat.OrNull(value));
        }

        [Fact]
        public void TheWireSendsNullRatherThanQuotingNaN()
        {
            JObject json = Json(new Average { Days = FiniteFloat.OrNull(float.NaN) });

            Assert.Equal(JTokenType.Null, json["days"].Type);
        }

        [Fact]
        public void TheWireCarriesASetValueAsANumber()
        {
            JObject json = Json(new Average { Days = FiniteFloat.OrNull(2.5f) });

            Assert.Equal(JTokenType.Float, json["days"].Type);
            Assert.Equal(2.5, (double)json["days"]);
        }

        private static JObject Json(Average value)
        {
            return JObject.Parse(JsonConvert.SerializeObject(value, Settings));
        }

        private class Average
        {
            // The way MentalStateDefDto.RecoveryMtbDays declares it: an unset
            // value is written as null instead of being left out.
            [JsonProperty(NullValueHandling = NullValueHandling.Include)]
            public float? Days { get; set; }
        }
    }
}
