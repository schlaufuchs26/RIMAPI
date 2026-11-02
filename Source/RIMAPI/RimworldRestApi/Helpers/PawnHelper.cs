using System;
using System.Collections.Generic;
using System.Linq;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Helpers
{
    public static class PawnHelper
    {
        public static Pawn FindPawnById(string id)
        {
            int.TryParse(id, out int pawnId);
            return FindPawnById(pawnId);
        }

        public static Pawn FindPawnById(int id)
        {
            // Search all maps
            foreach (var map in Find.Maps)
            {
                var pawn = map.mapPawns.AllPawns
                        .FirstOrDefault(p => p.ThingID.Equals(id.ToString()) || p.thingIDNumber == id);
                if (pawn != null) return pawn;
            }

            // Search world pawns (caravans, traveling, etc)
            return Find.WorldPawns.AllPawnsAliveOrDead
                    .FirstOrDefault(p => p.ThingID.Equals(id.ToString()) || p.thingIDNumber == id);
        }

        // RefusalStateOf collects the game's own state of one pawn for
        // CombatRefusal, which stays free of game types so a test can state
        // the rule (ticket #3433).
        public static CombatRefusal.PawnState RefusalStateOf(Pawn pawn)
        {
            MentalStateDef mental = pawn.MentalStateDef;
            return new CombatRefusal.PawnState
            {
                Dead = pawn.Dead,
                Spawned = pawn.Spawned,
                CarriedBy = pawn.CarriedBy != null ? pawn.CarriedBy.LabelShort : "",
                Colonist = pawn.IsColonist,
                MentalState = mental == null
                    ? ""
                    : CombatRefusal.MentalStateName(mental.label, mental.defName),
                Downed = pawn.Downed,
                HasDrafter = pawn.drafter != null,
            };
        }

        public static PawnDto PawnToDto(Pawn pawn)
        {
            return new PawnDto
            {
                Id = pawn.thingIDNumber,
                Name = pawn.Name?.ToStringShort ?? "Unknown",
                Gender = pawn.gender.ToString(),
                Age = pawn.ageTracker.AgeBiologicalYears,
                Health = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f,
                Mood = pawn.needs?.mood?.CurLevelPercentage ?? 0.5f,
                Hunger = pawn.needs.food?.CurLevel ?? 0,
                Position = new PositionDto
                {
                    X = pawn.Position.x,
                    Y = pawn.Position.y,
                    Z = pawn.Position.z,
                },
                // Areas are per map since 1.4, so the restriction is the one for
                // the map the pawn stands on; an unspawned pawn has none.
                AllowedArea = pawn.Spawned ? pawn.playerSettings?.AreaRestrictionInPawnCurrentMap?.Label : null,
                AllowedAreaCells = pawn.Spawned ? pawn.playerSettings?.AreaRestrictionInPawnCurrentMap?.TrueCount ?? 0 : 0,
                CarriedBy = pawn.CarriedBy?.LabelShort ?? "",
            };
        }

        // FindPawnForOrder resolves the pawn an order names the way the reads
        // do (fox ticket #3665): listerThings holds only spawned things, and a
        // carried pawn is despawned into his carrier's hands, so `rescue` and
        // `tend` answered "Pawn with ID=1063 not found" for a colonist the
        // colonists read listed while a raider carried him off the map. The
        // map's own pawn list keeps him, and it stays per-map: a pawn in a
        // caravan is on no map and is still reported as not found.
        public static Pawn FindPawnForOrder(Map map, int pawnId)
        {
            Pawn spawned = map.listerThings.AllThings.OfType<Pawn>()
                .FirstOrDefault(p => p.thingIDNumber == pawnId);
            if (spawned != null) return spawned;
            return map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber == pawnId);
        }

        public static bool AssignJob(Pawn pawn, JobDef jobDef, LocalTargetInfo target)
        {
            Job job = JobMaker.MakeJob(jobDef, target);
            return pawn.jobs.TryTakeOrderedJob(job);
        }

        public static bool AssignTendJob(Pawn doctor, Pawn patient)
        {
            Job job = JobMaker.MakeJob(JobDefOf.TendPatient, patient);
            return doctor.jobs.TryTakeOrderedJob(job);
        }

        public static bool AssignBedRest(Pawn patient, Building_Bed bed)
        {
            if (bed != null)
            {
                patient.ownership.ClaimBedIfNonMedical(bed);
            }
            Job job = JobMaker.MakeJob(JobDefOf.LayDown, patient.ownership.OwnedBed ?? bed);
            return patient.jobs.TryTakeOrderedJob(job);
        }

        public static PawnInventoryDto GetPawnInventory(Pawn pawn)
        {
            if (pawn == null) return null;

            var inventory = new PawnInventoryDto
            {
                Items = new List<ThingDto>(),
                Apparels = new List<ThingDto>(),
                Equipment = new List<ThingDto>()
            };

            // Inventory (Backpack)
            if (pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                foreach (var item in pawn.inventory.innerContainer)
                {
                    inventory.Items.Add(ResourcesHelper.ThingToDto(item));
                }
            }

            // Apparel (Clothes)
            if (pawn.apparel != null && pawn.apparel.WornApparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    inventory.Apparels.Add(ResourcesHelper.ThingToDto(apparel));
                }
            }

            // Equipment (Weapons)
            if (pawn.equipment != null && pawn.equipment.AllEquipmentListForReading != null)
            {
                foreach (var equip in pawn.equipment.AllEquipmentListForReading)
                {
                    inventory.Equipment.Add(ResourcesHelper.ThingToDto(equip));
                }
            }

            return inventory;
        }

        public static PawnDetailedDto PawnToDetailedDto(Pawn pawn)
        {
            try
            {
                return new PawnDetailedDto
                {
                    Sleep = pawn.needs.rest?.CurLevel ?? 0,
                    Comfort = pawn.needs.comfort?.CurLevel ?? 0,
                    Beauty = pawn.needs.beauty?.CurLevel ?? 0,
                    Joy = pawn.needs.joy?.CurLevel ?? 0,
                    Energy = pawn.needs.energy?.CurLevel ?? 0,
                    DrugsDesire = pawn.needs.drugsDesire?.CurLevel ?? 0,
                    SurroundingBeauty = pawn.needs.beauty?.CurLevel ?? 0,
                    FreshAir = pawn.needs.outdoors?.CurLevel ?? 0,
                    WorkInfo = new WorkInfoDto
                    {
                        Skills =
                            pawn.skills.skills?.Where(skill => skill != null && skill.def != null)
                                .Select(skill => new SkillDto
                                {
                                    Name = skill.def.defName,
                                    Level = skill.Level,
                                    Description = skill.def.description,
                                    MinLevel = SkillRecord.MinLevel,
                                    MaxLevel = SkillRecord.MaxLevel,
                                    LevelDescriptor = skill.LevelDescriptor,
                                    PermanentlyDisabled = skill.PermanentlyDisabled,
                                    TotallyDisabled = skill.TotallyDisabled,
                                    XpTotalEarned = skill.XpTotalEarned,
                                    XpProgressPercent = skill.XpProgressPercent,
                                    XpRequiredForLevelUp = skill.XpRequiredForLevelUp,
                                    XpSinceLastLevel = skill.xpSinceLastLevel,
                                    Aptitude = skill.Aptitude,
                                    Passion = (int)skill.passion,
                                    DisabledWorkTags = (int)skill.def.disablingWorkTags,
                                })
                                .ToList() ?? new List<SkillDto>(),
                        CurrentJob = pawn.CurJob?.def?.defName ?? "",
                        Traits = GetTraits(pawn),
                        WorkPriorities = GetWorkPriorities(pawn),
                        IncapableOf = GetIncapableOfWorkTags(pawn),
                        CanFight = CanFight(pawn),
                    },
                    PoliciesInfo = new PoliciesInfoDto
                    {
                        FoodPolicyId = pawn.foodRestriction?.CurrentFoodPolicy?.id ?? 0,
                        HostilityResponse = (int)pawn.playerSettings.hostilityResponse,
                    },
                    MoodInfo = GetMoodInfo(pawn),
                    MedicalInfo = new MedicalInfoDto
                    {
                        IsDead = pawn.Dead,
                        IsDowned = pawn.Downed,
                        DownedReason = DownedReason(pawn),
                        MentalState = MentalStateOf(pawn),
                        MentalStateAggro = MentalStateAggroOf(pawn),
                        Consciousness = CapacityOrZero(pawn, PawnCapacityDefOf.Consciousness),
                        Moving = CapacityOrZero(pawn, PawnCapacityDefOf.Moving),
                        Health = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 1f,
                        Hediffs = GetHediffs(pawn),
                        MedicalPolicyId = (int)(
                            pawn.playerSettings?.medCare ?? MedicalCareCategory.NoCare
                        ),
                        IsSelfTendAllowed = pawn.playerSettings?.selfTend ?? false,
                        InBed = pawn.InBed(),
                    },
                    SocialInfo = CreatePawnSocialInfoDto(pawn),
                    ReservedBy = ReservationsOn(pawn),
                };
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error converting pawn to DTO - {ex}");
                return null;
            }
        }

        // ReservationsOn lists the pawns that hold a reservation on this pawn
        // and the job each holds it for (fox ticket #4172). The game's own
        // manager is the source, so the read says exactly what the tend driver
        // will hit: it reserves the patient with maxPawns 1 and ends the job
        // when someone else holds him. A carried or off-map pawn has no map and
        // sends an empty list rather than a stale claim.
        private static List<ReservedByDto> ReservationsOn(Pawn pawn)
        {
            var holders = new List<ReservedByDto>();
            ReservationManager manager = pawn.Map?.reservationManager;
            if (manager == null)
                return holders;
            foreach (var reservation in manager.ReservationsReadOnly)
            {
                Pawn claimant = reservation.Claimant;
                if (claimant == null || reservation.Target.Thing != pawn)
                    continue;
                holders.Add(
                    new ReservedByDto
                    {
                        ClaimantId = claimant.thingIDNumber,
                        Claimant = claimant.LabelShort,
                        Job = reservation.Job?.def?.defName ?? "",
                    }
                );
            }
            return holders;
        }

        // DownedReason says why a downed pawn is down (ticket #3288). The state
        // itself is only Verse.Pawn_HealthTracker.healthState, so a colonist
        // who lies in bed at 100% summary health reads as "DOWNED" with no
        // cause; this names the condition behind it. The checks follow
        // ShouldBeDowned (no consciousness to stay awake, no Moving capacity,
        // pain shock, a life stage that is always downed) plus the two
        // force-downed states the game sets itself (deathrest, a mental
        // state). Empty for a pawn that is not downed.
        public static string DownedReason(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || !pawn.Downed)
                return "";

            try
            {
                var parts = new List<string>();
                if (pawn.InMentalState && pawn.MentalStateDef != null)
                    parts.Add("mental state " + pawn.MentalStateDef.defName);

                if (ModsConfig.BiotechActive && pawn.health.hediffSet.HasHediff(HediffDefOf.Deathrest))
                    parts.Add("deathrest");

                Hediff malnutrition = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition);
                if (malnutrition != null)
                    parts.Add("malnutrition severity " + malnutrition.Severity.ToString("0.00"));

                float consciousness = CapacityOrZero(pawn, PawnCapacityDefOf.Consciousness);
                if (consciousness < 0.3f)
                    parts.Add(
                        $"consciousness {Percent(consciousness)}{DownedCause(pawn, PawnCapacityDefOf.Consciousness)}"
                    );
                else if (
                    !pawn.RaceProps.doesntMove
                    && !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving)
                )
                    parts.Add(
                        $"cannot move, Moving {Percent(CapacityOrZero(pawn, PawnCapacityDefOf.Moving))}"
                            + DownedCause(pawn, PawnCapacityDefOf.Moving)
                    );

                if (pawn.health.InPainShock)
                    parts.Add(
                        $"pain shock, pain {Percent(pawn.health.hediffSet.PainTotal)} at or over "
                            + Percent(pawn.GetStatValue(StatDefOf.PainShockThreshold))
                    );

                var stage = pawn.ageTracker?.CurLifeStage;
                if (stage != null && stage.alwaysDowned)
                    parts.Add("life stage " + stage.defName + " is always downed");

                if (parts.Count == 0)
                    parts.Add("the game calls it downed with no capacity under its cap");
                return string.Join("; ", parts);
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error reading the downed reason of {pawn} - {ex}");
                return "";
            }
        }

        // MentalStateOf names the mental state a pawn is in (fox, #3513): a
        // colonist in a tantrum cannot take a tend job, so the fox's doctor
        // search needs the flag. Empty for a pawn in no state, and the label
        // reads ("tantrum") where the state has one.
        public static string MentalStateOf(Pawn pawn)
        {
            if (pawn == null)
                return "";

            MentalStateDef state = pawn.MentalStateDef;
            return state == null ? "" : CombatRefusal.MentalStateName(state.label, state.defName);
        }

        // MentalStateAggroOf is the game's own verdict on whether the pawn's
        // mental state attacks people (fox, #3761): a berserk pawn beats his
        // neighbours, a pawn in a sad wander or a corpse obsession only stops
        // working. The fox's wait steps game time through the harmless ones,
        // because a colony that cannot work cannot tend, feed or haul either,
        // and a list of state labels would break on a translated game. Null
        // for a pawn in no state.
        public static bool? MentalStateAggroOf(Pawn pawn)
        {
            if (pawn?.MentalStateDef == null)
                return null;
            return pawn.InAggroMentalState;
        }

        // NoJobReason names why the game would take no ordered job from this
        // pawn, in the shape a refusal sentence can carry (" (downed)",
        // " (in a mental state: tantrum)"), empty when nothing is in the way
        // (fox, #3548). A failed TryTakeOrderedJob answers with nothing else,
        // so the reason is the only part a caller can act on.
        public static string NoJobReason(Pawn pawn)
        {
            if (pawn == null)
                return "";

            return CombatRefusal.NoJobReason(pawn.Downed, MentalStateOf(pawn));
        }

        // DownedCause names the hediffs, genes or body parts that pull a
        // capacity down, using the game's own impactor list rather than a guess
        // about which hediff class does it.
        private static string DownedCause(Pawn pawn, PawnCapacityDef capacity)
        {
            try
            {
                var impactors = new List<PawnCapacityUtility.CapacityImpactor>();
                PawnCapacityUtility.CalculateCapacityLevel(pawn.health.hediffSet, capacity, impactors);
                var names = impactors
                    .Where(i => i.IsDirect)
                    .Select(i => i.Readable(pawn))
                    .Where(s => !s.NullOrEmpty())
                    .Distinct()
                    .ToList();
                if (names.Count == 0)
                    names = impactors
                        .Select(i => i.Readable(pawn))
                        .Where(s => !s.NullOrEmpty())
                        .Distinct()
                        .ToList();
                return names.Count == 0 ? "" : " from " + string.Join(", ", names);
            }
            catch
            {
                return "";
            }
        }

        private static float CapacityOrZero(Pawn pawn, PawnCapacityDef capacity)
        {
            try
            {
                return pawn.health?.capacities?.GetLevel(capacity) ?? 0f;
            }
            catch
            {
                return 0f;
            }
        }

        private static string Percent(float fraction)
        {
            return (fraction * 100f).ToString("F0") + "%";
        }

        public static SocialInfoDto CreatePawnSocialInfoDto(Pawn pawn)
        {
            var dto = new SocialInfoDto
            {
                Id = pawn.ThingID,
                Name = pawn.Name?.ToString(),
                DirectRelations = new List<RelationDto>(),
                ChildrenCount = pawn.relations.ChildrenCount,
            };

            foreach (var relation in pawn.relations.DirectRelations)
            {
                dto.DirectRelations.Add(
                    new RelationDto
                    {
                        relationDefName = relation.def.defName,
                        otherPawnId = relation.otherPawn.ThingID,
                        otherPawnName = relation.otherPawn.Name?.ToString(),
                    }
                );
            }
            return dto;
        }

        public static List<HediffDto> GetHediffs(Pawn pawn)
        {
            try
            {
                return pawn.health?.hediffSet?.hediffs?.Where(h => h != null)
                        .Select(h => HediffToDto(h))
                        .ToList() ?? new List<HediffDto>();
            }
            catch
            {
                return new List<HediffDto>();
            }
        }

        public static HediffDto HediffToDto(Hediff hediff)
        {
            if (hediff == null)
                return null;

            var dto = new HediffDto
            {
                LoadId = hediff.loadID,
                DefName = hediff.def?.defName,
                Label = hediff.Label,
                LabelCap = hediff.LabelCap,
                LabelInBrackets = hediff.LabelInBrackets,

                Severity = hediff.Severity,
                SeverityLabel = hediff.SeverityLabel,
                CurStageIndex = hediff.CurStageIndex,
                CurStageLabel = hediff.CurStage?.label,

                PartLabel = hediff.Part?.Label,
                PartDefName = hediff.Part?.def?.defName,

                AgeTicks = hediff.ageTicks,
                AgeString = hediff.ageTicks.ToStringTicksToPeriod(),

                Visible = hediff.Visible,
                IsPermanent = hediff.IsPermanent(),
                IsTended = hediff.IsTended(),
                TendableNow = hediff.TendableNow(),
                Bleeding = hediff.Bleeding,
                BleedRate = hediff.BleedRate,
                IsLethal = hediff.IsLethal,
                IsCurrentlyLifeThreatening = hediff.IsCurrentlyLifeThreatening,
                CanEverKill = hediff.CanEverKill(),

                IsInfection = hediff.def?.isInfection ?? false,
                SeverityPerDay = HediffRateOf(hediff.def),
                ImmunityPerDay = HediffImmunityRateOf(hediff.def),
                Immunity = ImmunityOf(hediff),

                SourceDefName = hediff.sourceDef?.defName,
                SourceLabel = hediff.sourceDef?.label,
                SourceBodyPartGroupDefName = hediff.sourceBodyPartGroup?.defName,
                SourceHediffDefName = hediff.sourceHediffDef?.defName,

                CombatLogText = hediff.combatLogText,
                TipStringExtra = hediff.TipStringExtra,
                PainFactor = hediff.PainFactor,
                PainOffset = hediff.PainOffset,
            };

            return dto;
        }

        /// <summary>
        /// The daily severity change the def states (fox, #3449): an
        /// immunizable infection climbs by severityPerDayNotImmune until
        /// immunity wins, a plain SeverityPerDay hediff by its severityPerDay.
        /// 0 means the def gives no rate, which the caller reads as "no
        /// projection" instead of falling back on a guess.
        /// </summary>
        public static float HediffRateOf(HediffDef def)
        {
            if (def == null)
                return 0f;

            var immunizable = def.CompProps<HediffCompProperties_Immunizable>();
            var perDay = def.CompProps<HediffCompProperties_SeverityPerDay>();
            return HediffRate.Pick(
                immunizable?.severityPerDayNotImmune ?? 0f,
                perDay?.severityPerDay ?? 0f);
        }

        /// <summary>
        /// The immunity a def's Immunizable comp states per day while the
        /// disease is present (fox, #3682). It is the second clock of the
        /// race: severity climbs at severityPerDayNotImmune, immunity at
        /// immunityPerDaySick, and whichever reaches 1.0 first decides the
        /// pawn. 0 means the def states no rate.
        /// </summary>
        public static float HediffImmunityRateOf(HediffDef def)
        {
            if (def == null)
                return 0f;

            var immunizable = def.CompProps<HediffCompProperties_Immunizable>();
            return HediffRate.PickImmunity(immunizable?.immunityPerDaySick ?? 0f);
        }

        /// <summary>
        /// The pawn's immunity progress against one hediff, 0 to 1 (fox,
        /// #3618). The game keeps one ImmunityRecord per disease in
        /// Pawn_HealthTracker's ImmunityHandler, so the race is per infection;
        /// the digest had nothing to race against and never printed a death
        /// date. 0 is what the game itself answers for a hediff with no record
        /// yet, which is the truth on the day an infection starts.
        /// </summary>
        public static float ImmunityOf(Hediff hediff)
        {
            var handler = hediff?.pawn?.health?.immunity;
            var def = hediff?.def;
            if (handler == null || def == null)
                return 0f;

            try
            {
                return handler.GetImmunity(def);
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error reading the immunity of {hediff} - {ex}");
                return 0f;
            }
        }

        // GetMoodInfo lists the thoughts the game currently counts toward this
        // colonist's mood (fox, #3129). Without it a low mood is a number with
        // no cause: a trait, a bad bedroom and a missing joy type all read the
        // same.
        public static MoodInfoDto GetMoodInfo(Pawn pawn)
        {
            var dto = new MoodInfoDto { Thoughts = new List<MoodThoughtDto>() };
            var mood = pawn.needs?.mood;
            if (mood == null)
                return dto;

            dto.Mood = mood.CurLevelPercentage;
            try
            {
                var thoughts = new List<Thought>();
                mood.thoughts.GetAllMoodThoughts(thoughts);
                foreach (var thought in thoughts)
                {
                    if (thought?.def == null)
                        continue;

                    var entry = new MoodThoughtDto
                    {
                        DefName = thought.def.defName,
                        Label = thought.LabelCap,
                        StageLabel = thought.CurStage?.label,
                        MoodOffset = thought.MoodOffset(),
                        Kind = thought is Thought_Memory ? "memory" : "situation",
                    };
                    if (thought is Thought_Memory memory)
                    {
                        entry.AgeDays = memory.age / GenDate.TicksPerDay;
                        entry.Permanent = memory.permanent;
                    }
                    if (thought is Thought_MemorySocial social)
                    {
                        entry.OtherPawn = social.otherPawn?.LabelShort;
                    }
                    dto.Thoughts.Add(entry);
                }

                // Worst first: a caller that only shows the head of the list
                // must see the thought that costs the most mood.
                dto.Thoughts = dto.Thoughts.OrderBy(t => t.MoodOffset).ToList();
                dto.TotalOffset = dto.Thoughts.Sum(t => t.MoodOffset);
            }
            catch (Exception ex)
            {
                Core.LogApi.Error($"Error reading mood thoughts - {ex}");
            }
            return dto;
        }

        public static List<TraitDto> GetTraits(Pawn pawn)
        {            try
            {
                return pawn.story?.traits?.allTraits?.Where(t => t != null)
                        .Select(t => new TraitDto
                        {
                            Name = t.def.defName,
                            Label = t.Label,
                            Description = t.def.description,
                            DisabledWorkTags = (int)t.def.disabledWorkTags,
                            Suppressed = t.Suppressed,
                        })
                        .ToList() ?? new List<TraitDto>();
            }
            catch
            {
                return new List<TraitDto>();
            }
        }

        // GetIncapableOfWorkTags reads the pawn's own disable mask, which the
        // game builds from backstory, traits and genes; the words come from
        // IncapabilityTags (ticket #3360).
        public static List<string> GetIncapableOfWorkTags(Pawn pawn)
        {
            try
            {
                return IncapabilityTags.From(pawn.CombinedDisabledWorkTags);
            }
            catch
            {
                return new List<string>();
            }
        }

        // CanFight is false when the game disables the Violent tag for him: no
        // draft and no order can make such a pawn shoot.
        public static bool CanFight(Pawn pawn)
        {
            try
            {
                return IncapabilityTags.CanFight(pawn.CombinedDisabledWorkTags);
            }
            catch
            {
                // A pawn we cannot ask keeps the old reading; a wrong "cannot
                // fight" would hide a fighter instead of naming a harmless one.
                return true;
            }
        }

        public static List<WorkPriorityDto> GetWorkPriorities(Pawn pawn)
        {
            var priorities = new List<WorkPriorityDto>();

            try
            {
                if (pawn.workSettings == null)
                    return priorities;

                foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefs)
                {
                    if (workType == null)
                        continue;

                    var priority = pawn.workSettings.GetPriority(workType);
                    if (priority > 0)
                    {
                        priorities.Add(
                            new WorkPriorityDto
                            {
                                WorkType = workType.defName,
                                Priority = priority,
                                IsTotallyDisabled = pawn.WorkTypeIsDisabled(workType),
                            }
                        );
                    }
                }

                return priorities.OrderBy(p => p.Priority).ToList();
            }
            catch (Exception ex)
            {
                Core.LogApi.Error(
                    $"Error getting work priorities for pawn {pawn.thingIDNumber} - {ex.Message}"
                );
                return priorities;
            }
        }

        public static ThingFilterDto GetThingFilterDto(ThingFilter filter)
        {
            var disallowedFilters = DefDatabase<SpecialThingFilterDef>
                .AllDefs.Where(sf => !filter.Allows(sf))
                .Select(sf => sf.defName)
                .ToList();

            return new ThingFilterDto
            {
                AllowedThingDefNames = filter.AllowedThingDefs.Select(d => d.defName).ToList(),
                DisallowedSpecialFilterDefNames = disallowedFilters,
                AllowedHitPointsMin = filter.AllowedHitPointsPercents.min,
                AllowedHitPointsMax = filter.AllowedHitPointsPercents.max,
                AllowedQualityMin = filter.AllowedQualityLevels.min.ToString(),
                AllowedQualityMax = filter.AllowedQualityLevels.max.ToString(),
                AllowedHitPointsConfigurable = filter.allowedHitPointsConfigurable,
                AllowedQualitiesConfigurable = filter.allowedQualitiesConfigurable,
            };
        }

        public static List<OutfitDto> GetOutfits()
        {
            List<OutfitDto> outfits = new List<OutfitDto>();

            foreach (var policy in Current.Game.outfitDatabase.AllOutfits)
            {
                outfits.Add(
                    new OutfitDto
                    {
                        Id = policy.id,
                        Label = policy.label,
                        Filter = GetThingFilterDto(policy.filter),
                    }
                );
            }

            return outfits;
        }
    }
}
