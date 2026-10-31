using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The mental-state flag on the detailed colonist read (fox ticket #3513):
    /// a colonist in a tantrum, daze or berserk cannot take a tend job, and the
    /// read carried no flag for it, so the fox's doctor search named him as a
    /// carer.
    /// </summary>
    public class MedicalInfoStateTests
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
        public void TheMentalStateTravelsAsMentalState()
        {
            JObject json = Json(new MedicalInfoDto { MentalState = "tantrum" });

            Assert.Equal("tantrum", (string)json["mental_state"]);
        }

        [Fact]
        public void AnUnsetMentalStateSendsNullRatherThanAState()
        {
            JObject json = Json(new MedicalInfoDto());

            Assert.Equal(JTokenType.Null, json["mental_state"].Type);
        }
    }
}
