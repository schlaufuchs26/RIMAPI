using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// The body of POST /api/v1/map/shuttle/enter (fox ticket #3862): the pawn
    /// that boards and, when the map holds more than one shuttle, the one it
    /// boards. The route reads the body, the way every other write of the mod
    /// does, so a caller does not have to put the ids in the query string.
    /// </summary>
    public class ShuttleEnterRequestDto
    {
        public int PawnId { get; set; }

        /// <summary>The shuttle's thing id; 0 takes the only one on the pawn's map.</summary>
        [ParameterDescription("The shuttle's numeric id.")]
        public int ShuttleId { get; set; }
    }
}
