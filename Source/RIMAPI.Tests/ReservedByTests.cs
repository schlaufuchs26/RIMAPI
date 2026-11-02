using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RIMAPI.Core;
using RIMAPI.Models;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The reservation holder on the detailed colonist read (fox ticket #4172):
    /// the tend order could name the pawn that holds a patient only from its own
    /// refusal, so the read now carries the game's own reservation list. A
    /// renamed property would silently send a field the fox never reads.
    /// </summary>
    public class ReservedByTests
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
        public void TheHolderTravelsAsReservedBy()
        {
            var detail = new ApiV1PawnDetailedDto
            {
                ReservedBy = new List<ReservedByDto>
                {
                    new ReservedByDto
                    {
                        ClaimantId = 857,
                        Claimant = "Mangle",
                        Job = "TendPatient",
                    },
                },
            };

            JObject holder = (JObject)Json(detail)["reserved_by"][0];

            Assert.Equal(857, (int)holder["claimant_id"]);
            Assert.Equal("Mangle", (string)holder["claimant"]);
            Assert.Equal("TendPatient", (string)holder["job"]);
        }

        // A bridge that predates the field sends nothing, and the fox reads a
        // null list as "not known" rather than "nobody holds him": an empty
        // list is the game saying the pawn is free.
        [Fact]
        public void NoReservationSendsAnEmptyListRatherThanAPlaceholder()
        {
            JObject json = Json(new ApiV1PawnDetailedDto { ReservedBy = new List<ReservedByDto>() });

            Assert.Empty((JArray)json["reserved_by"]);
        }
    }
}
