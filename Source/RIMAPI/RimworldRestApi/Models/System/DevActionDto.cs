using System.Collections.Generic;

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
        public float? Amount { get; set; }

        /// <summary>A skill's new level, 0 to 20.</summary>
        public int? Level { get; set; }

        /// <summary>How many items or animals to place.</summary>
        public int? Count { get; set; }

        /// <summary>
        /// `debug_actions` only: how many matching entries to skip before the
        /// answer starts listing, so a caller pages through a menu instead of
        /// guessing substrings (ticket #3947). Unset starts at the first match.
        /// </summary>
        public int? Offset { get; set; }

        /// <summary>Ticks to run by hand.</summary>
        public int? Ticks { get; set; }

        /// <summary>
        /// The body part a damage or hediff op acts on, by defName or label
        /// (leg, arm, head). Empty lets the game choose.
        /// </summary>
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
        public string Faction { get; set; }
    }

    /// <summary>
    /// What a `dev` call answers: the state the game holds after the op, read
    /// off the live objects rather than echoed from the request. Only the block
    /// the op touched is filled in.
    /// </summary>
    public class DevActionResultDto
    {
        public string Op { get; set; }

        /// <summary>True when the game took the change.</summary>
        public bool Done { get; set; }

        /// <summary>One sentence naming what changed, in the game's own words.</summary>
        public string Detail { get; set; }

        /// <summary>Real seconds the call spent, measured around the work.</summary>
        public float Seconds { get; set; }

        public DevPawnStateDto Pawn { get; set; }
        public DevThingStateDto Thing { get; set; }
        public DevIncidentStateDto Incident { get; set; }
        public DevTickStateDto Ticks { get; set; }
        public DevWealthStateDto Wealth { get; set; }
        public DevActionsStateDto Actions { get; set; }
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
        public int Walked { get; set; }
        /// <summary>How many of them this answer lists.</summary>
        public int Shown { get; set; }

        /// <summary>True when the walk reached the menu's end, so Walked is the whole menu.</summary>
        public bool Complete { get; set; }

        /// <summary>
        /// `debug_action` only: true when the colony's own counts and pawn states
        /// moved between the call's before and after (ticket #3922). An entry
        /// that needs something a call cannot give it runs without changing
        /// anything, and this is how the caller tells that apart from a built
        /// state.
        /// </summary>
        public bool Changed { get; set; }

        /// <summary>What moved, one phrase per counter, e.g. "pawns 12 -> 13"; empty when nothing did.</summary>
        public List<string> Differences { get; set; } = new List<string>();

        public List<DevActionNodeDto> Nodes { get; set; } = new List<DevActionNodeDto>();
    }

    /// <summary>One entry of the dev-action menu: its path, category and what it does.</summary>
    public class DevActionNodeDto
    {
        /// <summary>The path a `debug_action` call names, e.g. Actions\Spawning\WildMan.</summary>
        public string Path { get; set; }

        public string Category { get; set; }

        /// <summary>action (runs at once), menu (holds entries) or tool (acts on a clicked cell).</summary>
        public string Kind { get; set; }
    }

    /// <summary>One pawn after a dev op: health, wounds, hediffs, skills, traits.</summary>
    public class DevPawnStateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DefName { get; set; }

        /// <summary>Share of the body's hit points left, 0 to 1.</summary>
        public float HealthPercent { get; set; }

        public bool Downed { get; set; }
        public bool Dead { get; set; }

        /// <summary>The mental state's defName, empty when the pawn is calm.</summary>
        public string MentalState { get; set; }

        /// <summary>How many injuries the pawn carries (Hediff_Injury and missing parts).</summary>
        public int Injuries { get; set; }

        /// <summary>The faction the pawn belongs to, as the game names it; empty when none owns him.</summary>
        public string Faction { get; set; }

        /// <summary>True for a wild man or wild woman: a humanlike no faction owns.</summary>
        public bool Wild { get; set; }

        public List<DevHediffDto> Hediffs { get; set; } = new List<DevHediffDto>();
        public List<DevSkillDto> Skills { get; set; } = new List<DevSkillDto>();
        public List<string> Traits { get; set; } = new List<string>();
    }

    /// <summary>One hediff on the pawn, with the part it sits on.</summary>
    public class DevHediffDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public float Severity { get; set; }
        public string Part { get; set; }
        public bool Injury { get; set; }
        public bool Tended { get; set; }
    }

    /// <summary>One skill of the pawn, as the game holds it after the op.</summary>
    public class DevSkillDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public int Level { get; set; }
        public string Passion { get; set; }
    }

    /// <summary>What a spawn placed: the thing the game now holds on the map.</summary>
    public class DevThingStateDto
    {
        public int Id { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        public int StackCount { get; set; }
        public PositionDto Position { get; set; }
    }

    /// <summary>What an incident or quest left behind: the game's own record.</summary>
    public class DevIncidentStateDto
    {
        public string DefName { get; set; }

        /// <summary>The game's answer to TryExecute, or the quest it made.</summary>
        public bool Executed { get; set; }

        /// <summary>How many incidents the map's own log holds now.</summary>
        public int Incidents { get; set; }

        /// <summary>The incident that fired last, by defName.</summary>
        public string Newest { get; set; }

        /// <summary>How many letters stand on the stack, so a raid that wrote one is visible.</summary>
        public int Letters { get; set; }
    }

    /// <summary>The clock before and after a `ticks` op.</summary>
    public class DevTickStateDto
    {
        public int Before { get; set; }
        public int After { get; set; }
        public int Ran { get; set; }
    }

    /// <summary>Colony wealth before and after a `wealth` op.</summary>
    public class DevWealthStateDto
    {
        public float Before { get; set; }
        public float After { get; set; }

        /// <summary>The silver the op placed; wealth is derived, so this is what moved it.</summary>
        public float Added { get; set; }
    }
}
