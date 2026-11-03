using System.Collections.Generic;
using Newtonsoft.Json;
using RIMAPI.Core;

namespace RIMAPI.Models
{
    public class BodyPartsDto
    {
        [ParameterDescription("The path of the pawn's body texture.")]
        public string BodyImage { get; set; }
        [ParameterDescription("The body's tint colour, as a hex string.")]
        public string BodyColor { get; set; }
        [ParameterDescription("The path of the pawn's head texture.")]
        public string HeadImage { get; set; }
        [ParameterDescription("The head's tint colour, as a hex string.")]
        public string HeadColor { get; set; }
    }

    public class WorkInfoDto
    {
        [ParameterDescription("One entry per skill the pawn has.")]
        public List<SkillDto> Skills { get; set; }
        [ParameterDescription("The job the pawn is doing now, as the game names it.")]
        public string CurrentJob { get; set; }
        [ParameterDescription("One entry per trait the pawn has.")]
        public List<TraitDto> Traits { get; set; }
        [ParameterDescription("The pawn's priority for each work type.")]
        public List<WorkPriorityDto> WorkPriorities { get; set; }

        // IncapableOf names the work tags the game disables for this pawn
        // (fox, #3360): a pawn whose backstory disables violence carries no
        // trait that says so, so his trait list reads like any other one's and
        // a caller drafts him for a raid that never fires. The labels are the
        // ones the game's own incapability list uses.
        [ParameterDescription("The work tags the game disables for this pawn.")]
        public List<string> IncapableOf { get; set; }

        // CanFight is false when the game disables the Violent work tag for
        // him: no draft and no attack order can make such a pawn shoot.
        [ParameterDescription("Whether the pawn can fight at all.")]
        public bool CanFight { get; set; }
    }

    // The thoughts behind a colonist's mood number (fox, #3129). Need_Mood only
    // reports the level, so a caller that wants to fix a low mood cannot tell a
    // bad bedroom from a missing joy type or a trait; this carries the active
    // thoughts with their offsets, worst first.
    public class MoodInfoDto
    {
        [ParameterDescription("The pawn's mood, 0 to 1.")]
        public float Mood { get; set; }
        [ParameterDescription("How far the thoughts move the mood.")]
        public float TotalOffset { get; set; }
        [ParameterDescription("The thoughts that move the pawn's mood.")]
        public List<MoodThoughtDto> Thoughts { get; set; }
    }

    public class MoodThoughtDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The thought's current stage, as the game names it.")]
        public string StageLabel { get; set; }
        [ParameterDescription("How much this thought moves the mood.")]
        public float MoodOffset { get; set; }
        // Memory thoughts fade and carry an age; situational thoughts do not,
        // and AgeDays stays 0 for them.
        [ParameterDescription("How many days the thought has lasted.")]
        public int AgeDays { get; set; }
        [ParameterDescription("Whether the thought never expires.")]
        public bool Permanent { get; set; }
        // "memory" or "situation": a memory disappears when it is forgotten, a
        // situational thought disappears when the situation changes.
        [ParameterDescription("What kind of thought it is: memory, situation or trait.")]
        public string Kind { get; set; }
        // The other pawn of a social thought, empty otherwise.
        [ParameterDescription("The pawn the thought is about, when it names one.")]
        public string OtherPawn { get; set; }
    }

    public class TraitDto
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The work tags this trait disables.")]
        public int DisabledWorkTags { get; set; }
        [ParameterDescription("Whether the trait is currently suppressed.")]
        public bool Suppressed { get; set; }
    }

    public class PoliciesInfoDto
    {
        [ParameterDescription("The numeric id of the pawn's food policy.")]
        public int FoodPolicyId { get; set; }
        [ParameterDescription("How the pawn responds to hostiles: flee, attack or ignore.")]
        public int HostilityResponse { get; set; }
    }

    public class SocialInfoDto
    {
        [ParameterDescription("The numeric id of the pawn's social record.")]
        public string Id { get; set; }
        public string Name { get; set; }
        [ParameterDescription("The pawn's direct relations.")]
        public List<RelationDto> DirectRelations { get; set; }
        [ParameterDescription("How many children the pawn has.")]
        public int ChildrenCount { get; set; }
    }

    public class RelationDto
    {
        [ParameterDescription("The relation's def name, e.g. \"Spouse\".")]
        public string relationDefName { get; set; }
        [ParameterDescription("The numeric id of the related pawn.")]
        public string otherPawnId { get; set; }
        [ParameterDescription("The name of the related pawn.")]
        public string otherPawnName { get; set; }
    }

    public class OpinionAboutPawnDto
    {
        [ParameterDescription("How the pawn thinks of the other, -100 to 100.")]
        public int Opinion { get; set; }
        [ParameterDescription("How the other thinks of the pawn, -100 to 100.")]
        public int OpinionAboutMe { get; set; }
    }

    public class MedicalInfoDto
    {
        [ParameterDescription("Whether the pawn is dead.")]
        public bool IsDead { get; set; }
        [ParameterDescription("Whether the pawn is downed.")]
        public bool IsDowned { get; set; } // True if crawling or incapacitated

        // DownedReason says why a downed pawn is down (ticket #3288): the
        // condition behind the state, e.g. "consciousness 0% from Malnutrition;
        // malnutrition severity 0.62". Empty for a pawn that is not downed. A
        // pawn can lie in bed at 100% summary health with nothing to tend, and
        // without this the state is a fact with no cause.
        [ParameterDescription("Why the pawn is downed.")]
        public string DownedReason { get; set; }

        // MentalState names the mental state the pawn is in (fox, #3513):
        // "tantrum", "berserk", "daze". Such a pawn cannot take a tend job, and
        // the detailed read carried no flag for it, so the fox's doctor search
        // offered him as a carer. Empty when he is in none; a state without a
        // label reads by its defName.
        [ParameterDescription("The mental state the pawn is in, if any.")]
        public string MentalState { get; set; }

        // MentalStateAggro says the game itself calls the state aggressive
        // (Pawn.InAggroMentalState): a berserk or murderous-rage pawn attacks
        // the others, a pawn in a sad wander or a corpse obsession only stops
        // working. The fox's wait stops for the first kind and steps game time
        // through the second (fox, #3761), and it needs the game's verdict
        // rather than a list of labels, which the game translates. Null when
        // the pawn is in no state, so an older fox reading this build can tell
        // "no state" from "not aggressive".
        [ParameterDescription("Whether the mental state makes the pawn hostile.")]
        public bool? MentalStateAggro { get; set; }

        // Capacities (0.0 to 1.0+)
        [ParameterDescription("The pawn's consciousness, 0 to 1.")]
        public float Consciousness { get; set; }
        [ParameterDescription("The pawn's moving capacity, 0 to 1.")]
        public float Moving { get; set; }

        [ParameterDescription("The pawn's overall health, 0 to 1.")]
        public float Health { get; set; }
        [ParameterDescription("The pawn's injuries, diseases and other conditions.")]
        public List<HediffDto> Hediffs { get; set; }
        [ParameterDescription("The numeric id of the pawn's medical care policy.")]
        public int MedicalPolicyId { get; set; }
        [ParameterDescription("Whether the pawn may tend itself.")]
        public bool IsSelfTendAllowed { get; set; }

        // InBed is the game's own Pawn.InBed (ticket #3445). A downed pawn on
        // the open ground cannot be fed: the FeedPatient job needs a bed, and
        // without this flag the tool cannot tell the two states apart.
        [ParameterDescription("Whether the pawn is in bed.")]
        public bool InBed { get; set; }
    }

    public class HediffDto
    {
        // Basic identification
        [ParameterDescription("The hediff's load id, which survives a save.")]
        public int LoadId { get; set; }
        public string DefName { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The label with the game's capitalisation.")]
        public string LabelCap { get; set; }
        [ParameterDescription("The label as the game shows it in brackets.")]
        public string LabelInBrackets { get; set; }

        // Severity and stage
        [ParameterDescription("How far the condition has progressed.")]
        public float Severity { get; set; }
        [ParameterDescription("The severity as the game names it.")]
        public string SeverityLabel { get; set; }
        [ParameterDescription("The index of the condition's current stage.")]
        public int CurStageIndex { get; set; }
        [ParameterDescription("The name of the condition's current stage.")]
        public string CurStageLabel { get; set; }

        // Body part information
        [ParameterDescription("The body part the condition sits on.")]
        public string PartLabel { get; set; }
        [ParameterDescription("The def name of that body part.")]
        public string PartDefName { get; set; }

        // Age and timing
        [ParameterDescription("How many ticks the condition has lasted.")]
        public int AgeTicks { get; set; }
        [ParameterDescription("The same age as a readable string.")]
        public string AgeString { get; set; }

        // Status flags
        [ParameterDescription("Whether the game shows the condition.")]
        public bool Visible { get; set; }
        [ParameterDescription("Whether the condition never heals.")]
        public bool IsPermanent { get; set; }
        [ParameterDescription("Whether the condition has been tended.")]
        public bool IsTended { get; set; }
        [ParameterDescription("Whether it can be tended right now.")]
        public bool TendableNow { get; set; }
        [ParameterDescription("Whether the condition bleeds at all.")]
        public bool Bleeding { get; set; }
        [ParameterDescription("How much blood it loses per day.")]
        public float BleedRate { get; set; }
        [ParameterDescription("Whether the condition can kill.")]
        public bool IsLethal { get; set; }
        [ParameterDescription("Whether it threatens the pawn's life now.")]
        public bool IsCurrentlyLifeThreatening { get; set; }
        [ParameterDescription("Whether the condition may kill at all.")]
        public bool CanEverKill { get; set; }

        // Infection facts from the def itself (fox, #3449). The digest kept a
        // hand-copied table of both, which goes stale the moment a mod or a
        // game update changes a rate or adds an infection: the game has the
        // def at hand here, so it says what the def says.
        //
        // IsInfection is HediffDef.isInfection (flu and plague inherit it
        // through InfectionBase, gut worms carry it too).
        [ParameterDescription("Whether it is an infection.")]
        public bool IsInfection { get; set; }

        // SeverityPerDay is the severity the def gains per day while immunity
        // has not won: severityPerDayNotImmune of an immunizable infection, or
        // a plain SeverityPerDay comp's rate. 0 means the def states no rate,
        // which the caller reads as "no projection" rather than a guess.
        [ParameterDescription("How much severity changes per day.")]
        public float SeverityPerDay { get; set; }

        // ImmunityPerDay is the immunity the def's Immunizable comp states per
        // day while the disease is present (fox, #3682): the second clock, so
        // the digest compares the two clocks instead of a share of one figure.
        // 0 means the def states no rate.
        [ParameterDescription("How much immunity grows per day.")]
        public float ImmunityPerDay { get; set; }

        // Immunity is the pawn's immunity progress against this very hediff,
        // 0 to 1 (fox, #3618). The game keeps one ImmunityRecord per disease in
        // the pawn's ImmunityHandler, so this figure belongs on the infection
        // and not on the pawn. The digest races it against the severity; it
        // used to look for a hediff named "Immunity", which the game has no def
        // for, so its race line and its death date never appeared. 0 means no
        // record yet, which is the truth on the day an infection starts.
        [ParameterDescription("The immunity built up, 0 to 1.")]
        public float Immunity { get; set; }

        // Source information
        [ParameterDescription("The def name of what caused the condition.")]
        public string SourceDefName { get; set; }
        [ParameterDescription("The label of what caused the condition.")]
        public string SourceLabel { get; set; }
        [ParameterDescription("The body part group the source names.")]
        public string SourceBodyPartGroupDefName { get; set; }
        [ParameterDescription("The hediff def the source names.")]
        public string SourceHediffDefName { get; set; }

        // Combat log
        [ParameterDescription("The line the combat log shows for it.")]
        public string CombatLogText { get; set; }

        // Additional properties
        [ParameterDescription("The extra text in the game's tooltip.")]
        public string TipStringExtra { get; set; }
        [ParameterDescription("How much pain the condition causes.")]
        public float PainFactor { get; set; }
        [ParameterDescription("The flat pain the condition adds.")]
        public float PainOffset { get; set; }
    }

    public class SkillDto
    {
        public string Name { get; set; }
        [ParameterDescription("The pawn's level in the skill, 0 to 20.")]
        public int Level { get; set; }
        public string Description { get; set; }
        [ParameterDescription("The lowest level the skill can reach.")]
        public int MinLevel { get; set; }
        [ParameterDescription("The highest level the skill can reach.")]
        public int MaxLevel { get; set; }
        [ParameterDescription("The word the game uses for this level.")]
        public string LevelDescriptor { get; set; }
        [ParameterDescription("Whether the skill is permanently disabled.")]
        public bool PermanentlyDisabled { get; set; }
        [ParameterDescription("Whether the pawn cannot use the skill at all.")]
        public bool TotallyDisabled { get; set; }
        [ParameterDescription("The experience earned over the pawn's life.")]
        public float XpTotalEarned { get; set; }
        [ParameterDescription("Progress toward the next level, in percent.")]
        public float XpProgressPercent { get; set; }
        [ParameterDescription("The experience the next level needs.")]
        public float XpRequiredForLevelUp { get; set; }
        [ParameterDescription("The experience earned since the last level.")]
        public float XpSinceLastLevel { get; set; }
        [ParameterDescription("The pawn's aptitude for the skill.")]
        public int Aptitude { get; set; }
        [ParameterDescription("The pawn's passion: none, minor or major.")]
        public int Passion { get; set; }
        [ParameterDescription("The work tags the pawn cannot do through this skill.")]
        public int DisabledWorkTags { get; set; }
    }

    public class TraitDefDto
    {
        public string DefName { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("One entry per degree the trait has.")]
        public List<TraitDegreeDto> DegreeDatas { get; set; }
        [ParameterDescription("Traits the game will not combine with this one.")]
        public List<string> ConflictingTraits { get; set; }
        [ParameterDescription("Work types this trait disables.")]
        public List<string> DisabledWorkTypes { get; set; }
        [ParameterDescription("Work tags this trait disables.")]
        public string DisabledWorkTags { get; set; }
    }

    public class TraitDegreeDto
    {
        public string Label { get; set; }
        public string Description { get; set; }
        [ParameterDescription("Which degree of the trait this is.")]
        public int Degree { get; set; }
        [ParameterDescription("The skill bonuses the degree gives.")]
        public Dictionary<string, int> SkillGains { get; set; }
        [ParameterDescription("How much the degree shifts each stat.")]
        public List<StatModifierDto> StatOffsets { get; set; }
        [ParameterDescription("The stat multipliers the degree gives.")]
        public List<StatModifierDto> StatFactors { get; set; }
    }

    public class StatModifierDto
    {
        [ParameterDescription("The stat's def name.")]
        public string StatDefName { get; set; }
        [ParameterDescription("How much this modifier adds to the stat.")]
        public float Value { get; set; }
    }

    public class WorkPriorityDto
    {
        [ParameterDescription("The work type's def name.")]
        public string WorkType { get; set; }
        [ParameterDescription("The pawn's priority for it, 1 highest to 4 lowest.")]
        public int Priority { get; set; }
        [ParameterDescription("Whether the pawn cannot do this work.")]
        public bool IsTotallyDisabled { get; set; }
    }

    // One work type's level on one pawn (fox, #3085). The game keeps two
    // numbers and they are not the same: Stored is what the pawn is set to,
    // Priority is what the game acts on. While manual priorities are off the
    // game treats every active type as 3, whatever the pawn stores, so a read
    // that only reported GetPriority could not tell a hand-set level from a
    // default one.
    public class WorkPriorityEntryDto
    {
        [ParameterDescription("The work type's def name.")]
        public string WorkType { get; set; }
        [ParameterDescription("The pawn's priority for it, 1 highest to 4 lowest.")]
        public int Priority { get; set; }
        [ParameterDescription("The priority stored in the save.")]
        public int Stored { get; set; }
        [ParameterDescription("Whether the work type is disabled.")]
        public bool Disabled { get; set; }
    }

    public class ColonistWorkPrioritiesDto
    {
        public int PawnId { get; set; }
        [ParameterDescription("The pawn's name, as the pawn list shows it.")]
        public string PawnName { get; set; }
        [ParameterDescription("Whether the colony uses manual priorities.")]
        public bool ManualPriorities { get; set; }
        [ParameterDescription("Whether the priority table has been set up.")]
        public bool Initialized { get; set; }
        // Readiness separates the two ways Initialized can be false (fox
        // #3604): "needs_initialization" is a settings component whose levels
        // the game creates on the first write, "no_settings" is a pawn without
        // the component at all, which the game refuses. "ready" is Initialized.
        [ParameterDescription("How ready the pawn is for the work, 0 to 1.")]
        public string Readiness { get; set; }
        [ParameterDescription("One entry per work type.")]
        public List<WorkPriorityEntryDto> Work { get; set; } = new List<WorkPriorityEntryDto>();
    }

    // Every colonist's work settings in one answer (fox ticket #4158), so the
    // build path does not pay one read per pawn to say who would build a plan.
    // Each entry is the single-pawn answer, unchanged.
    public class ColonistsWorkPrioritiesDto
    {
        [ParameterDescription("How many pawns the answer holds.")]
        public int TotalPawns { get; set; }
        [ParameterDescription("One entry per pawn.")]
        public List<ColonistWorkPrioritiesDto> Pawns { get; set; } = new List<ColonistWorkPrioritiesDto>();
    }

    public class PawnTimeAssignmentRequestDto
    {
        public int PawnId { get; set; }
        [ParameterDescription("The hour of the day, 0 to 23.")]
        public int Hour { get; set; }
        [ParameterDescription("The time assignment's def name.")]
        public string Assignment { get; set; }
    }

    public class PawnWorkPrioritiesResponseDto
    {
        [ParameterDescription("One entry per pawn.")]
        public List<PawnWorkPrioritiesDto> Pawns { get; set; } = new List<PawnWorkPrioritiesDto>();
        [ParameterDescription("How many pawns the answer holds.")]
        public int TotalPawns { get; set; }
        [ParameterDescription("The game tick the table was last written.")]
        public string LastUpdated { get; set; }
    }

    public class PawnWorkPrioritiesDto
    {
        public int PawnId { get; set; }
        [ParameterDescription("The pawn's name, as the pawn list shows it.")]
        public string PawnName { get; set; }
        [ParameterDescription("The pawn's priority for each work type.")]
        public List<WorkPriorityDto> WorkPriorities { get; set; } = new List<WorkPriorityDto>();
    }

    public class WorkListDto
    {
        [ParameterDescription("One entry per work type.")]
        public List<string> Work { get; set; }
    }

    public class WorkPriorityRequestDto
    {
        [JsonProperty("id")]
        [ParameterDescription("The pawn's numeric id.")]
        public int Id { get; set; }

        [JsonProperty("work")]
        [ParameterDescription("The work type's def name.")]
        public string Work { get; set; } = "";

        [JsonProperty("priority")]
        [ParameterDescription("The priority to set, 1 highest to 4 lowest.")]
        public int Priority { get; set; }
    }

    public class ColonistsWorkPrioritiesRequestDto
    {
        [JsonProperty("priorities")]
        [ParameterDescription("One entry per pawn whose priorities change.")]
        public List<WorkPriorityRequestDto> Priorities { get; set; } = new List<WorkPriorityRequestDto>();
    }

    public class TimeAssignmentDto
    {
        [ParameterDescription("The time assignment's def name.")]
        public string Name { get; set; }
    }

    public class OutfitDto
    {
        [ParameterDescription("The apparel policy's numeric id.")]
        public int Id { get; set; }
        public string Label { get; set; }
        [ParameterDescription("The item filter behind the policy.")]
        public ThingFilterDto Filter { get; set; }

        // The pawns wearing this policy, the Assign tab's column (fox ticket
        // #4232): the write's answer said how many, so a caller that wanted
        // "who wears the tattered policy" read every colonist instead. The ids
        // are the same thing_id the pawn orders take.
        [ParameterDescription("The pawns that wear this policy.")]
        public List<OutfitPawnDto> Pawns { get; set; } = new List<OutfitPawnDto>();
    }

    public class OutfitPawnDto
    {
        public int PawnId { get; set; }
        public string Name { get; set; }
    }

    public class ThingFilterDto
    {
        [ParameterDescription("The thing defs the filter allows.")]
        public List<string> AllowedThingDefNames { get; set; }
        [ParameterDescription("The special filters it refuses.")]
        public List<string> DisallowedSpecialFilterDefNames { get; set; }
        [ParameterDescription("The lowest hit points an allowed item may have.")]
        public float AllowedHitPointsMin { get; set; }
        [ParameterDescription("The highest hit points an allowed item may have.")]
        public float AllowedHitPointsMax { get; set; }
        [ParameterDescription("The lowest quality an allowed item may have.")]
        public string AllowedQualityMin { get; set; }
        [ParameterDescription("The highest quality an allowed item may have.")]
        public string AllowedQualityMax { get; set; }
        [ParameterDescription("Whether the player may change the hit point bounds.")]
        public bool AllowedHitPointsConfigurable { get; set; }
        [ParameterDescription("Whether the player may change the quality bounds.")]
        public bool AllowedQualitiesConfigurable { get; set; }
    }

    public class PawnJobRequestDto
    {
        public int PawnId { get; set; }
        [ParameterDescription("The job's def name.")]
        public string JobDef { get; set; }
        [ParameterDescription("The numeric id of the thing the job targets.")]
        public int? TargetThingId { get; set; }
        [ParameterDescription("The map cell the job is aimed at.")]
        public PositionDto TargetPosition { get; set; }
    }

    public class MedicalTendRequestDto
    {
        [ParameterDescription("The patient's numeric id.")]
        public int PatientPawnId { get; set; }
        [ParameterDescription("The doctor's numeric id.")]
        public int? DoctorPawnId { get; set; }
    }

    public class MedicalBedRestRequestDto
    {
        [ParameterDescription("The patient's numeric id.")]
        public int PatientPawnId { get; set; }
        [ParameterDescription("The bed's numeric id.")]
        public int? BedBuildingId { get; set; }
    }
}
