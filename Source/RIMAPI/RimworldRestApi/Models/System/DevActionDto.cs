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
        /// state, a skill, a trait, an incident, or a quest script.
        /// </summary>
        public string Name { get; set; }

        /// <summary>Damage in hit points, a hediff's severity, or silver for wealth.</summary>
        public float? Amount { get; set; }

        /// <summary>A skill's new level, 0 to 20.</summary>
        public int? Level { get; set; }

        /// <summary>How many items or animals to place.</summary>
        public int? Count { get; set; }

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
