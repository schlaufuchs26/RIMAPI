using System.Collections.Generic;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    /// <summary>
    /// One `dev` call (fox ticket #3757): the op plus the fields it reads. Every
    /// field is optional in the body because each op needs a different few; the
    /// service refuses a call whose op is missing its field instead of guessing
    /// (DevActionRules.Problem).
    /// </summary>
    public class DevActionRequest
    {
        /// <summary>One of DevActionRules.Ops, e.g. damage, hediff, incident, ticks.</summary>
        [ParameterDescription("One of DevActionRules.Ops, e.g.")]
        public string Op { get; set; }

        /// <summary>The pawn an op acts on, as the colonists read prints it.</summary>
        public int? PawnId { get; set; }

        /// <summary>
        /// The defName the op needs: an item, an animal kind, a hediff, a mental
        /// state, a skill, a trait, an incident, or a quest script. `debug_action`
        /// reads the dev-action menu's path here, `debug_actions` a substring to
        /// filter by.
        /// </summary>
        public string Name { get; set; }

        /// <summary>Damage in hit points, a hediff's severity, or silver for wealth.</summary>
        [ParameterDescription("Damage in hit points, a hediff's severity, or silver for wealth.")]
        public float? Amount { get; set; }

        /// <summary>A skill's new level, 0 to 20.</summary>
        [ParameterDescription("A skill's new level, 0 to 20.")]
        public int? Level { get; set; }

        /// <summary>How many items or animals to place.</summary>
        public int? Count { get; set; }

        /// <summary>
        /// `debug_actions` only: how many matching entries to skip before the
        /// answer starts listing, so a caller pages through a menu instead of
        /// guessing substrings (ticket #3947). Unset starts at the first match.
        /// </summary>
        [ParameterDescription("`debug_actions` only: how many matching entries to skip before the answer starts listing, so a caller pages through a menu instead of guessing substrings (ticket #3947).")]
        public int? Offset { get; set; }

        /// <summary>Ticks to run by hand.</summary>
        [ParameterDescription("Ticks to run by hand.")]
        public int? Ticks { get; set; }

        /// <summary>
        /// The body part a damage or hediff op acts on, by defName or label
        /// (leg, arm, head). Empty lets the game choose.
        /// </summary>
        [ParameterDescription("The body part a damage or hediff op acts on, by defName or label (leg, arm, head).")]
        public string Part { get; set; }

        /// <summary>The cell a spawn goes to; both unset means near a colonist.</summary>
        public int? X { get; set; }
        public int? Z { get; set; }

        /// <summary>
        /// `spawn_pawn` only: which faction the pawn joins, as the game names it
        /// (a faction's name or defName), or player, or none for a pawn no
        /// faction owns (a wild man). Empty takes the pawn kind's own default,
        /// which is what the game's dev spawner does.
        /// </summary>
        [ParameterDescription("`spawn_pawn` only: which faction the pawn joins, as the game names it (a faction's name or defName), or player, or none for a pawn no faction owns (a wild man).")]
        public string Faction { get; set; }

        /// <summary>
        /// `quest` only: take the quest the call just offered, through the same
        /// accept path the Quests tab runs, so its parts start and a pending
        /// delay can be found without a second call (fox ticket #4049).
        /// </summary>
        [ParameterDescription("`quest` only: take the quest the call just offered, through the same accept path the Quests tab runs, so its parts start and a pending delay can be found without a second call (fox ticket #4049).")]
        public bool? Accept { get; set; }

        /// <summary>
        /// `quest` with accept only: the reward choice's index when the quest
        /// offers several, 0 for the first (fox ticket #4049).
        /// </summary>
        [ParameterDescription("`quest` with accept only: the reward choice's index when the quest offers several, 0 for the first (fox ticket #4049).")]
        public int? Choice { get; set; }
    }

    /// <summary>
    /// What a `dev` call answers: the state the game holds after the op, read
    /// off the live objects rather than echoed from the request. Only the block
    /// the op touched is filled in.
    /// </summary>
    public class DevActionResultDto
    {
        [ParameterDescription("The dev action that ran.")]
        public string Op { get; set; }

        /// <summary>True when the game took the change.</summary>
        [ParameterDescription("True when the game took the change.")]
        public bool Done { get; set; }

        /// <summary>One sentence naming what changed, in the game's own words.</summary>
        [ParameterDescription("One sentence naming what changed, in the game's own words.")]
        public string Detail { get; set; }

        /// <summary>Real seconds the call spent, measured around the work.</summary>
        [ParameterDescription("Real seconds the call spent, measured around the work.")]
        public float Seconds { get; set; }

        [ParameterDescription("The pawn the action left, when it touched one.")]
        public DevPawnStateDto Pawn { get; set; }
        [ParameterDescription("The thing the action left, when it touched one.")]
        public DevThingStateDto Thing { get; set; }
        [ParameterDescription("The incident the action fired, when it fired one.")]
        public DevIncidentStateDto Incident { get; set; }
        [ParameterDescription("The tick state the action left.")]
        public DevTickStateDto Ticks { get; set; }
        [ParameterDescription("The wealth numbers the action changed.")]
        public DevWealthStateDto Wealth { get; set; }
        [ParameterDescription("The dev actions the menu offers.")]
        public DevActionsStateDto Actions { get; set; }
        [ParameterDescription("The quest delay the action set, in ticks.")]
        public DevQuestDelayStateDto QuestDelay { get; set; }
    }

    /// <summary>
    /// The quest delay a `quest_delay` op moved (ticket #4001): which quest,
    /// which part, and the span it held before and after, read off the live
    /// part rather than echoed from the request.
    /// </summary>
    public class DevQuestDelayStateDto
    {
        /// <summary>The quest's own id, as the `quests` read prints it.</summary>
        [ParameterDescription("The quest's own id, as the `quests` read prints it.")]
        public int QuestId { get; set; }

        /// <summary>The quest's name, e.g. "Hospitality: shelter".</summary>
        [ParameterDescription("The quest's name, e.g.")]
        public string QuestName { get; set; }

        /// <summary>The defName of the script the quest was built from.</summary>
        [ParameterDescription("The defName of the script the quest was built from.")]
        public string ScriptDefName { get; set; }

        /// <summary>The part's own type, e.g. QuestPart_Delay or QuestPart_ShuttleDelay.</summary>
        [ParameterDescription("The part's own type, e.g.")]
        public string PartType { get; set; }

        /// <summary>Ticks the delay held before the op.</summary>
        [ParameterDescription("Ticks the delay held before the op.")]
        public int Before { get; set; }

        /// <summary>Ticks the delay holds after the op; 0 when it ended in the call.</summary>
        [ParameterDescription("Ticks the delay holds after the op; 0 when it ended in the call.")]
        public int After { get; set; }

        /// <summary>True when the op ended the delay in the call instead of moving it.</summary>
        [ParameterDescription("True when the op ended the delay in the call instead of moving it.")]
        public bool Ended { get; set; }

        /// <summary>Other enabled delays in that quest, left as they were.</summary>
        [ParameterDescription("Other enabled delays in that quest, left as they were.")]
        public int Others { get; set; }
    }

    /// <summary>
    /// The game's own dev-action menu (fox ticket #3799): every entry the player
    /// finds under the debug dialog's Actions tab, so a check can look the tools
    /// up instead of waiting for a new op per state.
    /// </summary>
    public class DevActionsStateDto
    {
        /// <summary>
        /// Entries the walk visited. The menu's own sub-menus (pawn kinds,
        /// incidents) expand to thousands, so a walk that stopped early leaves
        /// this a lower bound (ticket #3855).
        /// </summary>
        [ParameterDescription("The entries the walk visited on its way.")]
        public int Walked { get; set; }
        /// <summary>How many of them this answer lists.</summary>
        [ParameterDescription("How many of them this answer lists.")]
        public int Shown { get; set; }

        /// <summary>True when the walk reached the menu's end, so Walked is the whole menu.</summary>
        [ParameterDescription("True when the walk reached the menu's end, so Walked is the whole menu.")]
        public bool Complete { get; set; }

        /// <summary>
        /// `debug_action` only: true when the colony's own counts and pawn states
        /// moved between the call's before and after (ticket #3922). An entry
        /// that needs something a call cannot give it runs without changing
        /// anything, and this is how the caller tells that apart from a built
        /// state.
        /// </summary>
        [ParameterDescription("`debug_action` only: true when the colony's own counts and pawn states moved between the call's before and after (ticket #3922).")]
        public bool Changed { get; set; }

        /// <summary>What moved, one phrase per counter, e.g. "pawns 12 -> 13"; empty when nothing did.</summary>
        [ParameterDescription("What moved, one phrase per counter, e.g.")]
        public List<string> Differences { get; set; } = new List<string>();

        [ParameterDescription("The menu's nodes, one per dev action.")]
        public List<DevActionNodeDto> Nodes { get; set; } = new List<DevActionNodeDto>();
    }

    /// <summary>One entry of the dev-action menu: its path, category and what it does.</summary>
    public class DevActionNodeDto
    {
        /// <summary>The path a `debug_action` call names, e.g. Actions\Spawning\WildMan.</summary>
        [ParameterDescription("The path a `debug_action` call names, e.g.")]
        public string Path { get; set; }

        [ParameterDescription("The category the node sits under.")]
        public string Category { get; set; }

        /// <summary>action (runs at once), menu (holds entries) or tool (acts on a clicked cell).</summary>
        [ParameterDescription("action (runs at once), menu (holds entries) or tool (acts on a clicked cell).")]
        public string Kind { get; set; }
    }

    /// <summary>One pawn after a dev op: health, wounds, hediffs, skills, traits.</summary>
    public class DevPawnStateDto
    {
        [ParameterDescription("The pawn's numeric id.")]
        public int Id { get; set; }
        public string Name { get; set; }
        public string DefName { get; set; }

        /// <summary>Share of the body's hit points left, 0 to 1.</summary>
        [ParameterDescription("Share of the body's hit points left, 0 to 1.")]
        public float HealthPercent { get; set; }

        [ParameterDescription("Whether the pawn is downed.")]
        public bool Downed { get; set; }
        [ParameterDescription("Whether the pawn is dead.")]
        public bool Dead { get; set; }

        /// <summary>The mental state's defName, empty when the pawn is calm.</summary>
        [ParameterDescription("The mental state's defName, empty when the pawn is calm.")]
        public string MentalState { get; set; }

        /// <summary>How many injuries the pawn carries (Hediff_Injury and missing parts).</summary>
        [ParameterDescription("How many injuries the pawn carries (Hediff_Injury and missing parts).")]
        public int Injuries { get; set; }

        /// <summary>The faction the pawn belongs to, as the game names it; empty when none owns him.</summary>
        [ParameterDescription("The faction the pawn belongs to, as the game names it; empty when none owns him.")]
        public string Faction { get; set; }

        /// <summary>True for a wild man or wild woman: a humanlike no faction owns.</summary>
        [ParameterDescription("True for a wild man or wild woman: a humanlike no faction owns.")]
        public bool Wild { get; set; }

        [ParameterDescription("The hediffs the pawn carries, one entry each.")]
        public List<DevHediffDto> Hediffs { get; set; } = new List<DevHediffDto>();
        [ParameterDescription("The pawn's skills, one entry each.")]
        public List<DevSkillDto> Skills { get; set; } = new List<DevSkillDto>();
        [ParameterDescription("The pawn's traits, one entry each.")]
        public List<string> Traits { get; set; } = new List<string>();
    }

    /// <summary>One hediff on the pawn, with the part it sits on.</summary>
    public class DevHediffDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How strong the hediff is.")]
        public float Severity { get; set; }
        [ParameterDescription("The body part the hediff sits on.")]
        public string Part { get; set; }
        [ParameterDescription("Whether the hediff is an injury.")]
        public bool Injury { get; set; }
        [ParameterDescription("Whether the hediff is tended.")]
        public bool Tended { get; set; }
    }

    /// <summary>One skill of the pawn, as the game holds it after the op.</summary>
    public class DevSkillDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The skill's level, 0 to 20.")]
        public int Level { get; set; }
        [ParameterDescription("The passion the skill carries: none, minor or major.")]
        public string Passion { get; set; }
    }

    /// <summary>What a spawn placed: the thing the game now holds on the map.</summary>
    public class DevThingStateDto
    {
        [ParameterDescription("The thing's numeric id.")]
        public int Id { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How many units the stack holds.")]
        public int StackCount { get; set; }
        public PositionDto Position { get; set; }
    }

    /// <summary>What an incident or quest left behind: the game's own record.</summary>
    public class DevIncidentStateDto
    {
        public string DefName { get; set; }

        /// <summary>The game's answer to TryExecute, or the quest it made.</summary>
        [ParameterDescription("The game's answer to TryExecute, or the quest it made.")]
        public bool Executed { get; set; }

        /// <summary>How many incidents the map's own log holds now.</summary>
        [ParameterDescription("How many incidents the map's own log holds now.")]
        public int Incidents { get; set; }

        /// <summary>The incident that fired last, by defName.</summary>
        [ParameterDescription("The incident that fired last, by defName.")]
        public string Newest { get; set; }

        /// <summary>How many letters stand on the stack, so a raid that wrote one is visible.</summary>
        [ParameterDescription("How many letters stand on the stack, so a raid that wrote one is visible.")]
        public int Letters { get; set; }
    }

    /// <summary>The clock before and after a `ticks` op.</summary>
    public class DevTickStateDto
    {
        [ParameterDescription("The tick before the action ran.")]
        public int Before { get; set; }
        [ParameterDescription("The tick after the action ran.")]
        public int After { get; set; }
        [ParameterDescription("How many ticks the action ran.")]
        public int Ran { get; set; }
    }

    /// <summary>Colony wealth before and after a `wealth` op.</summary>
    public class DevWealthStateDto
    {
        [ParameterDescription("The wealth before the action ran.")]
        public float Before { get; set; }
        [ParameterDescription("The wealth after the action ran.")]
        public float After { get; set; }

        /// <summary>The silver the op placed; wealth is derived, so this is what moved it.</summary>
        [ParameterDescription("The silver the op placed; wealth is derived, so this is what moved it.")]
        public float Added { get; set; }
    }
}
