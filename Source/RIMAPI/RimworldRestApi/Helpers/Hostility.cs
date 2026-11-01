using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// One rule for "this pawn is an enemy of the colony", shared by the
    /// combat read and the cheap headcount (fox ticket #3451). The headcount
    /// counted humanlikes only, so a manhunter warg raid reached the tool as
    /// "0 hostiles": the digest never named it and a play run only found out
    /// when a colonist was already bleeding.
    ///
    /// The rule is the combat read's: an enemy faction, or an animal the game
    /// has turned manhunter. Mechanoids and hostile insects are covered by the
    /// faction test, which the humanlike filter also used to drop.
    ///
    /// Since fox #3630 it is not only the side that counts but what the pawn is
    /// doing: a wild man has no faction, so DiLeo (id 54348) beating Veniamin
    /// from 100% to 40% health read as "0 hostiles" and the wait ran its full
    /// hour. An attack job on a colony target, an aggro mental state and an
    /// assaulting lord make a pawn a threat as well. The decision itself lives
    /// in HostilityRules, which is free of game types and unit-tested.
    /// </summary>
    public static class Hostility
    {
        public static bool IsHostileToColony(Pawn pawn)
        {
            if (pawn == null) return false;
            return HostilityRules.ThreatensColony(StateOf(pawn));
        }

        public static bool IsManhunter(Pawn pawn)
        {
            if (pawn == null || pawn.RaceProps == null || !pawn.RaceProps.Animal) return false;
            MentalStateDef state = pawn.MentalStateDef;
            return state == MentalStateDefOf.Manhunter || state == MentalStateDefOf.ManhunterPermanent;
        }

        /// <summary>
        /// The pawn as the rule reads it: its side, its mental state, the job it
        /// is in and whether that job acts on the colony.
        /// </summary>
        public static HostilityRules.PawnState StateOf(Pawn pawn)
        {
            HostilityRules.PawnState state = new HostilityRules.PawnState
            {
                Colonist = pawn.IsColonist,
                Animal = pawn.RaceProps != null && pawn.RaceProps.Animal,
                WildMan = pawn.IsWildMan(),
                Prisoner = pawn.IsPrisonerOfColony,
                Slave = pawn.IsSlaveOfColony,
                Guest = pawn.HostFaction == Faction.OfPlayer,
                HasFaction = pawn.Faction != null,
                EnemyFaction = pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer),
                MentalState = pawn.MentalStateDef != null ? pawn.MentalStateDef.defName : "",
                JobDef = pawn.CurJobDef != null ? pawn.CurJobDef.defName : "",
                LordJob = LordJobNameOf(pawn),
            };
            state.JobTargetsColony = JobTargetsColony(pawn.CurJob);
            return state;
        }

        /// <summary>The side the tool prints for this pawn; see HostilityRules.KindOf.</summary>
        public static string KindOf(Pawn pawn)
        {
            if (pawn == null) return "";
            return HostilityRules.KindOf(StateOf(pawn));
        }

        /// <summary>
        /// The colony pawn the hostile's current job acts on, or null (fox
        /// ticket #3690). A digest line that names the victim tells a raid from
        /// a wild man at the barracks door without a second read.
        /// </summary>
        public static Pawn AttackedColonist(Pawn pawn)
        {
            if (pawn == null || pawn.CurJob == null) return null;
            Pawn victim = pawn.CurJob.targetA.Thing as Pawn;
            return victim != null && victim.IsColonist ? victim : null;
        }

        /// <summary>
        /// True when one of the job's targets belongs to the colony: the pawn
        /// being beaten (Veniamin), a colony animal or a colony building. The
        /// game puts the victim in targetA, but a job may carry the second half
        /// in targetB, so both are read.
        /// </summary>
        private static bool JobTargetsColony(Job job)
        {
            if (job == null) return false;
            return IsColonyThing(job.targetA.Thing) || IsColonyThing(job.targetB.Thing);
        }

        private static bool IsColonyThing(Thing thing)
        {
            if (thing == null) return false;
            if (thing.Faction == Faction.OfPlayer) return true;
            Pawn pawn = thing as Pawn;
            return pawn != null && pawn.IsColonist;
        }

        private static string LordJobNameOf(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            return lord != null && lord.LordJob != null ? lord.LordJob.GetType().Name : "";
        }
    }
}
