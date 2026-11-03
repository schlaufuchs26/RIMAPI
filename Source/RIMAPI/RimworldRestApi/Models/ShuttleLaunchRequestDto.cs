using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// The body of POST /api/v1/map/shuttle/launch (fox ticket #4150): the
    /// shuttle that leaves and, when it has to be found, the map it stands on.
    /// A send-away call names no pawn; the shuttle already holds everyone and
    /// everything its quest waits for.
    /// </summary>
    public class ShuttleLaunchRequestDto
    {
        /// <summary>The map the shuttle stands on; 0 takes the open one.</summary>
        public int MapId { get; set; }

        /// <summary>The shuttle's thing id; 0 takes the only one on the map.</summary>
        [ParameterDescription("The shuttle's numeric id.")]
        public int ShuttleId { get; set; }
    }
}
