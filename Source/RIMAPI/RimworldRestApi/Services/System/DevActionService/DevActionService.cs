using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LudeonTK;
using RIMAPI.Core;
using RIMAPI.Helpers;
using RIMAPI.Models;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RIMAPI.Services
{
    /// <summary>
    /// The `dev` action (fox ticket #3757): build a test situation in a
    /// throwaway game instead of hunting for a save that already carries it.
    /// 31 live checks were left undone on 2026-11-01 because no save held the
    /// state they needed (an infected pawn, a berserk colonist, a raid, a job
    /// that has to finish).
    ///
    /// A state the ops here do not name is still reachable through the game's
    /// own dev-action menu (ticket #3799): `debug_actions` lists it and
    /// `debug_action` runs an entry, so the fox works with the tools a player in
    /// dev mode has instead of waiting for a new op per state.
    ///
    /// The whole route is refused unless the game itself runs in dev mode, so a
    /// colony the fox plays honestly cannot be tampered with even if the flag
    /// were passed by mistake. Every op ends by reading the live object back:
    /// the answer says what the game holds now, never what the request asked
    /// for.
    /// </summary>
    public partial class DevActionService : IDevActionService
    {
        /// <summary>The infection a `infection` op adds, in the game's own defNames.</summary>
        private static readonly string[] InfectionDefs = { "WoundInfection", "Flu", "Plague", "Malaria" };

        public ApiResult<DevActionResultDto> Run(DevActionRequest request)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                if (request == null)
                {
                    return ApiResult<DevActionResultDto>.Fail(
                        "a dev call needs a JSON body with `op`; the ops are " + DevActionRules.OpList()
                    );
                }
                if (!Prefs.DevMode)
                {
                    return ApiResult<DevActionResultDto>.Fail(
                        "dev mode is off in this game, so no dev op runs: the debug start "
                            + "(deploy/rimworld/debug-instance.sh) turns it on in the throwaway "
                            + "game's Prefs.xml, and the colony the fox plays honestly never does"
                    );
                }
                Map map = Find.CurrentMap;
                if (map == null)
                {
                    return ApiResult<DevActionResultDto>.Fail(
                        "no colony is loaded, so there is nothing to build a state in"
                    );
                }

                string problem = DevActionRules.Problem(
                    request.Op,
                    request.PawnId,
                    request.Name,
                    request.Amount,
                    request.Level,
                    request.Count,
                    request.Ticks
                );
                if (problem != null)
                {
                    return ApiResult<DevActionResultDto>.Fail(problem);
                }

                DevActionResultDto result;
                switch (request.Op)
                {
                    case "damage":
                        result = Damage(map, request);
                        break;
                    case "heal":
                        result = Heal(request);
                        break;
                    case "hediff":
                        result = AddHediff(request, null);
                        break;
                    case "infection":
                        result = AddHediff(request, InfectionDefs);
                        break;
                    case "spawn":
                        result = SpawnItem(map, request);
                        break;
                    case "spawn_animal":
                        result = SpawnPawnKind(map, request);
                        break;
                    case "spawn_pawn":
                        result = SpawnPawnKind(map, request);
                        break;
                    case "mental":
                        result = Mental(request);
                        break;
                    case "skill":
                        result = SetSkill(request);
                        break;
                    case "trait":
                        result = SetTrait(request);
                        break;
                    case "incident":
                        result = FireIncident(map, request);
                        break;
                    case "quest":
                        result = OfferQuest(map, request);
                        break;
                    case "wealth":
                        result = AddWealth(map, request);
                        break;
                    case "ticks":
                        result = RunTicks(request);
                        break;
                    case "debug_actions":
                        result = ListDebugActions(request);
                        break;
                    case "debug_action":
                        result = RunDebugAction(request);
                        break;
                    default:
                        return ApiResult<DevActionResultDto>.Fail(
                            "unknown op \"" + request.Op + "\"; use one of " + DevActionRules.OpList()
                        );
                }
                result.Seconds = (float)watch.Elapsed.TotalSeconds;
                return ApiResult<DevActionResultDto>.Ok(result);
            }
            catch (DevRefusal ex)
            {
                return ApiResult<DevActionResultDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResult<DevActionResultDto>.Fail(ex.ToString());
            }
        }

        // ---- pawn ops -----------------------------------------------------

        private DevActionResultDto Damage(Map map, DevActionRequest request)
        {
            Pawn pawn = Pawn(request.PawnId);
            float before = pawn.health.summaryHealth.SummaryHealthPercent;
            DamageDef def =
                DefDatabase<DamageDef>.GetNamedSilentFail(
                    string.IsNullOrEmpty(request.Name) ? "Cut" : request.Name
                )
                ?? DamageDefOf.Cut;
            BodyPartRecord part = Part(pawn, request.Part);
            DamageInfo dinfo = new DamageInfo(def, request.Amount.Value);
            if (part != null) dinfo.SetHitPart(part);

            pawn.TakeDamage(dinfo);
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Pawn = DescribePawn(pawn),
            };
            result.Detail =
                pawn.LabelShort
                + " took "
                + request.Amount.Value
                + " "
                + def.defName
                + " damage"
                + (part == null ? " (a random part)" : " to the " + part.Label)
                + "; health "
                + Percent(before)
                + " -> "
                + Percent(result.Pawn.HealthPercent)
                + (pawn.Dead ? ", and he died" : pawn.Downed ? ", and he is down" : "");
            return result;
        }

        private DevActionResultDto Heal(DevActionRequest request)
        {
            Pawn pawn = Pawn(request.PawnId);
            float before = pawn.health.summaryHealth.SummaryHealthPercent;
            int injuries = InjuryCount(pawn);
            HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(pawn);
            pawn.health.RestorePart(null);
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Pawn = DescribePawn(pawn),
            };
            result.Detail =
                pawn.LabelShort
                + " healed: health "
                + Percent(before)
                + " -> "
                + Percent(result.Pawn.HealthPercent)
                + ", "
                + injuries
                + " injuries before, "
                + result.Pawn.Injuries
                + " now"
                + (result.Pawn.Dead ? " (he was already dead, so nothing brings him back)" : "");
            return result;
        }

        /// <summary>
        /// Adds a hediff, either the one the request names or the first of
        /// <paramref name="candidates"/> the game knows (the infection op, whose
        /// defName differs between a wound infection and a disease).
        /// </summary>
        private DevActionResultDto AddHediff(DevActionRequest request, string[] candidates)
        {
            Pawn pawn = Pawn(request.PawnId);
            HediffDef def = null;
            if (candidates != null)
            {
                foreach (string name in candidates)
                {
                    def = DefDatabase<HediffDef>.GetNamedSilentFail(name);
                    if (def != null) break;
                }
                if (def == null)
                {
                    throw new DevRefusal(
                        "this game knows none of "
                            + string.Join(", ", candidates)
                            + ", so the infection op has nothing to add"
                    );
                }
            }
            else
            {
                def = DefDatabase<HediffDef>.GetNamedSilentFail(request.Name);
                if (def == null)
                {
                    throw new DevRefusal(
                        "\""
                            + request.Name
                            + "\" is not a hediff this game knows; the `defs` read lists the "
                            + "defNames, e.g. WoundInfection, Flu, Plague, Malaria"
                    );
                }
            }
            BodyPartRecord part = Part(pawn, request.Part);
            Hediff hediff = HediffMaker.MakeHediff(def, pawn, part);
            float? severity = DevActionRules.Severity(request.Amount);
            if (severity.HasValue) hediff.Severity = severity.Value;
            pawn.health.AddHediff(hediff, part);

            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Pawn = DescribePawn(pawn),
            };
            Hediff after = pawn.health.hediffSet.hediffs
                .LastOrDefault(h => h.def == def && h.Part == part);
            result.Detail =
                pawn.LabelShort
                + " now carries "
                + def.defName
                + (part == null ? "" : " on the " + part.Label)
                + (after == null
                    ? "; the game does not list it"
                    : " at severity " + after.Severity.ToString("0.##"))
                + " (" + pawn.health.hediffSet.hediffs.Count + " hediffs in all)";
            return result;
        }

        private DevActionResultDto Mental(DevActionRequest request)
        {
            Pawn pawn = Pawn(request.PawnId);
            MentalStateDef def = DefDatabase<MentalStateDef>.GetNamedSilentFail(request.Name);
            if (def == null)
            {
                throw new DevRefusal(
                    "\""
                        + request.Name
                        + "\" is not a mental state this game knows; the `defs` read lists them, "
                        + "e.g. Berserk, Wander_Psychotic, InsultingSpree, MurderousRage, "
                        + "FirestartingSpree, CorpseObsession, GiveUpExit, PanicFlee, Tantrum"
                );
            }
            bool started = pawn.mindState.mentalStateHandler.TryStartMentalState(
                def,
                "dev",
                true,
                true,
                false,
                null,
                false
            );
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = started,
                Pawn = DescribePawn(pawn),
            };
            result.Detail =
                pawn.LabelShort
                + (started ? " is now in " : " refused ")
                + def.defName
                + (started
                    ? " (the game's own handler took it)"
                    : ": the game's handler would not start it, and the pawn reads "
                        + (pawn.MentalStateDef == null ? "calm" : pawn.MentalStateDef.defName));
            return result;
        }

        private DevActionResultDto SetSkill(DevActionRequest request)
        {
            Pawn pawn = Pawn(request.PawnId);
            if (pawn.skills == null)
            {
                throw new DevRefusal(pawn.LabelShort + " has no skills: an animal or a mech carries none");
            }
            SkillDef def = DefDatabase<SkillDef>.GetNamedSilentFail(request.Name);
            if (def == null)
            {
                throw new DevRefusal(
                    "\""
                        + request.Name
                        + "\" is not a skill this game knows; use the defName, e.g. Cooking, "
                        + "Growing, Construction, Medicine, Shooting"
                );
            }
            SkillRecord record = pawn.skills.GetSkill(def);
            if (record == null)
            {
                throw new DevRefusal(pawn.LabelShort + " has no " + def.defName + " skill record");
            }
            int before = record.Level;
            record.Level = request.Level.Value;
            record.xpSinceLastLevel = record.XpRequiredForLevelUp / 2f;

            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Pawn = DescribePawn(pawn),
            };
            result.Detail =
                pawn.LabelShort
                + "'s "
                + def.defName
                + " is now "
                + record.Level
                + " (was "
                + before
                + ")";
            return result;
        }

        private DevActionResultDto SetTrait(DevActionRequest request)
        {
            Pawn pawn = Pawn(request.PawnId);
            if (pawn.story?.traits == null)
            {
                throw new DevRefusal(pawn.LabelShort + " has no trait tracker: an animal carries none");
            }
            TraitDef def = DefDatabase<TraitDef>.GetNamedSilentFail(request.Name);
            if (def == null)
            {
                throw new DevRefusal(
                    "\""
                        + request.Name
                        + "\" is not a trait this game knows; use the defName from the `defs` read"
                );
            }
            bool had = pawn.story.traits.HasTrait(def);
            if (!had)
            {
                int degree = def.degreeDatas != null && def.degreeDatas.Count > 0
                    ? def.degreeDatas[0].degree
                    : 0;
                pawn.story.traits.GainTrait(new Trait(def, degree));
            }
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Pawn = DescribePawn(pawn),
            };
            result.Detail =
                pawn.LabelShort
                + (had ? " already had " : " gained ")
                + def.defName
                + " (" + pawn.story.traits.allTraits.Count + " traits now)";
            return result;
        }

        // ---- map ops ------------------------------------------------------

        private DevActionResultDto SpawnItem(Map map, DevActionRequest request)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(request.Name);
            if (def == null)
            {
                throw new DevRefusal(
                    "\""
                        + request.Name
                        + "\" is not a thing this game knows; the `defs` read lists the defNames, "
                        + "e.g. Steel, Medicine, ComponentIndustrial"
                );
            }
            if (def.category != ThingCategory.Item)
            {
                throw new DevRefusal(
                    def.defName
                        + " is a "
                        + def.category
                        + ", not an item: `build` places a building, and a spawn would leave "
                        + "a thing the game does not count as stock"
                );
            }
            int count = DevActionRules.SpawnCount(request.Count);
            IntVec3 cell = Cell(map, request);
            List<Thing> placed = PlaceItems(map, def, count, cell);
            if (placed.Count == 0)
            {
                throw new DevRefusal("no free cell near (" + cell.x + "," + cell.z + ") to place " + def.defName);
            }
            Thing last = placed[placed.Count - 1];
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Thing = DescribeThing(last),
            };
            result.Detail =
                "placed "
                + placed.Sum(t => t.stackCount)
                + " "
                + def.defName
                + " in "
                + placed.Count
                + " stack(s) near ("
                + cell.x
                + ","
                + cell.z
                + "); the newest is id "
                + last.thingIDNumber;
            return result;
        }

        /// <summary>
        /// Places pawns of one kind: an animal, a humanlike, a mech. It runs the
        /// game's own dev spawner (GeneratePawn, GenSpawn, then a lord for a
        /// hostile faction), so a state the player can build in dev mode is one
        /// call here: a wild man is `name=WildMan`, a raid party is a kind with
        /// its faction named (ticket #3799).
        /// </summary>
        private DevActionResultDto SpawnPawnKind(Map map, DevActionRequest request)
        {
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(request.Name);
            if (kind == null)
            {
                throw new DevRefusal(
                    "\""
                        + request.Name
                        + "\" is not a pawn kind this game knows; use the defName, e.g. WildMan, "
                        + "Colonist, Muffalo, Boomrat, Warg, Chicken"
                );
            }
            Faction faction = FactionOf(kind, request.Faction);
            int count = DevActionRules.SpawnCount(request.Count);
            IntVec3 cell = Cell(map, request);
            Pawn last = null;
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction, map.Tile);
                if (pawn == null)
                {
                    throw new DevRefusal("the game generated no " + kind.defName + " (pawn generation failed)");
                }
                IntVec3 at = CellFinder.RandomClosewalkCellNear(cell, map, 4);
                GenSpawn.Spawn(pawn, at, map, WipeMode.Vanish);
                PostPawnSpawn(pawn);
                last = pawn;
                spawned++;
            }
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = spawned > 0,
                Thing = last == null ? null : DescribeThing(last),
                Pawn = last == null ? null : DescribePawn(last),
            };
            result.Detail =
                "spawned "
                + spawned
                + " "
                + kind.defName
                + " near ("
                + cell.x
                + ","
                + cell.z
                + ")"
                + (last == null ? "" : "; the newest is id " + last.thingIDNumber)
                + (faction == null ? ", which no faction owns" : ", in " + faction.Name);
            return result;
        }

        /// <summary>
        /// The faction a spawn puts the pawn in: the request's word (player,
        /// none, or a faction's name or defName), else the pawn kind's own
        /// default, which is what the game's dev spawner uses.
        /// </summary>
        private static Faction FactionOf(PawnKindDef kind, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return FactionUtility.DefaultFactionFrom(kind.defaultFactionDef);
            }
            if (string.Equals(name, "none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if (string.Equals(name, "player", StringComparison.OrdinalIgnoreCase))
            {
                return Faction.OfPlayer;
            }
            Faction found = Find.FactionManager.AllFactions.FirstOrDefault(f =>
                string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(f.def.defName, name, StringComparison.OrdinalIgnoreCase)
            );
            if (found == null)
            {
                string known = string.Join(
                    ", ",
                    Find
                        .FactionManager.AllFactions.Where(f => !f.IsPlayer)
                        .Select(f => f.def.defName)
                        .Take(12)
                );
                throw new DevRefusal(
                    "\""
                        + name
                        + "\" is not a faction this game knows; use player, none, or one of "
                        + known
                );
            }
            return found;
        }

        /// <summary>
        /// What the game's own dev spawner does after it placed a pawn: a pawn
        /// of another faction joins a lord, or he stands where he landed and
        /// never acts.
        /// </summary>
        private static void PostPawnSpawn(Pawn pawn)
        {
            if (pawn.Spawned && pawn.Faction != null && pawn.Faction != Faction.OfPlayer)
            {
                Lord lord = null;
                if (pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction).Any(p => p != pawn))
                {
                    lord = (
                        (Pawn)
                            GenClosest.ClosestThing_Global(
                                pawn.Position,
                                pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction),
                                99999f,
                                p => p != pawn && ((Pawn)p).GetLord() != null
                            )
                    )?.GetLord();
                }
                if (lord == null || !lord.CanAddPawn(pawn))
                {
                    lord = LordMaker.MakeNewLord(
                        pawn.Faction,
                        new LordJob_DefendPoint(pawn.Position),
                        Find.CurrentMap
                    );
                }
                if (lord != null && lord.LordJob.CanAutoAddPawns)
                {
                    lord.AddPawn(pawn);
                }
            }
            pawn.Rotation = Rot4.South;
        }

        private DevActionResultDto FireIncident(Map map, DevActionRequest request)
        {
            IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(request.Name);
            if (def == null)
            {
                throw new DevRefusal(
                    "\""
                        + request.Name
                        + "\" is not an incident this game knows; the defNames include RaidEnemy, "
                        + "RaidFriendly, ManhunterPack, Infestation, Blight, ColdSnap, HeatWave, "
                        + "SolarFlare, Eclipse, ToxicFallout, VolcanicWinter, WandererJoins, "
                        + "ResourcePodCrash, TraderCaravanArrival"
                );
            }
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
            parms.forced = true;
            bool executed = def.Worker.TryExecute(parms);
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = executed,
                Incident = DescribeIncidents(map, def.defName, executed),
            };
            result.Detail =
                def.defName
                + (executed
                    ? " fired: the game's own worker ran it"
                    : " did not fire: the game's own worker refused it (its CanFireNow said no)")
                + "; "
                + result.Incident.Incidents
                + " incidents in the map's log, "
                + result.Incident.Letters
                + " letters standing";
            return result;
        }

        private DevActionResultDto OfferQuest(Map map, DevActionRequest request)
        {
            QuestScriptDef script = null;
            if (!string.IsNullOrEmpty(request.Name))
            {
                script = DefDatabase<QuestScriptDef>.GetNamedSilentFail(request.Name);
                if (script == null)
                {
                    throw new DevRefusal(
                        "\""
                            + request.Name
                            + "\" is not a quest script this game knows; leave `name` empty for a "
                            + "random one the game can run on this map"
                    );
                }
            }
            else
            {
                List<QuestScriptDef> candidates = DefDatabase<QuestScriptDef>
                    .AllDefsListForReading.Where(d => d.root != null && d.CanRun(new RimWorld.QuestGen.Slate(), map))
                    .ToList();
                if (candidates.Count == 0)
                {
                    throw new DevRefusal("no quest script can run on this map right now");
                }
                script = candidates[Rand.Range(0, candidates.Count)];
            }

            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(
                script,
                StorytellerUtility.DefaultThreatPointsNow(map)
            );
            bool made = quest != null;
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = made,
                Incident = DescribeIncidents(map, script.defName, made),
            };
            result.Detail =
                (made
                    ? "offered "
                    : "the game made no quest from ")
                + script.defName
                + (made ? " (quest id " + quest.id + ")" : "")
                + "; "
                + result.Incident.Letters
                + " letters standing, "
                + Find.QuestManager.QuestsListForReading.Count
                + " quests known";
            return result;
        }

        private DevActionResultDto AddWealth(Map map, DevActionRequest request)
        {
            map.wealthWatcher.ForceRecount();
            float before = map.wealthWatcher.WealthTotal;
            float amount = request.Amount.Value;
            List<Thing> placed = new List<Thing>();
            if (amount > 0)
            {
                IntVec3 cell = Cell(map, request);
                placed = PlaceItems(map, ThingDefOf.Silver, (int)amount, cell);
                if (placed.Count == 0)
                {
                    throw new DevRefusal("no free cell near (" + cell.x + "," + cell.z + ") to place the silver");
                }
            }
            map.wealthWatcher.ForceRecount();
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Wealth = new DevWealthStateDto
                {
                    Before = before,
                    After = map.wealthWatcher.WealthTotal,
                    Added = placed.Sum(t => t.stackCount),
                },
            };
            result.Detail =
                "colony wealth is derived, so this adds silver: "
                + result.Wealth.Added
                + " placed, wealth "
                + before.ToString("0")
                + " -> "
                + result.Wealth.After.ToString("0")
                + " (the game recounts what the map holds)";
            return result;
        }

        private DevActionResultDto RunTicks(DevActionRequest request)
        {
            TickManager ticks = Find.TickManager;
            int before = ticks.TicksGame;
            // The handler runs on the game's own loop, so a nested tick is only
            // safe while nothing else ticks: pause first, and leave the game
            // paused (the same hand-back the fox's wait does).
            if (!ticks.Paused) ticks.Pause();
            int want = request.Ticks.Value;
            int ran = 0;
            for (int i = 0; i < want; i++)
            {
                if (!ticks.Paused) ticks.Pause();
                ticks.DoSingleTick();
                ran++;
            }
            DevActionResultDto result = new DevActionResultDto
            {
                Op = request.Op,
                Done = true,
                Ticks = new DevTickStateDto
                {
                    Before = before,
                    After = ticks.TicksGame,
                    Ran = ran,
                },
            };
            result.Detail =
                "ran "
                + ran
                + " ticks by hand: the clock went from "
                + before
                + " to "
                + ticks.TicksGame
                + " ("
                + ((ticks.TicksGame - before) / (float)GenDate.TicksPerHour).ToString("0.##")
                + " in-game hours); the game is paused now";
            return result;
        }


        // ---- helpers ------------------------------------------------------

        private static Pawn Pawn(int? id)
        {
            Pawn pawn = PawnHelper.FindPawnById(id.Value);
            if (pawn == null)
            {
                throw new ArgumentException(
                    "no pawn with id " + id.Value + " on the map or in the world; the `colonists` read prints the ids"
                );
            }
            return pawn;
        }

        /// <summary>
        /// The body part a request names, or null for the game's own choice.
        /// The part is matched by defName (leg, arm, head) and by label, so a
        /// caller can write either.
        /// </summary>
        private static BodyPartRecord Part(Pawn pawn, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            BodyPartRecord found = pawn.health.hediffSet
                .GetNotMissingParts()
                .FirstOrDefault(p =>
                    string.Equals(p.def.defName, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p.Label, name, StringComparison.OrdinalIgnoreCase)
                );
            if (found == null)
            {
                string parts = string.Join(
                    ", ",
                    pawn.health.hediffSet.GetNotMissingParts().Select(p => p.def.defName).Distinct()
                );
                throw new ArgumentException(
                    "\""
                        + name
                        + "\" is not a body part "
                        + pawn.LabelShort
                        + " still has; the parts are "
                        + parts
                );
            }
            return found;
        }

        /// <summary>
        /// Places <paramref name="count"/> of one item, split into the stacks the
        /// def allows, and returns what the map holds. A full cell falls back to
        /// the nearest free one, which is what the game's own dev spawn does.
        /// </summary>
        private static List<Thing> PlaceItems(Map map, ThingDef def, int count, IntVec3 cell)
        {
            List<Thing> placed = new List<Thing>();
            int remaining = count;
            int stackLimit = Math.Max(1, def.stackLimit);
            while (remaining > 0)
            {
                Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
                int stack = Math.Min(remaining, stackLimit);
                thing.stackCount = stack;
                if (!GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near, out Thing landed))
                {
                    break;
                }
                placed.Add(landed);
                remaining -= stack;
            }
            return placed;
        }

        /// <summary>
        /// The cell a spawn goes to: the request's, else a colonist's, else the
        /// map's centre. A request that names one coordinate only is refused by
        /// the caller's own Problem check, so this reads both or neither.
        /// </summary>
        private static IntVec3 Cell(Map map, DevActionRequest request)
        {
            if (request.X.HasValue && request.Z.HasValue)
            {
                return new IntVec3(request.X.Value, 0, request.Z.Value);
            }
            Pawn colonist = map.mapPawns.FreeColonistsSpawned.FirstOrDefault();
            if (colonist != null) return colonist.Position;
            return map.Center;
        }

        private static int InjuryCount(Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs.Count(h =>
                h is Hediff_Injury || h is Hediff_MissingPart
            );
        }

        private static string Percent(float share)
        {
            return (share * 100f).ToString("0") + "%";
        }

        private static PositionDto PositionOf(IntVec3 cell)
        {
            return new PositionDto { X = cell.x, Y = cell.y, Z = cell.z };
        }

        private static DevThingStateDto DescribeThing(Thing thing)
        {
            return new DevThingStateDto
            {
                Id = thing.thingIDNumber,
                DefName = thing.def.defName,
                Label = thing.Label,
                StackCount = thing.stackCount,
                Position = PositionOf(thing.Position),
            };
        }

        private static DevIncidentStateDto DescribeIncidents(
            Map map,
            string defName,
            bool executed
        )
        {
            List<IncidentDto> log = GameEventsHelper.GetIncidentsLog(map);
            return new DevIncidentStateDto
            {
                DefName = defName,
                Executed = executed,
                Incidents = log.Count,
                Newest = log.Count > 0 ? log[0].IncidentDef : "",
                Letters = Find.LetterStack?.LettersListForReading?.Count ?? 0,
            };
        }

        /// <summary>
        /// One pawn as the game holds him now: health share, wounds, hediffs,
        /// skills and traits. Read after the op, never from the request.
        /// </summary>
        private static DevPawnStateDto DescribePawn(Pawn pawn)
        {
            DevPawnStateDto dto = new DevPawnStateDto
            {
                Id = pawn.thingIDNumber,
                Name = pawn.Name?.ToStringShort ?? pawn.LabelShort,
                DefName = pawn.def.defName,
                HealthPercent = pawn.health.summaryHealth.SummaryHealthPercent,
                Downed = pawn.Downed,
                Dead = pawn.Dead,
                MentalState = pawn.MentalStateDef == null ? "" : pawn.MentalStateDef.defName,
                Injuries = InjuryCount(pawn),
                Faction = pawn.Faction == null ? "" : pawn.Faction.Name,
                Wild = pawn.IsWildMan(),
            };
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                dto.Hediffs.Add(
                    new DevHediffDto
                    {
                        DefName = hediff.def.defName,
                        Label = hediff.Label,
                        Severity = hediff.Severity,
                        Part = hediff.Part == null ? "" : hediff.Part.Label,
                        Injury = hediff is Hediff_Injury || hediff is Hediff_MissingPart,
                        Tended = hediff.TryGetComp<HediffComp_TendDuration>()?.IsTended ?? false,
                    }
                );
            }
            if (pawn.skills != null)
            {
                foreach (SkillRecord record in pawn.skills.skills)
                {
                    dto.Skills.Add(
                        new DevSkillDto
                        {
                            DefName = record.def.defName,
                            Label = record.def.label,
                            Level = record.Level,
                            Passion = record.passion.ToString(),
                        }
                    );
                }
            }
            if (pawn.story?.traits != null)
            {
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    dto.Traits.Add(trait.def.defName);
                }
            }
            return dto;
        }

        /// <summary>
        /// A refusal the op raises after its shape passed: a defName the game
        /// does not know, a pawn without skills. It reaches the caller as the
        /// route's own error, not as a stack trace.
        /// </summary>
        private class DevRefusal : Exception
        {
            public DevRefusal(string message) : base(message) { }
        }
    }
}
