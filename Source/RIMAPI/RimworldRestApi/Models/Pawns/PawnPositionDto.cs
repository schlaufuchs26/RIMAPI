using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class PawnPositionDto
    {
        [ParameterDescription("The pawn's numeric id.")]
        public int Id { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public int MapId { get; set; }
    }
}
