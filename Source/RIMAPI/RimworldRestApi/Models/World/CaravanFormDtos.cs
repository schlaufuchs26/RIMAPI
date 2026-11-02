using System.Collections.Generic;

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
        public List<int> PawnIds { get; set; }

        /// <summary>Goods to load, by the thing id the things read prints.</summary>
        public List<CaravanItemRequest> Items { get; set; }

        /// <summary>The tile they travel to, as the world read prints it.</summary>
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
        public int Id { get; set; }
        public string Name { get; set; }
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
        public List<CaravanPawnDto> Pawns { get; set; }
        public List<string> Items { get; set; }
        public int ExitX { get; set; }
        public int ExitZ { get; set; }
        public int MeetingX { get; set; }
        public int MeetingZ { get; set; }
        public int StartingTile { get; set; }
        public int DirectionTile { get; set; }
        public int DestinationTile { get; set; }
        public float MassUsage { get; set; }
        public float MassCapacity { get; set; }
        public string Note { get; set; }
    }
}
