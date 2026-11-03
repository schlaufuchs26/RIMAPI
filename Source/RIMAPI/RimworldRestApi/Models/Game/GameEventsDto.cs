using System.Collections.Generic;
using System.Linq;
using Verse.AI.Group;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class IncidentChanceDto
    {
        [ParameterDescription("The chance the incident fires, 0 to 1.")]
        public float Value { get; set; }
    }

    public class IncidentWeightDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The incident category the weight belongs to.")]
        public string Category { get; set; }
        [ParameterDescription("The weight the storyteller gives the category now.")]
        public float CurrentWeight { get; set; }
    }

    public class QuestsDto
    {
        [ParameterDescription("The quests the colony can still take.")]
        public List<QuestDto> ActiveQuests { get; set; } = new List<QuestDto>();
        [ParameterDescription("The quests the colony has finished or lost.")]
        public List<QuestDto> HistoricalQuests { get; set; } = new List<QuestDto>();
    }

    public class IncidentsDto
    {
        [ParameterDescription("One entry per incident the map has seen.")]
        public List<IncidentDto> Incidents { get; set; } = new List<IncidentDto>();
    }

    public class IncidentDto
    {
        [ParameterDescription("The incident's def name.")]
        public string IncidentDef { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The category the incident falls in.")]
        public string Category { get; set; }
        [ParameterDescription("The hour of the day the incident fired.")]
        public float IncidentHour { get; set; }
        [ParameterDescription("How many in-game days ago it fired.")]
        public float DaysSinceOccurred { get; set; }
    }

    public class IncidentDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The base chance the incident fires per check.")]
        public float BaseChance { get; set; }
        [ParameterDescription("The chance when Royalty content is loaded.")]
        public float BaseChanceWithRoyalty { get; set; }
        [ParameterDescription("The letter the incident sends, by def name.")]
        public string LetterDefName { get; set; }
        [ParameterDescription("How the incident changes the colony's population.")]
        public string PopulationEffect { get; set; }
        [ParameterDescription("The text the incident's letter shows.")]
        public string LetterText { get; set; }
        [ParameterDescription("The fewest colonists needed for it to fire.")]
        public int MinPopulation { get; set; }
        [ParameterDescription("The lowest threat points it fires at.")]
        public float MinThreatPoints { get; set; }
        [ParameterDescription("The highest threat points it fires at.")]
        public float MaxThreatPoints { get; set; }
        [ParameterDescription("The category the incident falls in.")]
        public string Category { get; set; }
        [ParameterDescription("Whether the recent-incident weighting skips it.")]
        public bool ShouldIgnoreRecentWeighting { get; set; }
        [ParameterDescription("The tags that decide when it may fire.")]
        public List<string> Tags { get; set; } = new List<string>();
        [ParameterDescription("The biomes where it never fires.")]
        public List<string> DisallowedBiomes { get; set; } = new List<string>();
        [ParameterDescription("The biomes where it may fire.")]
        public List<string> AllowedBiomes { get; set; } = new List<string>();
        [ParameterDescription("Whether colonists must stand on the map for it.")]
        public bool RequireColonistsPresent { get; set; }
    }

    public class GameConditionDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The text the condition's letter shows.")]
        public string LetterText { get; set; }
        [ParameterDescription("Whether the condition may last forever.")]
        public bool CanBePermanent { get; set; }
        [ParameterDescription("How much the condition moves the temperature, in Celsius.")]
        public float TemperatureOffset { get; set; }
        [ParameterDescription("The sky colour the condition draws.")]
        public float SkyTarget { get; set; }
        [ParameterDescription("How fast the sky fades into that colour.")]
        public float SkyTargetLerpFactor { get; set; }
    }

    public class QuestDto
    {
        [ParameterDescription("The quest's numeric id.")]
        public int Id { get; set; }
        [ParameterDescription("The quest's def name.")]
        public string QuestDef { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The quest's state, as the game names it.")]
        public string State { get; set; }
        [ParameterDescription("How many hours are left before it expires.")]
        public float ExpiryHours { get; set; }
        [ParameterDescription("What the quest pays out on completion.")]
        public List<string> Reward { get; set; }
        // The game's own answer for this quest, the report its accept button
        // greys out with (QuestUtility.CanAcceptQuest, ticket #3216).
        [ParameterDescription("The game's own answer for this quest, the report its accept button greys out with (QuestUtility.CanAcceptQuest, ticket #3216).")]
        public bool CanAccept { get; set; }
        // Why the game refuses the quest; empty when it can be accepted.
        [ParameterDescription("Why the game refuses the quest; empty when it can be accepted.")]
        public string AcceptReason { get; set; }
        // Whether the quest needs a named colonist to accept it; without one
        // the game takes the quest for the colony (Quest.Accept(null)).
        [ParameterDescription("Whether the quest needs a named colonist to accept it; without one the game takes the quest for the colony (Quest.Accept(null)).")]
        public bool RequiresAccepter { get; set; }
        // Reward choices the game draws one accept button per choice for. Empty
        // for a quest with one fixed reward; a quest with choices needs the
        // index to accept.
        [ParameterDescription("Reward choices the game draws one accept button per choice for.")]
        public List<QuestChoiceDto> Choices { get; set; } = new List<QuestChoiceDto>();
        // The accepter the game recorded, once the quest was taken.
        [ParameterDescription("The accepter the game recorded, once the quest was taken.")]
        public string Accepter { get; set; }
        // The tick the quest ended in: Quest.cleanupTick, which the game sets
        // when it cleans a quest up after the quest left the running state, -1
        // while it is on offer or running. The fox's finished-quest tail sorts
        // by it, so a quest created early and finished yesterday is not
        // dropped for one created late and finished days ago (fox ticket #4080).
        [ParameterDescription("The tick the quest ended in: Quest.cleanupTick, which the game sets when it cleans a quest up after the quest left the running state, -1 while it is on offer or running.")]
        public int EndedTick { get; set; }
        // How long ago that was, in game ticks (Quest.TicksSinceCleanup), -1
        // while the quest has not ended. Counted inside the game, so a caller
        // needs no clock of its own.
        [ParameterDescription("How long ago that was, in game ticks (Quest.TicksSinceCleanup), -1 while the quest has not ended.")]
        public int EndedTicksAgo { get; set; }
        // Why this quest could not be read, empty for a quest that reads
        // normally. One quest whose parts throw is reported here instead of
        // failing the whole list, so an offer standing beside it still reads
        // (fox ticket #4164).
        [ParameterDescription("Why this quest could not be read, empty for a quest that reads normally.")]
        public string ReadError { get; set; }
    }

    public class TriggerIncidentRequestDto
    {
        public string Name { get; set; }
        public string MapId { get; set; }
        [ParameterDescription("The parameters the incident is built with.")]
        public IncidentParmsDto IncidentParms { get; set; }
    }

    public class LordDto
    {
        [ParameterDescription("The lord's load id, which survives a save.")]
        public int LoadId { get; set; }
        [ParameterDescription("The faction's name, as the game shows it.")]
        public string FactionName { get; set; }
        [ParameterDescription("The faction's def name.")]
        public string FactionDefName { get; set; }
        [ParameterDescription("The lord job the group runs.")]
        public string LordJobType { get; set; }
        [ParameterDescription("The toil the group is in right now.")]
        public string CurrentToilName { get; set; }
        [ParameterDescription("How many ticks the group has spent in that toil.")]
        public int TicksInToil { get; set; }
        [ParameterDescription("How many of its pawns died violently.")]
        public int NumPawnsLostViolently { get; set; }
        [ParameterDescription("How many pawns it ever gained.")]
        public int NumPawnsEverGained { get; set; }
        [ParameterDescription("The pawns the lord owns, by numeric id.")]
        public List<string> OwnedPawnIds { get; set; }
        [ParameterDescription("The buildings the lord owns, by numeric id.")]
        public List<string> OwnedBuildingIds { get; set; }
        [ParameterDescription("The quest tags the lord carries, one per tag.")]
        public List<string> QuestTags { get; set; }
        [ParameterDescription("Whether the lord waits for a leave signal.")]
        public string InSignalLeave { get; set; }
        [ParameterDescription("Whether any of its pawns is still active.")]
        public bool AnyActivePawn { get; set; }

        public static LordDto ToDto(Lord lord)
        {
            return new LordDto
            {
                LoadId = lord.loadID,
                FactionName = lord.faction?.Name,
                FactionDefName = lord.faction?.def?.defName,
                LordJobType = lord.LordJob?.GetType().Name,
                CurrentToilName = lord.CurLordToil?.GetType().Name,
                TicksInToil = lord.ticksInToil,
                NumPawnsLostViolently = lord.numPawnsLostViolently,
                NumPawnsEverGained = lord.numPawnsEverGained,
                OwnedPawnIds = lord.ownedPawns.Select(p => p.ThingID).ToList(),
                OwnedBuildingIds = lord.ownedBuildings.Select(b => b.ThingID).ToList(),
                QuestTags = lord.questTags?.ToList(),
                InSignalLeave = lord.inSignalLeave,
                AnyActivePawn = lord.AnyActivePawn,
            };
        }
    }

    public class IncidentParmsDto
    {
        // Basic incident parameters
        [ParameterDescription("The thing the incident is aimed at.")]
        public string Target { get; set; }
        [ParameterDescription("The threat points the incident was generated with.")]
        public float Points { get; set; } = -1f;
        [ParameterDescription("The faction the incident comes from.")]
        public string Faction { get; set; }
        [ParameterDescription("Whether the call forced the incident.")]
        public bool Forced { get; set; }

        // Letter parameters
        [ParameterDescription("The letter label the call set.")]
        public string CustomLetterLabel { get; set; }
        [ParameterDescription("The letter text the call set.")]
        public string CustomLetterText { get; set; }
        [ParameterDescription("The letter def the call set.")]
        public string CustomLetterDef { get; set; }
        [ParameterDescription("Whether the incident sends a letter.")]
        public bool SendLetter { get; set; } = true;
        [ParameterDescription("The things the letter links to.")]
        public List<string> LetterHyperlinkThingDefs { get; set; }
        [ParameterDescription("The hediffs the letter links to.")]
        public List<string> LetterHyperlinkHediffDefs { get; set; }

        // Signal and spawn parameters
        [ParameterDescription("Signal and spawn parameters")]
        public string InSignalEnd { get; set; }
        [ParameterDescription("Whether the incident stays silent.")]
        public bool Silent { get; set; }
        [ParameterDescription("The cell the incident spawns at.")]
        public string SpawnCenter { get; set; }
        [ParameterDescription("The rotation the incident spawns with.")]
        public string SpawnRotation { get; set; }

        // Raid and combat parameters
        [ParameterDescription("Raid and combat parameters")]
        public bool GenerateFightersOnly { get; set; }
        [ParameterDescription("Whether raiders skip single-use launchers.")]
        public bool DontUseSingleUseRocketLaunchers { get; set; }
        [ParameterDescription("The strategy the raid follows, e.g. immediate attack.")]
        public string RaidStrategy { get; set; }
        [ParameterDescription("How the raid arrives: walking, drop pods or edge.")]
        public string RaidArrivalMode { get; set; }
        [ParameterDescription("Whether one raider is always left downed.")]
        public bool RaidForceOneDowned { get; set; }
        [ParameterDescription("Whether raiders never flee alone.")]
        public bool RaidNeverFleeIndividual { get; set; }
        [ParameterDescription("How a quick military aid raid arrives.")]
        public bool RaidArrivalModeForQuickMilitaryAid { get; set; }
        [ParameterDescription("The age band the raid draws from.")]
        public string RaidAgeRestriction { get; set; }

        // Biocode parameters
        [ParameterDescription("How often raiders carry biocoded weapons.")]
        public float BiocodeWeaponsChance { get; set; }
        [ParameterDescription("How often raiders wear biocoded apparel.")]
        public float BiocodeApparelChance { get; set; }

        // Pawn group parameters
        [ParameterDescription("The pawn groups the incident spawns, one entry each.")]
        public Dictionary<string, int> PawnGroups { get; set; }
        [ParameterDescription("The seed the pawn groups are made from.")]
        public int? PawnGroupMakerSeed { get; set; }
        [ParameterDescription("The ideoligion the spawned pawns follow.")]
        public string PawnIdeo { get; set; }
        [ParameterDescription("The lord the spawned pawns join.")]
        public string Lord { get; set; }
        [ParameterDescription("The pawn kind it spawns, by def name.")]
        public string PawnKind { get; set; }
        [ParameterDescription("How many pawns it spawns.")]
        public int PawnCount { get; set; }
        [ParameterDescription("The kind of pawn group it spawns, e.g. a raid.")]
        public string PawnGroupKind { get; set; }
        [ParameterDescription("The trader kind the incident brings, by def name.")]
        public string TraderKind { get; set; }

        // Quest parameters
        [ParameterDescription("The quest the incident belongs to.")]
        public string Quest { get; set; }
        [ParameterDescription("The quest script the incident starts.")]
        public string QuestScriptDef { get; set; }
        [ParameterDescription("The tag the incident puts on its quest.")]
        public string QuestTag { get; set; }

        // Mech cluster parameters
        [ParameterDescription("The mech cluster the incident drops.")]
        public string MechClusterSketch { get; set; }

        // Behavior parameters
        [ParameterDescription("Whether the attackers may give up and leave.")]
        public bool CanTimeoutOrFlee { get; set; } = true;
        [ParameterDescription("Whether the attackers may steal.")]
        public bool CanSteal { get; set; } = true;
        [ParameterDescription("Whether the attackers may kidnap.")]
        public bool CanKidnap { get; set; } = true;

        // Controller and location parameters
        [ParameterDescription("Controller and location parameters")]
        public string ControllerPawn { get; set; }
        [ParameterDescription("The cell the infestation is forced to.")]
        public string InfestationLocOverride { get; set; }

        // Target and gift parameters
        [ParameterDescription("Target and gift parameters")]
        public List<string> AttackTargets { get; set; }
        [ParameterDescription("The gifts the incident brings along.")]
        public List<string> Gifts { get; set; }

        // Body size parameters
        [ParameterDescription("The body size of everything it spawns.")]
        public float TotalBodySize { get; set; }

        // Psychic ritual parameters
        [ParameterDescription("The psychic ritual the incident starts.")]
        public string PsychicRitualDef { get; set; }

        // Multiplier parameters
        [ParameterDescription("How much the points are multiplied.")]
        public float PointMultiplier { get; set; } = 1f;
        [ParameterDescription("Whether the incident skips the storyteller's rules.")]
        public bool BypassStorytellerSettings { get; set; }

        // Generated pawns
        [ParameterDescription("Whether generated neutral pawns stay on the map.")]
        public List<string> StoreGeneratedNeutralPawns { get; set; }

        // Computed property
        [ParameterDescription("How many pawn groups it spawns.")]
        public int PawnGroupCount { get; set; }

        // Pod opening delay
        [ParameterDescription("How many ticks a drop pod takes to open.")]
        public int PodOpenDelay { get; set; } = 140;
    }
}
