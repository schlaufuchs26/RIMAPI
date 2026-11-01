using System.Collections.Generic;

namespace RIMAPI.Models
{
    /// <summary>
    /// One shuttle on the map (fox ticket #3862). A quest shuttle arrives to
    /// collect named pawns; the game's own lever is the right-click "Enter
    /// shuttle" on the shuttle, and the shuttle lists the people it waits for
    /// (CompShuttle.requiredPawns) plus the items it wants loaded. Colony 16
    /// lost the reward pawn of a hospitality quest because nothing could send
    /// the lodger onto the shuttle, and no read named the cell the shuttle
    /// stands on or the pawn it was waiting for.
    /// </summary>
    public class ShuttleDto
    {
        public int Id { get; set; }

        /// <summary>The thing's defName, e.g. Shuttle or PassengerShuttle.</summary>
        public string DefName { get; set; }

        public string Label { get; set; }

        /// <summary>The faction the shuttle belongs to; empty for an unowned one.</summary>
        public string Faction { get; set; }

        /// <summary>The cell the shuttle stands on, which is what an order needs.</summary>
        public PositionDto Position { get; set; }

        /// <summary>True while the shuttle is on the map; a shuttle that left is gone from the read.</summary>
        public bool Spawned { get; set; }

        /// <summary>True while the game loads the shuttle or holds it ready to launch.</summary>
        public bool Loading { get; set; }

        /// <summary>The game's own automatic-boarding toggle; off unless a caller switched it on.</summary>
        public bool Autoload { get; set; }

        /// <summary>True when every pawn and item the shuttle requires is aboard, which is what the quest checks.</summary>
        public bool AllRequiredLoaded { get; set; }

        /// <summary>The number of colonists the shuttle requires aboard; 0 when it names none.</summary>
        public int RequiredColonistCount { get; set; }

        /// <summary>True when the shuttle requires every colonist on the map aboard.</summary>
        public bool RequireAllColonistsOnMap { get; set; }

        /// <summary>
        /// The people the shuttle waits for, as the quest named them. A pawn
        /// that is downed or off the map is still listed here, because that is
        /// the state a run has to fix; the game's own RequiredPawns property
        /// would drop him silently.
        /// </summary>
        public List<ShuttlePawnDto> RequiredPawns { get; set; } = new List<ShuttlePawnDto>();

        /// <summary>The pawns already inside the shuttle.</summary>
        public List<ShuttlePawnDto> ContainedPawns { get; set; } = new List<ShuttlePawnDto>();

        /// <summary>The items the shuttle wants loaded, with how many are aboard.</summary>
        public List<ShuttleItemDto> RequiredItems { get; set; } = new List<ShuttleItemDto>();

        /// <summary>
        /// Ticks until the shuttle leaves on its own, from the quest part that
        /// sends it away (-1 when no quest part counts down for this shuttle).
        /// The letter says "leaves in 3 days"; this is the number behind it.
        /// </summary>
        public int LeavesInTicks { get; set; } = -1;

        /// <summary>True when the countdown part is critical, i.e. under one day.</summary>
        public bool LeavesSoon { get; set; }
    }

    /// <summary>One pawn a shuttle waits for, or one that is already aboard.</summary>
    public class ShuttlePawnDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        /// <summary>True when this pawn is inside the shuttle now.</summary>
        public bool Contained { get; set; }

        /// <summary>True when the pawn lies down and cannot walk to the shuttle on his own.</summary>
        public bool Downed { get; set; }

        /// <summary>True when the pawn stands on the shuttle's map.</summary>
        public bool OnMap { get; set; }

        public bool Dead { get; set; }

        /// <summary>What the pawn is to the colony: colonist, quest lodger, prisoner, animal, guest.</summary>
        public string Kind { get; set; }
    }

    /// <summary>One item a shuttle wants loaded, with the count that is aboard.</summary>
    public class ShuttleItemDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }

        /// <summary>How many the quest asks for.</summary>
        public int Required { get; set; }

        /// <summary>How many are inside the shuttle now.</summary>
        public int Loaded { get; set; }
    }

    /// <summary>
    /// What a boarding call did (fox ticket #3862): the game's own "Enter
    /// shuttle" path, reported as the state the game holds afterwards instead
    /// of an acknowledgement.
    /// </summary>
    public class ShuttleEnterResultDto
    {
        public int PawnId { get; set; }
        public string Pawn { get; set; }

        public int ShuttleId { get; set; }
        public string Shuttle { get; set; }
        public PositionDto Position { get; set; }

        /// <summary>True when the pawn was inside the shuttle before the call, so nothing was ordered.</summary>
        public bool AlreadyAboard { get; set; }

        /// <summary>True when the game took the job the order queued.</summary>
        public bool TookJob { get; set; }

        /// <summary>
        /// The colonist who carries a downed pawn in, empty when the pawn walks
        /// or nobody can carry him. The game's own "Carry to shuttle" puts a
        /// hauler in front of the shuttle, so the answer names him.
        /// </summary>
        public string CarriedBy { get; set; }

        /// <summary>The job the pawn holds after the call, as the game names it.</summary>
        public string Job { get; set; }

        /// <summary>True when the pawn is inside the shuttle after the call.</summary>
        public bool Contained { get; set; }

        /// <summary>True when the shuttle lists this pawn among the people it waits for.</summary>
        public bool Required { get; set; }

        /// <summary>The shuttle's state after the call, so a run sees what is still missing.</summary>
        public ShuttleDto ShuttleState { get; set; }
    }
}
