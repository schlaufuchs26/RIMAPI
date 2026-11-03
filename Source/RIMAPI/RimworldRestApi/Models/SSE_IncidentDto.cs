using RIMAPI.Core;

namespace RIMAPI.SseModels
{
    public class IncidentLetterEvent
    {
        public string Label { get; set; }
        [ParameterDescription("The letter's text, as the game writes it.")]
        public string Text { get; set; }
        [ParameterDescription("The incident's def name.")]
        public string IncidentDef { get; set; }
        [ParameterDescription("The letter's def name.")]
        public string LetterDef { get; set; }
        [ParameterDescription("Whether the letter is urgent.")]
        public bool IsUrgent { get; set; }
        [ParameterDescription("The faction the incident involves, as the game names it.")]
        public FactionInfo Faction { get; set; }
        [ParameterDescription("The quest the incident belongs to.")]
        public QuestInfo Quest { get; set; }
        [ParameterDescription("The thing the incident is aimed at.")]
        public TargetInfoLite Target { get; set; }
        [ParameterDescription("What the camera should look at.")]
        public LookTargetsLite LookTargets { get; set; }
        [ParameterDescription("The parameters the incident was built with.")]
        public IncidentParmsLite Parms { get; set; }
    }

    public class FactionInfo
    {
        public string DefName { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The faction's numeric id.")]
        public int Id { get; set; }
    }

    public class QuestInfo
    {
        public string Name { get; set; }
        [ParameterDescription("The quest's numeric id.")]
        public int Id { get; set; }
    }

    public class TargetInfoLite
    {
        [ParameterDescription("What kind of thing the target is.")]
        public string Type { get; set; }
        public int? MapId { get; set; }
        [ParameterDescription("The world tile the target stands on.")]
        public int? Tile { get; set; }
        [ParameterDescription("The name of the thing the target belongs to.")]
        public string ParentName { get; set; }
    }

    public class LookTargetsLite
    {
        public int Count { get; set; }
        [ParameterDescription("The label of the main look target.")]
        public string PrimaryLabel { get; set; }
        [ParameterDescription("The def name of the main look target.")]
        public string PrimaryThingDef { get; set; }
        [ParameterDescription("The cell of the main look target.")]
        public string PrimaryPosition { get; set; }
        [ParameterDescription("The map of the main look target.")]
        public int? PrimaryMapId { get; set; }
    }

    public class IncidentParmsLite
    {
        [ParameterDescription("The points the incident was generated with.")]
        public float points { get; set; }
        [ParameterDescription("Whether the incident was forced.")]
        public bool forced { get; set; }
        [ParameterDescription("The cell the incident spawns at.")]
        public string SpawnCenter { get; set; }
        [ParameterDescription("The letter label the call set.")]
        public string CustomLetterLabel { get; set; }
        [ParameterDescription("The letter text the call set.")]
        public string CustomLetterText { get; set; }
    }
}
