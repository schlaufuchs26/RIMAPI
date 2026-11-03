using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class CaravanDto
    {
        [ParameterDescription("The caravan's numeric id, as the caravan list reports it.")]
        public int Id { get; set; }
        public string Name { get; set; }
        [ParameterDescription("Whether the caravan is the player's own.")]
        public bool IsPlayerControlled { get; set; }
        public PositionDto Position { get; set; }
        [ParameterDescription("The world tile the caravan stands on.")]
        public int Tile { get; set; }
        [ParameterDescription("The pawns travelling with the caravan.")]
        public List<PawnDto> Pawns { get; set; }
        [ParameterDescription("The item stacks the caravan carries.")]
        public List<ThingDto> Items { get; set; }
        [ParameterDescription("How much mass the caravan carries, in kilograms.")]
        public float MassUsage { get; set; }
        [ParameterDescription("How much mass its people and animals can carry.")]
        public float MassCapacity { get; set; }
        [ParameterDescription("How much food the tile offers the caravan, 0 to 1.")]
        public string Forageability { get; set; }
        [ParameterDescription("How easily other factions spot the caravan, 0 to 1.")]
        public string Visibility { get; set; }
        [ParameterDescription("How many in-game days the caravan needs to reach its destination.")]
        public float DaysToArrive { get; set; }
    }

    public class CaravanPathDto
    {
        [ParameterDescription("The caravan's numeric id.")]
        public int Id { get; set; }
        [ParameterDescription("Whether the caravan is walking right now.")]
        public bool Moving { get; set; }
        [ParameterDescription("The world tile the caravan stands on.")]
        public int CurrentTile { get; set; }
        [ParameterDescription("The tile it is walking toward.")]
        public int NextTile { get; set; }
        [ParameterDescription("How far along the current step it is, 0 to 1.")]
        public float Progress { get; set; }
        [ParameterDescription("The tile its path ends on.")]
        public int DestinationTile { get; set; }
        [ParameterDescription("Every tile of the path, in walking order.")]
        public List<int> Path { get; set; }
    }
}