using System.Collections.Generic;
using RIMAPI.Helpers;
using RimWorld;
using Verse;

namespace RIMAPI.Services
{
    /// <summary>
    /// The colony fingerprint behind `debug_action` (ticket #3922): read the
    /// state before and after an entry runs, so the answer says whether the
    /// entry built anything instead of claiming "ran X" for a silent no-op.
    /// The comparison itself is game-free (DevActionRules.Difference).
    /// </summary>
    public partial class DevActionService
    {
        /// <summary>
        /// Reads the colony as it stands now. Cheap enough to call twice per
        /// action (a few thousand things), and deliberately blind to what a
        /// running game changes on its own: a pawn's position and current job.
        /// </summary>
        private static DevActionRules.ColonyStamp Stamp()
        {
            DevActionRules.ColonyStamp stamp = new DevActionRules.ColonyStamp();
            if (Find.LetterStack?.LettersListForReading != null)
            {
                stamp.Letters = Find.LetterStack.LettersListForReading.Count;
            }
            if (Find.Selector != null) stamp.Selected = Find.Selector.NumSelected;
            if (Find.WindowStack?.Windows != null) stamp.Windows = Find.WindowStack.Windows.Count;

            List<Map> maps = Find.Maps;
            if (maps == null) return stamp;
            stamp.Maps = maps.Count;
            int pawnDigest = 17;
            int thingDigest = 17;
            foreach (Map map in maps)
            {
                if (map == null) continue;
                if (map.designationManager?.AllDesignations != null)
                {
                    stamp.Designations += map.designationManager.AllDesignations.Count;
                }
                if (map.zoneManager?.AllZones != null)
                {
                    stamp.Zones += map.zoneManager.AllZones.Count;
                }
                if (map.weatherManager?.curWeather != null)
                {
                    stamp.Weather = unchecked(stamp.Weather * 31 + Hash(map.weatherManager.curWeather.defName));
                }
                List<Thing> things = map.listerThings?.AllThings;
                if (things == null) continue;
                stamp.Things += things.Count;
                foreach (Thing thing in things)
                {
                    if (thing == null) continue;
                    thingDigest = ThingDigest(thingDigest, thing);
                    if (thing is IBillGiver giver && giver.BillStack != null)
                    {
                        stamp.Bills += giver.BillStack.Count;
                    }
                    if (!(thing is Pawn pawn)) continue;
                    stamp.Pawns++;
                    pawnDigest = PawnDigest(pawnDigest, pawn);
                }
            }
            stamp.PawnDigest = pawnDigest;
            stamp.ThingDigest = thingDigest;

            ResearchManager research = Find.ResearchManager;
            ResearchProjectDef project = research?.GetProject();
            if (project != null)
            {
                stamp.Research = unchecked(
                    Hash(project.defName) * 31 + (int)(research.GetProgress(project) * 1000f)
                );
            }
            return stamp;
        }

        /// <summary>
        /// One thing's own state: its id, stack count and hit points. A defName
        /// never changes on a spawned thing, so it stays out.
        /// </summary>
        private static int ThingDigest(int hash, Thing thing)
        {
            unchecked
            {
                hash = hash * 31 + thing.thingIDNumber;
                hash = hash * 31 + thing.stackCount;
                hash = hash * 31 + thing.HitPoints;
            }
            return hash;
        }

        /// <summary>
        /// One pawn's own state: what a dev entry changes when it makes a
        /// prisoner, wounds, infects, calms or re-skills him. Position and
        /// current job stay out; a running game moves those by itself.
        /// </summary>
        private static int PawnDigest(int hash, Pawn pawn)
        {
            unchecked
            {
                hash = hash * 31 + pawn.thingIDNumber;
                hash = hash * 31 + (pawn.Dead ? 1 : 0) + (pawn.Downed ? 2 : 0) + (pawn.Drafted ? 4 : 0);
                hash = hash * 31 + (pawn.IsPrisoner ? 8 : 0) + (pawn.IsSlave ? 16 : 0);
                hash = hash * 31 + Hash(pawn.Faction?.def?.defName);
                hash = hash * 31 + Hash(pawn.HostFaction?.def?.defName);
                hash = hash * 31 + Hash(pawn.MentalStateDef?.defName);
                List<Hediff> hediffs = pawn.health?.hediffSet?.hediffs;
                if (hediffs != null)
                {
                    hash = hash * 31 + hediffs.Count;
                    foreach (Hediff hediff in hediffs)
                    {
                        hash = hash * 31 + Hash(hediff.def?.defName);
                        hash = hash * 31 + (int)(hediff.Severity * 100f);
                    }
                }
                if (pawn.needs?.mood != null)
                {
                    hash = hash * 31 + (int)(pawn.needs.mood.CurLevelPercentage * 100f);
                }
                List<SkillRecord> skills = pawn.skills?.skills;
                if (skills != null)
                {
                    foreach (SkillRecord skill in skills)
                    {
                        hash = hash * 31 + skill.Level;
                    }
                }
            }
            return hash;
        }

        /// <summary>A string's hash, with null as zero so an absent faction stays stable.</summary>
        private static int Hash(string value)
        {
            return value == null ? 0 : value.GetHashCode();
        }
    }
}
