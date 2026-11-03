using System.Collections.Generic;
using RIMAPI.Core;

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
        [ParameterDescription("The shuttle's numeric id.")]
        public int Id { get; set; }

        /// <summary>The thing's defName, e.g. Shuttle or PassengerShuttle.</summary>
        public string DefName { get; set; }

        public string Label { get; set; }

        /// <summary>The faction the shuttle belongs to; empty for an unowned one.</summary>
        [ParameterDescription("The faction the shuttle belongs to; empty for an unowned one.")]
        public string Faction { get; set; }

        /// <summary>The cell the shuttle stands on, which is what an order needs.</summary>
        public PositionDto Position { get; set; }

        /// <summary>True while the shuttle is on the map; a shuttle that left is gone from the read.</summary>
        [ParameterDescription("True while the shuttle is on the map; a shuttle that left is gone from the read.")]
        public bool Spawned { get; set; }

        /// <summary>True while the game loads the shuttle or holds it ready to launch.</summary>
        [ParameterDescription("True while the game loads the shuttle or holds it ready to launch.")]
        public bool Loading { get; set; }

        /// <summary>The game's own automatic-boarding toggle; off unless a caller switched it on.</summary>
        [ParameterDescription("The game's own automatic-boarding toggle; off unless a caller switched it on.")]
        public bool Autoload { get; set; }

        /// <summary>True when every pawn and item the shuttle requires is aboard, which is what the quest checks.</summary>
        [ParameterDescription("True when every pawn and item the shuttle requires is aboard, which is what the quest checks.")]
        public bool AllRequiredLoaded { get; set; }

        /// <summary>
        /// The game's own reason it would refuse a launch right now, empty when
        /// it would take one (CompShuttle.CanLaunch, ticket #4154). A quest
        /// shuttle with everything aboard and no colonist with piloting reads
        /// "Cannot launch: no pilot aboard." here, which is why the send-away
        /// call refuses instead of letting the game fail it.
        /// </summary>
        [ParameterDescription("The game's own reason it would refuse a launch right now, empty when it would take one (CompShuttle.CanLaunch, ticket #4154).")]
        public string LaunchProblem { get; set; }

        /// <summary>
        /// True when the game asks this shuttle for a pilot before it launches:
        /// the shipDef is a player shuttle, which is the branch CanLaunch
        /// checks. Deliberately not CompShuttle.IsPlayerShuttle, which also
        /// asks ModLister.OdysseyInstalled and therefore reads false in a
        /// Steam-less debug clone while CanLaunch still refuses for the pilot
        /// (measured 2026-11-02, fox ticket #4155). A player shuttle needs a
        /// free colonist aboard whose piloting ability clears the game's 10%
        /// floor; a quest shuttle whose def is not a player shuttle needs none.
        /// </summary>
        [ParameterDescription("True when the game asks this shuttle for a pilot before it launches: the shipDef is a player shuttle, which is the branch CanLaunch checks.")]
        public bool PilotRequired { get; set; }

        /// <summary>
        /// The game's own CompShuttle.HasPilot: a free colonist inside the
        /// shuttle who can fly it. False while nobody aboard can, which is the
        /// state CanLaunch refuses with "No colonist capable of piloting
        /// shuttle." A bridge from before the field sends false as well, which
        /// is why the read prints the pilot state only for a shuttle that
        /// carries PilotRequired.
        /// </summary>
        [ParameterDescription("The game's own CompShuttle.HasPilot: a free colonist inside the shuttle who can fly it.")]
        public bool HasPilot { get; set; }

        /// <summary>The number of colonists the shuttle requires aboard; 0 when it names none.</summary>
        [ParameterDescription("The number of colonists the shuttle requires aboard; 0 when it names none.")]
        public int RequiredColonistCount { get; set; }

        /// <summary>True when the shuttle requires every colonist on the map aboard.</summary>
        [ParameterDescription("True when the shuttle requires every colonist on the map aboard.")]
        public bool RequireAllColonistsOnMap { get; set; }

        /// <summary>
        /// The people the shuttle waits for, as the quest named them. A pawn
        /// that is downed or off the map is still listed here, because that is
        /// the state a run has to fix; the game's own RequiredPawns property
        /// would drop him silently.
        /// </summary>
        [ParameterDescription("The people the shuttle waits for, as the quest named them.")]
        public List<ShuttlePawnDto> RequiredPawns { get; set; } = new List<ShuttlePawnDto>();

        /// <summary>The pawns already inside the shuttle.</summary>
        [ParameterDescription("The pawns already inside the shuttle.")]
        public List<ShuttlePawnDto> ContainedPawns { get; set; } = new List<ShuttlePawnDto>();

        /// <summary>The items the shuttle wants loaded, with how many are aboard.</summary>
        [ParameterDescription("The items the shuttle wants loaded, with how many are aboard.")]
        public List<ShuttleItemDto> RequiredItems { get; set; } = new List<ShuttleItemDto>();

        /// <summary>
        /// Ticks until the shuttle leaves on its own, from the quest part that
        /// sends it away (-1 when no quest part counts down for this shuttle).
        /// The letter says "leaves in 3 days"; this is the number behind it.
        /// </summary>
        [ParameterDescription("Ticks until the shuttle leaves on its own, from the quest part that sends it away (-1 when no quest part counts down for this shuttle).")]
        public int LeavesInTicks { get; set; } = -1;

        /// <summary>True when the countdown part is critical, i.e. under one day.</summary>
        [ParameterDescription("True when the countdown part is critical, i.e.")]
        public bool LeavesSoon { get; set; }
    }

    /// <summary>One pawn a shuttle waits for, or one that is already aboard.</summary>
    public class ShuttlePawnDto
    {
        [ParameterDescription("The pawn's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }

        /// <summary>True when this pawn is inside the shuttle now.</summary>
        [ParameterDescription("True when this pawn is inside the shuttle now.")]
        public bool Contained { get; set; }

        /// <summary>True when the pawn lies down and cannot walk to the shuttle on his own.</summary>
        [ParameterDescription("True when the pawn lies down and cannot walk to the shuttle on his own.")]
        public bool Downed { get; set; }

        /// <summary>True when the pawn stands on the shuttle's map.</summary>
        [ParameterDescription("True when the pawn stands on the shuttle's map.")]
        public bool OnMap { get; set; }

        [ParameterDescription("Whether the pawn is dead.")]
        public bool Dead { get; set; }

        /// <summary>What the pawn is to the colony: colonist, quest lodger, prisoner, animal, guest.</summary>
        [ParameterDescription("What the pawn is to the colony: colonist, quest lodger, prisoner, animal, guest.")]
        public string Kind { get; set; }
    }

    /// <summary>One item a shuttle wants loaded, with the count that is aboard.</summary>
    public class ShuttleItemDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }

        /// <summary>How many the quest asks for.</summary>
        [ParameterDescription("How many the quest asks for.")]
        public int Required { get; set; }

        /// <summary>How many are inside the shuttle now.</summary>
        [ParameterDescription("How many are inside the shuttle now.")]
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
        [ParameterDescription("The pawn the call boarded, as the game names it.")]
        public string Pawn { get; set; }

        [ParameterDescription("The shuttle's numeric id.")]
        public int ShuttleId { get; set; }
        [ParameterDescription("The shuttle, as the game names it.")]
        public string Shuttle { get; set; }
        public PositionDto Position { get; set; }

        /// <summary>True when the pawn was inside the shuttle before the call, so nothing was ordered.</summary>
        [ParameterDescription("True when the pawn was inside the shuttle before the call, so nothing was ordered.")]
        public bool AlreadyAboard { get; set; }

        /// <summary>True when the game took the job the order queued.</summary>
        [ParameterDescription("True when the game took the job the order queued.")]
        public bool TookJob { get; set; }

        /// <summary>
        /// The colonist who carries a downed pawn in, empty when the pawn walks
        /// or nobody can carry him. The game's own "Carry to shuttle" puts a
        /// hauler in front of the shuttle, so the answer names him.
        /// </summary>
        [ParameterDescription("The colonist who carries a downed pawn in, empty when the pawn walks or nobody can carry him.")]
        public string CarriedBy { get; set; }

        /// <summary>The job the pawn holds after the call, as the game names it.</summary>
        [ParameterDescription("The job the pawn holds after the call, as the game names it.")]
        public string Job { get; set; }

        /// <summary>True when the pawn is inside the shuttle after the call.</summary>
        [ParameterDescription("True when the pawn is inside the shuttle after the call.")]
        public bool Contained { get; set; }

        /// <summary>True when the shuttle lists this pawn among the people it waits for.</summary>
        [ParameterDescription("True when the shuttle lists this pawn among the people it waits for.")]
        public bool Required { get; set; }

        /// <summary>The shuttle's state after the call, so a run sees what is still missing.</summary>
        [ParameterDescription("The shuttle's state after the call, so a run sees what is still missing.")]
        public ShuttleDto ShuttleState { get; set; }
    }

    /// <summary>
    /// What a send-away call did (fox ticket #4150): the game's own "Send
    /// shuttle" lever, reported as the state the game holds afterwards. The
    /// shuttle is gone from the map in the same call, so the answer carries the
    /// cell it left from and whether anything is still aboard.
    /// </summary>
    public class ShuttleLaunchResultDto
    {
        [ParameterDescription("The shuttle's numeric id.")]
        public int ShuttleId { get; set; }
        [ParameterDescription("The shuttle, as the game names it.")]
        public string Shuttle { get; set; }

        /// <summary>The cell the shuttle stood on when the call sent it away.</summary>
        public PositionDto Position { get; set; }

        /// <summary>True when the shuttle is off the map after the call.</summary>
        [ParameterDescription("True when the shuttle is off the map after the call.")]
        public bool Left { get; set; }

        /// <summary>
        /// The ship job the shuttle holds after the call ("FlyAway"), empty when
        /// the game already disposed the ship. FlyAway hands the shuttle's
        /// contents to the leaving skyfaller and tells the quest "FlewAway".
        /// </summary>
        [ParameterDescription("The ship job the shuttle holds after the call (\"FlyAway\"), empty when the game already disposed the ship.")]
        public string Job { get; set; }
    }
}
