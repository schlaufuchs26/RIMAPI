using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// Forming a caravan (fox ticket #4212, wave 2): the pawns and the goods
    /// that leave, and the tile they travel to. The route runs the same
    /// CaravanFormingUtility.StartFormingCaravan the dialog's Send button runs,
    /// so the pawns gather at the packing spot, haul the named goods and walk
    /// out through the game's own exit.
    /// </summary>
    public class FormCaravanRequest
    {
        /// <summary>The pawns that go; the ids the colonists read prints.</summary>
        [ParameterDescription("The pawns that go; the ids the colonists read prints.")]
        public List<int> PawnIds { get; set; }

        /// <summary>Goods to load, by the thing id the things read prints.</summary>
        [ParameterDescription("Goods to load, by the thing id the things read prints.")]
        public List<CaravanItemRequest> Items { get; set; }

        /// <summary>The tile they travel to, as the world read prints it.</summary>
        [ParameterDescription("The tile they travel to, as the world read prints it.")]
        public int DestinationTile { get; set; }

        public int MapId { get; set; }
    }

    /// <summary>One stack that goes with the caravan.</summary>
    public class CaravanItemRequest
    {
        public int ThingId { get; set; }
        public int Count { get; set; }
    }

    /// <summary>One pawn of the caravan as the answer reads him back.</summary>
    public class CaravanPawnDto
    {
        [ParameterDescription("The pawn's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }
        [ParameterDescription("Whether the pawn is downed.")]
        public bool Downed { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
    }

    /// <summary>
    /// What the call started: who goes, what they carry, where they gather and
    /// which cell they leave through. The caravan itself appears in
    /// /api/v1/world/caravans once the game's own gather and walk-out finished,
    /// which takes game time; the answer names that so a caller waits instead of
    /// forming a second one.
    /// </summary>
    public class FormCaravanResultDto
    {
        [ParameterDescription("The pawns the caravan took with it.")]
        public List<CaravanPawnDto> Pawns { get; set; }
        [ParameterDescription("The item stacks it took.")]
        public List<string> Items { get; set; }
        [ParameterDescription("The cell the caravan leaves from, x.")]
        public int ExitX { get; set; }
        [ParameterDescription("The cell the caravan leaves from, z.")]
        public int ExitZ { get; set; }
        [ParameterDescription("The cell the pawns gather at, x.")]
        public int MeetingX { get; set; }
        [ParameterDescription("The cell the pawns gather at, z.")]
        public int MeetingZ { get; set; }
        [ParameterDescription("The world tile the caravan forms on.")]
        public int StartingTile { get; set; }
        [ParameterDescription("The tile it heads toward.")]
        public int DirectionTile { get; set; }
        [ParameterDescription("The tile the player picked as destination.")]
        public int DestinationTile { get; set; }
        [ParameterDescription("How much mass the caravan carries, in kilograms.")]
        public float MassUsage { get; set; }
        [ParameterDescription("How much its people and animals can carry.")]
        public float MassCapacity { get; set; }
        [ParameterDescription("A sentence about what the game did.")]
        public string Note { get; set; }
    }
}
