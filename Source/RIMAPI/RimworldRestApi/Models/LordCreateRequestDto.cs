using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class LordCreateRequestDto
    {
        // Who owns this squad?
        [ParameterDescription("The faction to spawn the lord's pawns from.")]
        public string Faction { get; set; }
        public string MapId { get; set; }

        // Who joins this squad?
        [ParameterDescription("The pawns the lord takes, by numeric id.")]
        public List<int> PawnIds { get; set; }

        // What should they do?
        // Options: "AssaultColony", "AssaultThings", "DefendPoint", "Wander"
        [ParameterDescription("The lord job to give, e.g. a raid or a siege.")]
        public string JobType { get; set; } = "AssaultColony";

        // For "DefendPoint"
        public PositionDto Position { get; set; }

        // For "AssaultThings" (List of Enemy Pawn IDs to attack)
        [ParameterDescription("The targets the lord moves against, by numeric id.")]
        public List<int> TargetIds { get; set; }
    }

    public class LordCreateDto
    {
        [ParameterDescription("The numeric id of the lord that was created.")]
        public int LordId { get; set; }
        [ParameterDescription("How many pawns the lord holds.")]
        public int MemberCount { get; set; }
    }
}