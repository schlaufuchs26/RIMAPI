using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class AllDefsRequestDto
    {
        [ParameterDescription("The def kinds to return, by name.")]
        public List<string> Filters { get; set; }
    }

    public class DefsDto
    {
        [ParameterDescription("Every thing def the game holds.")]
        public List<ThingDefDto> ThingsDefs { get; set; }
        [ParameterDescription("Every incident def the game holds.")]
        public List<IncidentDefDto> IncidentsDefs { get; set; }
        [ParameterDescription("Every quest and ritual condition def.")]
        public List<GameConditionDefDto> ConditionsDefs { get; set; }
        [ParameterDescription("Every pawn kind def the game holds.")]
        public List<PawnKindDefDto> PawnKindDefs { get; set; }
        [ParameterDescription("Every trait def the game holds.")]
        public List<TraitDefDto> TraitDefs { get; set; }
        [ParameterDescription("Every research project def.")]
        public List<ResearchProjectDefDto> ResearchDefs { get; set; }
        [ParameterDescription("Every hediff def the game holds.")]
        public List<HediffDefDto> HediffDefs { get; set; }
        [ParameterDescription("Every skill def the game holds.")]
        public List<SkillDefDto> SkillDefs { get; set; }
        [ParameterDescription("Every work type def the game holds.")]
        public List<WorkTypeDefDto> WorkTypeDefs { get; set; }
        [ParameterDescription("Every need def the game holds.")]
        public List<NeedDefDto> NeedDefs { get; set; }
        [ParameterDescription("Every thought def the game holds.")]
        public List<ThoughtDefDto> ThoughtDefs { get; set; }
        [ParameterDescription("Every stat def the game holds.")]
        public List<StatDefDto> StatDefs { get; set; }
        [ParameterDescription("Every world object def the game holds.")]
        public List<WorldObjectDefDto> WorldObjectDefs { get; set; }
        [ParameterDescription("Every biome def the game holds.")]
        public List<BiomeDefDto> BiomeDefs { get; set; }
        [ParameterDescription("Every terrain def the game holds.")]
        public List<TerrainDefDto> TerrainDefs { get; set; }
        [ParameterDescription("Every recipe def the game holds.")]
        public List<RecipeDefDto> RecipeDefs { get; set; }
        [ParameterDescription("Every body def the game holds.")]
        public List<BodyDefDto> BodyDefs { get; set; }
        [ParameterDescription("Every body part def the game holds.")]
        public List<BodyPartDefDto> BodyPartDefs { get; set; }
        [ParameterDescription("Every faction def the game holds.")]
        public List<FactionDefDto> FactionDefs { get; set; }
        [ParameterDescription("Every sound def the game holds.")]
        public List<SoundDefDto> SoundDefs { get; set; }
        [ParameterDescription("Every designation category def.")]
        public List<DesignationCategoryDefDto> DesignationCategoryDefs { get; set; }
        [ParameterDescription("Every joy kind def the game holds.")]
        public List<JoyKindDefDto> JoyKindDefs { get; set; }
        [ParameterDescription("Every ideoligion meme def.")]
        public List<MemeDefDto> MemeDefs { get; set; }
        [ParameterDescription("Every precept def the game holds.")]
        public List<PreceptDefDto> PreceptDefs { get; set; }
        [ParameterDescription("Every ability def the game holds.")]
        public List<AbilityDefDto> AbilityDefs { get; set; }
        [ParameterDescription("Every gene def the game holds.")]
        public List<GeneDefDto> GeneDefs { get; set; }
        [ParameterDescription("Every weather def the game holds.")]
        public List<WeatherDefDto> WeatherDefs { get; set; }
        [ParameterDescription("Every room role def the game holds.")]
        public List<RoomRoleDefDto> RoomRoleDefs { get; set; }
        [ParameterDescription("Every room stat def the game holds.")]
        public List<RoomStatDefDto> RoomStatDefs { get; set; }
        [ParameterDescription("Every mental state def the game holds.")]
        public List<MentalStateDefDto> MentalStateDefs { get; set; }
        [ParameterDescription("Every drug policy def the game holds.")]
        public List<DrugPolicyDefDto> DrugPolicyDefs { get; set; }
        [ParameterDescription("Every plant def the game holds.")]
        public List<PlantDefDto> PlantDefs { get; set; }
        [ParameterDescription("Every animal race def the game holds.")]
        public List<AnimalDefDto> AnimalDefs { get; set; }
        [ParameterDescription("Every storyteller def the game holds.")]
        public List<StorytellerDefDto> StorytellerDefs { get; set; }
        [ParameterDescription("Every difficulty def the game holds.")]
        public List<DifficultyDefDto> DifficultyDefs { get; set; }
        [ParameterDescription("Every job def the game holds.")]
        public List<JobDefDto> JobDefs { get; set; }
    }

    public class JobDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("Whether a player order may break the job.")]
        public bool PlayerInterruptible { get; set; }
        [ParameterDescription("Whether the pawn always shows its weapon during it.")]
        public bool AlwaysShowWeapon { get; set; }
        [ParameterDescription("Whether the pawn never shows its weapon during it.")]
        public bool NeverShowWeapon { get; set; }
        [ParameterDescription("Whether the job may be suspended and resumed.")]
        public bool Suspendable { get; set; }
    }

    public class ThingCostDto
    {
        [ParameterDescription("The def name of the thing the cost is paid in.")]
        public string ThingDef { get; set; }
        public int Count { get; set; }
    }

    public class PawnKindDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The race's def name.")]
        public string Race { get; set; }
        [ParameterDescription("The combat power the game rates it at.")]
        public float CombatPower { get; set; }
        [ParameterDescription("The weapon tags it may carry.")]
        public List<string> WeaponTags { get; set; }
        [ParameterDescription("The apparel tags it may wear.")]
        public List<string> ApparelTags { get; set; }
        [ParameterDescription("The hit point scale the kind starts with.")]
        public float BaseHealthScale { get; set; }
        [ParameterDescription("The body size the kind starts with.")]
        public float BaseBodySize { get; set; }
        [ParameterDescription("The faction tags that may field it.")]
        public List<string> FactionTags { get; set; }
    }

    public class TraitDegreeDataDto
    {
        [ParameterDescription("The degree's number, 0 for the base level.")]
        public int Degree { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The stat offsets the trait degree adds.")]
        public Dictionary<string, float> StatOffsets { get; set; }
        [ParameterDescription("How much more likely the degree makes social fights.")]
        public float SocialFightChanceFactor { get; set; }
        [ParameterDescription("How many days between the mental states it forces.")]
        public float ForcedMentalStateMtbDays { get; set; }
        [ParameterDescription("How much faster the degree makes the pawn hungry.")]
        public float HungerRateFactor { get; set; }
    }

    public class ResearchProjectDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("How many research points the project costs.")]
        public float Cost { get; set; }
        [ParameterDescription("The projects that must finish first.")]
        public List<string> Prerequisites { get; set; }
        [ParameterDescription("The bench a researcher needs to work on it.")]
        public string RequiredResearchBuilding { get; set; }
        [ParameterDescription("The tech level the project belongs to.")]
        public string TechLevel { get; set; }
        [ParameterDescription("The research tab the project sits on.")]
        public string Tab { get; set; }
        [ParameterDescription("Whether the game hides the project.")]
        public bool Hidden { get; set; }
        [ParameterDescription("The defs the project unlocks when it finishes.")]
        public List<string> UnlockedDefs { get; set; }
    }

    public class HediffDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The C# class the hediff runs.")]
        public string HediffClass { get; set; }
        [ParameterDescription("Whether the hediff is an addiction.")]
        public bool IsAddiction { get; set; }
        [ParameterDescription("Whether it makes the pawn feel sick.")]
        public bool MakesSickThought { get; set; }
        [ParameterDescription("How much the severity moves per day.")]
        public float SeverityPerDay { get; set; }
        [ParameterDescription("The highest severity the hediff reaches.")]
        public float MaxSeverity { get; set; }
        [ParameterDescription("Whether a doctor can tend it.")]
        public bool Tendable { get; set; }
        [ParameterDescription("Whether the game counts it as a bad condition.")]
        public bool IsBad { get; set; }
        [ParameterDescription("The severity stages the hediff passes.")]
        public List<HediffStageDto> Stages { get; set; }
        [ParameterDescription("The comps the hediff carries, such as its immunity driver.")]
        public string CompProps { get; set; }
    }

    public class HediffStageDto
    {
        [ParameterDescription("The severity the stage starts at.")]
        public float MinSeverity { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The hediffs the pawn becomes immune to.")]
        public List<string> BecomeImmuneTo { get; set; }
        [ParameterDescription("How many days until the stage kills, on average.")]
        public float DeathMtbDays { get; set; }
        [ParameterDescription("How much pain the stage causes, as a multiplier.")]
        public float PainFactor { get; set; }
        [ParameterDescription("How many days between the memories the stage erases.")]
        public float ForgetMemoryThoughtMtbDays { get; set; }
        [ParameterDescription("How many days between the vomiting the stage causes.")]
        public float VomitMtbDays { get; set; }
        [ParameterDescription("How many days between the mental breaks it forces.")]
        public float MentalBreakMtbDays { get; set; }
    }

    public class SkillDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("Whether the skill can carry a passion.")]
        public bool PassionSusceptible { get; set; }
        [ParameterDescription("The work tags the skill switches off while it is missing.")]
        public string DisablingWorkTags { get; set; }
    }

    public class WorkTypeDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The work tags the work type covers.")]
        public string WorkTags { get; set; }
        [ParameterDescription("The skills that speed the work up.")]
        public List<string> RelevantSkills { get; set; }
        [ParameterDescription("The priority the game gives the work type by default.")]
        public int NaturalPriority { get; set; }
        [ParameterDescription("Whether the work type shows only when work exists.")]
        public bool VisibleOnlyWithWork { get; set; }
    }

    public class NeedDefDto
    {
        public string DefName { get; set; }
        [ParameterDescription("The C# class the need runs.")]
        public string NeedClassName { get; set; }
        [ParameterDescription("Whether the need stops moving in a mental state.")]
        public bool FreezeInMentalState { get; set; }
        [ParameterDescription("Whether the need stops moving while the pawn sleeps.")]
        public bool FreezeWhileSleeping { get; set; }
        [ParameterDescription("How fast the need falls per hour when the pawn seeks it.")]
        public float SeekerFallPerHour { get; set; }
        [ParameterDescription("How fast it rises per hour while the pawn seeks it.")]
        public float SeekerRisePerHour { get; set; }
        [ParameterDescription("How much the need falls per day.")]
        public float FallPerDay { get; set; }
        [ParameterDescription("Whether the need shows its value in ticks.")]
        public bool ShowUnitTicks { get; set; }
        [ParameterDescription("Whether the need shows a bar.")]
        public bool ScaleBar { get; set; }
        [ParameterDescription("Whether the need shows for caravan members.")]
        public bool ShowForCaravanMembers { get; set; }
        [ParameterDescription("The tag the tutor uses to highlight it.")]
        public string TutorHighlightTag { get; set; }
        [ParameterDescription("The order the need takes in the list.")]
        public int ListPriority { get; set; }
        [ParameterDescription("Whether the need is a major one.")]
        public bool Major { get; set; }
        [ParameterDescription("The level the need starts at.")]
        public float BaseLevel { get; set; }
        [ParameterDescription("Whether the need appears in the needs list.")]
        public bool ShowOnNeedList { get; set; }
        [ParameterDescription("The life stages the need applies to.")]
        public string DevelopmentalStageFilter { get; set; }
        [ParameterDescription("The precepts that remove the need.")]
        public List<string> NullifyingPrecepts { get; set; }
        [ParameterDescription("The hediffs that must be present for the need.")]
        public List<string> HediffRequiredAny { get; set; }
        [ParameterDescription("The titles that must be held for the need.")]
        public List<string> TitleRequiredAny { get; set; }
        [ParameterDescription("Whether slaves never carry the need.")]
        public bool NeverOnSlave { get; set; }
        [ParameterDescription("Whether prisoners never carry it.")]
        public bool NeverOnPrisoner { get; set; }
        [ParameterDescription("Whether it appears only when a trait causes it.")]
        public bool OnlyIfCausedByTrait { get; set; }
        [ParameterDescription("Whether it appears only when a gene causes it.")]
        public bool OnlyIfCausedByGene { get; set; }
        [ParameterDescription("Whether it appears only when a hediff causes it.")]
        public bool OnlyIfCausedByHediff { get; set; }
        [ParameterDescription("Whether only slaves carry it.")]
        public bool SlavesOnly { get; set; }
        [ParameterDescription("Whether only colonists carry it.")]
        public bool ColonistsOnly { get; set; }
        [ParameterDescription("Whether only the player's mechs carry it.")]
        public bool PlayerMechsOnly { get; set; }
        [ParameterDescription("Whether only colonists and prisoners carry it.")]
        public bool ColonistAndPrisonersOnly { get; set; }
        [ParameterDescription("The lowest intelligence that carries the need.")]
        public string MinIntelligence { get; set; }
        [ParameterDescription("The comps a pawn needs to carry it.")]
        public List<string> RequiredComps { get; set; }

        public static NeedDefDto NeedDefToDto(NeedDef needDef)
        {
            return new NeedDefDto
            {
                DefName = needDef.defName,
                NeedClassName = needDef.needClass?.FullName,
                FreezeInMentalState = needDef.freezeInMentalState,
                FreezeWhileSleeping = needDef.freezeWhileSleeping,
                SeekerFallPerHour = needDef.seekerFallPerHour,
                SeekerRisePerHour = needDef.seekerRisePerHour,
                FallPerDay = needDef.fallPerDay,
                ShowUnitTicks = needDef.showUnitTicks,
                ScaleBar = needDef.scaleBar,
                ShowForCaravanMembers = needDef.showForCaravanMembers,
                TutorHighlightTag = needDef.tutorHighlightTag,
                ListPriority = needDef.listPriority,
                Major = needDef.major,
                BaseLevel = needDef.baseLevel,
                ShowOnNeedList = needDef.showOnNeedList,
                DevelopmentalStageFilter = needDef.developmentalStageFilter.ToString(),
                NullifyingPrecepts = needDef.nullifyingPrecepts?.Select(p => p.defName).ToList(),
                HediffRequiredAny = needDef.hediffRequiredAny?.Select(h => h.defName).ToList(),
                TitleRequiredAny = needDef.titleRequiredAny?.Select(t => t.defName).ToList(),
                NeverOnSlave = needDef.neverOnSlave,
                NeverOnPrisoner = needDef.neverOnPrisoner,
                OnlyIfCausedByTrait = needDef.onlyIfCausedByTrait,
                OnlyIfCausedByGene = needDef.onlyIfCausedByGene,
                OnlyIfCausedByHediff = needDef.onlyIfCausedByHediff,
                SlavesOnly = needDef.slavesOnly,
                ColonistsOnly = needDef.colonistsOnly,
                PlayerMechsOnly = needDef.playerMechsOnly,
                ColonistAndPrisonersOnly = needDef.colonistAndPrisonersOnly,
                MinIntelligence = needDef.minIntelligence.ToString(),
                RequiredComps = needDef.requiredComps?.Select(c => c.GetType().Name).ToList(),
            };
        }
    }

    public class ThoughtDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("How much mood the thought moves.")]
        public float MoodOffset { get; set; }
        [ParameterDescription("How many days the thought lasts.")]
        public float DurationDays { get; set; }
        [ParameterDescription("How many times the thought stacks.")]
        public int StackLimit { get; set; }
        [ParameterDescription("The stages the thought passes as it settles.")]
        public List<ThoughtStageDto> Stages { get; set; }
    }

    public class ThoughtStageDto
    {
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("How much mood the stage moves.")]
        public float BaseMoodEffect { get; set; }
        [ParameterDescription("How much opinion the stage moves.")]
        public float BaseOpinionOffset { get; set; }
    }

    public class StatDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The category the stat is listed under.")]
        public string Category { get; set; }
        [ParameterDescription("The lowest value the stat can take.")]
        public float MinValue { get; set; }
        [ParameterDescription("The highest value it can take.")]
        public float MaxValue { get; set; }
        [ParameterDescription("The value the stat starts at.")]
        public float DefaultBaseValue { get; set; }
        [ParameterDescription("Whether the stat shows for pawns.")]
        public bool ShowOnPawns { get; set; }
        [ParameterDescription("Whether it shows for humanlikes.")]
        public bool ShowOnHumanlikes { get; set; }
        [ParameterDescription("Whether it shows for animals.")]
        public bool ShowOnAnimals { get; set; }
        [ParameterDescription("Whether it shows for mechanoids.")]
        public bool ShowOnMechanoids { get; set; }
        [ParameterDescription("Whether it shows on things other than work tables.")]
        public bool ShowOnNonWorkTables { get; set; }
        [ParameterDescription("Whether a base value of zero is allowed.")]
        public bool AlwaysAllowBaseZero { get; set; }
    }

    public class WorldObjectDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("Whether the object can belong to a faction.")]
        public bool CanHaveFaction { get; set; }
        [ParameterDescription("Whether the player can select it.")]
        public bool Selectable { get; set; }
        [ParameterDescription("Whether it refuses a multi-selection.")]
        public bool NeverMultiSelect { get; set; }
        [ParameterDescription("Whether its icon expands.")]
        public bool ExpandingIcon { get; set; }
        [ParameterDescription("Whether the game may place it at random.")]
        public bool CanBeRandomlyPlaced { get; set; }
    }

    public class BiomeDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("How many animals the biome holds per cell.")]
        public float AnimalDensity { get; set; }
        [ParameterDescription("How many plants the biome holds per cell.")]
        public float PlantDensity { get; set; }
        [ParameterDescription("How many days between diseases, on average.")]
        public float DiseaseMtbDays { get; set; }
        [ParameterDescription("The food foragers find in the biome.")]
        public string ForagedFood { get; set; }
        [ParameterDescription("How much slower travel is through it.")]
        public float MovementDifficulty { get; set; }
        [ParameterDescription("Every plant the biome grows wild.")]
        public List<string> AllWildPlants { get; set; }
    }

    public class TerrainDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How well plants grow on it, 0 to 1.")]
        public float Fertility { get; set; }
        [ParameterDescription("How much movement the terrain costs.")]
        public int PathCost { get; set; }
        [ParameterDescription("How much faster things on it wear out.")]
        public float ExtraDeteriorationFactor { get; set; }
        [ParameterDescription("Whether another terrain may lie on it.")]
        public bool Layerable { get; set; }
        [ParameterDescription("What the terrain lets pawns do, such as walk or build.")]
        public List<string> Affordances { get; set; }
        [ParameterDescription("The projects that must finish before it can be built.")]
        public List<string> ResearchPrerequisites { get; set; }
    }

    public class RecipeDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The skill that speeds the recipe's work.")]
        public string WorkSkill { get; set; }
        [ParameterDescription("How much skill the work teaches.")]
        public float WorkSkillLearnFactor { get; set; }
        [ParameterDescription("How much work the recipe takes.")]
        public float WorkAmount { get; set; }
        [ParameterDescription("The stat that speeds the work.")]
        public string WorkSpeedStat { get; set; }
        [ParameterDescription("The stat that decides the yield.")]
        public string EfficiencyStat { get; set; }
        [ParameterDescription("The things one run of the recipe produces.")]
        public List<string> Products { get; set; }
        [ParameterDescription("The skill levels the crafter needs.")]
        public List<SkillRequirementDto> SkillRequirements { get; set; }
    }

    public class SkillRequirementDto
    {
        [ParameterDescription("The skill the requirement asks for.")]
        public string Skill { get; set; }
        [ParameterDescription("The lowest level that satisfies it.")]
        public int MinLevel { get; set; }
    }

    public class IngredientDto
    {
        [ParameterDescription("The filter that decides which items count.")]
        public string Filter { get; set; }
        public int Count { get; set; }
        [ParameterDescription("Whether the ingredient is one fixed def.")]
        public bool IsFixedIngredient { get; set; }
        [ParameterDescription("The one def the ingredient must be.")]
        public string FixedIngredient { get; set; }
    }

    public class BodyDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The body's core part, usually the torso.")]
        public string CorePart { get; set; }
        [ParameterDescription("Every part the body holds.")]
        public List<string> Parts { get; set; }
    }

    public class BodyPartDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How fast the part bleeds when hurt.")]
        public float BleedRate { get; set; }
        [ParameterDescription("How much damage the part takes.")]
        public float HitPoints { get; set; }
        [ParameterDescription("How likely a wound here leaves a scar.")]
        public float PermanentInjuryChanceFactor { get; set; }
    }

    public class SoundDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("How many copies of the sound may play at once.")]
        public int MaxSimultaneous { get; set; }
    }

    public class DesignationCategoryDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
    }

    public class JoyKindDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
    }

    public class MemeDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
    }

    public class PreceptDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("How strongly the precept shapes the ideoligion.")]
        public string Impact { get; set; }
        // Issue and TipLabel are what the ideoligion window shows and what a
        // founding accepts as a precept name (ticket #3927).
        [ParameterDescription("Issue and TipLabel are what the ideoligion window shows and what a founding accepts as a precept name (ticket #3927).")]
        public string Issue { get; set; }
        [ParameterDescription("The short label the tooltip shows.")]
        public string TipLabel { get; set; }
        // RequiredMemes names the memes a precept demands; Effects what it
        // buys and costs, restated from the def.
        [ParameterDescription("RequiredMemes names the memes a precept demands; Effects what it buys and costs, restated from the def.")]
        public List<string> RequiredMemes { get; set; }
        [ParameterDescription("What the precept changes in play.")]
        public List<string> Effects { get; set; }
        [ParameterDescription("The ritual the precept attaches to, when it names one.")]
        public string Ritual { get; set; }
    }

    public class AbilityDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The verb the ability fires with.")]
        public string VerbProperties { get; set; }
    }

    public class GeneDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("How much complexity the gene adds to the genome.")]
        public int BiostatCpx { get; set; }
        [ParameterDescription("How much metabolism the gene adds to the genome.")]
        public int BiostatMet { get; set; }
        [ParameterDescription("The category the gene shows under.")]
        public string DisplayCategory { get; set; }
        [ParameterDescription("The age from which the gene works.")]
        public float MinAgeActive { get; set; }
    }

    public class WeatherDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The lowest temperature the weather occurs at.")]
        public float MinTemperature { get; set; }
        [ParameterDescription("The highest temperature it occurs at.")]
        public float MaxTemperature { get; set; }
        [ParameterDescription("How much wind it brings.")]
        public float WindSpeedFactor { get; set; }
        [ParameterDescription("How much it slows movement.")]
        public float MoveSpeedMultiplier { get; set; }
        [ParameterDescription("How much it spoils shooting.")]
        public float AccuracyMultiplier { get; set; }
    }

    public class RoomRoleDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
    }

    public class RoomStatDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The score bands the room stat reports.")]
        public List<RoomStatScoreStageDto> ScoreStages { get; set; }
    }

    public class RoomStatScoreStageDto
    {
        public string Label { get; set; }
        [ParameterDescription("The score the band starts at.")]
        public float MinScore { get; set; }
    }

    public class MentalStateDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("How many days until the state ends, on average.")]
        public float RecoveryMtbDays { get; set; }
        [ParameterDescription("Whether the state attacks others.")]
        public bool IsAggro { get; set; }
    }

    public class DrugPolicyDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("One entry per drug the policy rules on.")]
        public List<DrugPolicyEntryDto> Entries { get; set; }
    }

    public class DrugPolicyEntryDto
    {
        [ParameterDescription("The drug the entry rules on.")]
        public string Drug { get; set; }
        [ParameterDescription("Whether the policy allows it for addiction.")]
        public bool AllowedForAddiction { get; set; }
        [ParameterDescription("Whether the policy allows it for joy.")]
        public bool AllowedForJoy { get; set; }
        [ParameterDescription("Whether the policy allows a scheduled dose.")]
        public bool AllowScheduled { get; set; }
        [ParameterDescription("How many days between scheduled doses.")]
        public float DaysFrequency { get; set; }
        [ParameterDescription("Take it only when mood falls below this.")]
        public float OnlyIfMoodBelow { get; set; }
        [ParameterDescription("Take it only when joy falls below this.")]
        public float OnlyIfJoyBelow { get; set; }
    }

    public class PlantDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The lowest fertility the plant grows on.")]
        public float FertilityMin { get; set; }
        [ParameterDescription("How much the ground's fertility helps it.")]
        public float FertilitySensitivity { get; set; }
        [ParameterDescription("How many days the plant needs to ripen.")]
        public float GrowDays { get; set; }
        [ParameterDescription("The thing the plant yields when harvested.")]
        public string HarvestedThingDef { get; set; }
    }

    public class AnimalDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The body size the race starts with.")]
        public float BaseBodySize { get; set; }
        [ParameterDescription("The hit point scale the race starts with.")]
        public float BaseHealthScale { get; set; }
        [ParameterDescription("What the race eats: meat, plants or both.")]
        public string FoodType { get; set; }
        [ParameterDescription("Whether the race hunts other pawns.")]
        public bool Predator { get; set; }
        [ParameterDescription("Whether the race carries caravan loads.")]
        public bool PackAnimal { get; set; }
        [ParameterDescription("How likely the race is kept as a pet.")]
        public float Petness { get; set; }
        [ParameterDescription("The life stages the race passes, from baby to adult.")]
        public List<LifeStageAgeDto> LifeStages { get; set; }
    }

    public class DifficultyDefDto
    {
        public string DefName { get; set; }
    }

    public class StorytellerDefDto
    {
        public string DefName { get; set; }
    }

    public class LifeStageAgeDto
    {
        [ParameterDescription("The stage's name, e.g. adult.")]
        public string LifeStage { get; set; }
        [ParameterDescription("The age the stage starts at, in years.")]
        public float MinAge { get; set; }
    }
}
