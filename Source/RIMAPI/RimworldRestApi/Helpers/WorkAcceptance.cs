using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// Which colonist will do the work a designation leaves behind (fox ticket
    /// #3625). The tool answered "colonists with PlantCutting cut them"
    /// whether or not anyone would: colony 14 marked 19 trees for chopping
    /// with no wood in the store, and the run could not tell "wait for a free
    /// colonist" from "nobody will ever cut these".
    ///
    /// The walk mirrors the work loop: each free colonist is asked the work
    /// giver's own question on the marked targets, so the answer names the
    /// colonists whose check accepts the job and, for the rest, the check that
    /// refuses them.
    /// </summary>
    public static class WorkAcceptance
    {
        /// <summary>
        /// How many marked targets one colonist is tried on. A sweep marks
        /// hundreds of cells and reach is the check that differs between them;
        /// a dozen covers a rectangle's corners and its middle without walking
        /// the whole work list per colonist.
        /// </summary>
        public const int SampleTargets = 12;

        /// <summary>One colonist's verdict on a piece of work.</summary>
        public sealed class Verdict
        {
            public Pawn Pawn;
            /// <summary>The check that refuses this colonist, null when they will take the job.</summary>
            public string Refusal;
            public int Skill;
            public string SkillLabel;
        }

        /// <summary>
        /// The work giver that hands out a designation's work, the same
        /// instance the work loop uses (its def, work type and capacities come
        /// from the def). Null for a designation this class does not cover.
        /// </summary>
        public static WorkGiver_Scanner ScannerFor(string designationDefName)
        {
            string giverDefName;
            switch (designationDefName)
            {
                case "CutPlant":
                case "HarvestPlant":
                    giverDefName = "PlantsCut";
                    break;
                case "Mine":
                case "MineVein":
                    giverDefName = "Mine";
                    break;
                case "Deconstruct":
                    giverDefName = "Deconstruct";
                    break;
                case "Hunt":
                    giverDefName = "HunterHunt";
                    break;
                default:
                    return null;
            }
            WorkGiverDef def = DefDatabase<WorkGiverDef>.GetNamedSilentFail(giverDefName);
            return def?.Worker as WorkGiver_Scanner;
        }

        /// <summary>
        /// One verdict per free colonist, the most skilled first, so a caller
        /// can print the best candidate first.
        /// </summary>
        public static List<Verdict> Walk(Map map, WorkGiver_Scanner giver, IList<LocalTargetInfo> targets, bool forced = false)
        {
            List<Verdict> verdicts = new List<Verdict>();
            if (map == null || giver?.def == null || targets == null || targets.Count == 0)
                return verdicts;

            SkillDef skill = FirstSkill(giver.def.workType);
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned
                .OrderByDescending(p => SkillLevel(p, skill))
                .ThenBy(p => p.LabelShort)
                .ToList();

            foreach (Pawn pawn in colonists)
            {
                Verdict verdict = new Verdict
                {
                    Pawn = pawn,
                    Skill = SkillLevel(pawn, skill),
                    SkillLabel = skill?.LabelCap ?? string.Empty,
                };
                verdict.Refusal = CommonRefusal(pawn, giver);
                if (verdict.Refusal == null && !AcceptsAny(pawn, giver, targets, forced))
                {
                    verdict.Refusal = TargetRefusal(pawn, giver, targets);
                }
                verdicts.Add(verdict);
            }
            return verdicts;
        }

        /// <summary>
        /// The first check that keeps a colonist off the work type altogether,
        /// or null. These are the game's own tests, in the order the work loop
        /// applies them.
        /// </summary>
        public static string CommonRefusal(Pawn pawn, WorkGiver_Scanner giver)
        {
            bool workTypeActive = pawn.workSettings != null
                && giver.def.workType != null
                && pawn.workSettings.WorkIsActive(giver.def.workType);
            return WorkAcceptanceRules.Refusal(new WorkAcceptanceRules.State
            {
                Downed = pawn.Downed,
                MentalState = pawn.InMentalState,
                Drafted = pawn.Drafted,
                HasWorkSettings = pawn.workSettings != null,
                WorkTypeActive = workTypeActive,
                WorkTagsAllowed = giver.def.workTags == WorkTags.None || !pawn.WorkTagIsDisabled(giver.def.workTags),
                MissingCapacity = giver.MissingRequiredCapacity(pawn)?.label,
                WorkTypeName = giver.def.workType?.defName ?? "this",
            });
        }

        /// <summary>
        /// Why the work giver builds no job for a colonist the common checks
        /// let through: reach first, then the giver's own rule in its terms.
        /// </summary>
        public static string TargetRefusal(Pawn pawn, WorkGiver_Scanner giver, IList<LocalTargetInfo> targets)
        {
            LocalTargetInfo target = targets[0];
            if (!pawn.CanReach(target, giver.PathEndMode, giver.MaxPathDanger(pawn)))
                return "cannot reach it";
            return SpecificRefusal(pawn, giver, target);
        }

        private static bool AcceptsAny(Pawn pawn, WorkGiver_Scanner giver, IList<LocalTargetInfo> targets, bool forced)
        {
            int tried = 0;
            foreach (LocalTargetInfo target in targets)
            {
                if (tried++ >= SampleTargets) break;
                // HasJobOnThing is the work loop's own question, and some
                // givers override only that one: HunterHunt has no
                // JobOnThing at all, so asking for a job would call every
                // hunter unable to hunt.
                bool accepted = target.HasThing
                    ? giver.HasJobOnThing(pawn, target.Thing, forced)
                    : giver.HasJobOnCell(pawn, target.Cell, forced);
                if (accepted) return true;
            }
            return false;
        }

        // SpecificRefusal names the giver's own rule that turned the target
        // down, as far as the game states it without a fail reason. A case not
        // named here still gets the work type, never a claim that the colonist
        // is free to take it.
        private static string SpecificRefusal(Pawn pawn, WorkGiver_Scanner giver, LocalTargetInfo target)
        {
            if (giver is WorkGiver_PlantsCut)
            {
                Plant plant = target.Thing as Plant;
                if (plant != null)
                {
                    if (plant.IsBurning()) return "it is burning";
                    if (!PlantUtility.PawnWillingToCutPlant_Job(plant, pawn))
                        return "his beliefs forbid cutting it";
                }
                return "the game's own plant-cutting check refuses this one for him";
            }
            if (giver is WorkGiver_HunterHunt)
            {
                if (!WorkGiver_HunterHunt.HasHuntingWeapon(pawn)) return "he carries no hunting weapon";
                if (pawn.Ideo != null && target.Thing is Pawn animal && pawn.Ideo.IsVeneratedAnimal(animal))
                    return "his beliefs forbid hunting this animal";
                return "he finds no shooting position on it";
            }
            if (giver is WorkGiver_Deconstruct)
            {
                Building building = target.Thing as Building;
                if (building != null && !building.DeconstructibleBy(pawn.Faction))
                    return "it is not his to take apart";
                return "the game's own deconstruct check refuses it for him";
            }
            if (giver is WorkGiver_Miner)
                return "the game's own mining check refuses it for him";
            return "the game's own " + giver.def.workType.labelShort + " check refuses it for him";
        }

        private static SkillDef FirstSkill(WorkTypeDef workType)
        {
            if (workType?.relevantSkills == null || workType.relevantSkills.Count == 0) return null;
            return workType.relevantSkills[0];
        }

        private static int SkillLevel(Pawn pawn, SkillDef skill)
        {
            if (skill == null || pawn.skills == null) return 0;
            return pawn.skills.GetSkill(skill)?.Level ?? 0;
        }
    }
}
