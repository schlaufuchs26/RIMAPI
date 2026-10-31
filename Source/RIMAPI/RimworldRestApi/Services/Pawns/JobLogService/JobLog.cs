using System;
using System.Collections.Generic;
using RIMAPI.Helpers;
using RimWorld;
using Verse;
using Verse.AI;

namespace RIMAPI.Services
{
    // What each colonist did, with the tick it started: a wait report that only
    // says what changed in the colony never says whether the orders took
    // effect, or whether someone spent six hours with nothing to do (ticket
    // #3300). The record is a list of job changes, not spans; the tool folds
    // consecutive entries of one job into a duration, so this side only has to
    // be right about when something changed.
    //
    // An entry is written when the pawn's job (its def and the thing it works
    // on) differs from the one recorded before, so a job that is re-started
    // without changing anything costs no line. A pawn with no job at all is an
    // entry of its own ("idle"), which is the state a run needs to see.
    public static class JobLog
    {
        public class Entry
        {
            public int Tick;
            public int PawnId;
            public string Pawn;
            // Job is the def's defName, empty when the pawn has no job at all.
            public string Job;
            // Label is the def's own label ("harvest"); "idle" without a job.
            public string Label;
            // Target is the thing the job works on, empty when it works on a
            // cell or on nothing.
            public string Target;
            // Idle is the game's own answer to "does this pawn have something
            // to do" (Pawn_MindState.IsIdle, the state the colonists-idle alert
            // shows). The tool used to guess it from job names, so a new
            // wander job in a game update counted as work (ticket #3392).
            public bool Idle;
        }

        // KeepTicks bounds the record to two in-game days, which is longer than
        // any wait asks for, and MaxEntries keeps a long-running colony from
        // growing it without bound.
        private const int KeepTicks = 2 * 60000;
        private const int MaxEntries = 20000;

        private static readonly object Gate = new object();
        private static readonly List<Entry> Entries = new List<Entry>();
        // Current is the entry each pawn is in right now, so a repeated hook
        // (StartJob and EndCurrentJob both fire on one change) writes nothing.
        private static readonly Dictionary<int, Entry> Current = new Dictionary<int, Entry>();

        // Record looks at one pawn and appends an entry when its job changed.
        // Both hooks call it after the game has set the new state, so it reads
        // the pawn instead of taking the job as an argument.
        public static void Record(Pawn pawn)
        {
            if (pawn == null || pawn.jobs == null || pawn.Dead || pawn.Destroyed)
                return;
            if (!pawn.IsColonistPlayerControlled)
                return;
            int now = Find.TickManager?.TicksGame ?? 0;
            string job = "";
            string label = "idle";
            string target = "";
            Job cur = pawn.jobs.curJob;
            if (cur != null && cur.def != null)
            {
                job = cur.def.defName ?? "";
                label = cur.def.label ?? job;
                target = TargetLabel(cur);
            }
            bool idle = IdleState(pawn);
            int id = pawn.thingIDNumber;
            lock (Gate)
            {
                Entry last;
                if (Current.TryGetValue(id, out last)
                    && JobLogState.Same(last.Job, last.Target, last.Idle, job, target, idle))
                    return;
                var entry = new Entry
                {
                    Tick = now,
                    PawnId = id,
                    Pawn = pawn.LabelShort ?? pawn.Name?.ToStringShort ?? "?",
                    Job = job,
                    Label = label,
                    Target = target,
                    Idle = idle,
                };
                Entries.Add(entry);
                Current[id] = entry;
                Prune(now);
            }
        }

        // IdleState is the game's own answer, not a list of job names: the
        // mind state's idle flag is what the "colonists idle" alert shows, and
        // it covers whatever job the game hands an idle colonist (wandering,
        // joy, waiting), including one a game update adds.
        private static bool IdleState(Pawn pawn)
        {
            try
            {
                return pawn.mindState != null && pawn.mindState.IsIdle;
            }
            catch (Exception)
            {
                // A pawn whose mind state is not initialized yet (a spawn in
                // progress) is not known to be idle; the job def still counts.
                return false;
            }
        }

        // A terminal entry ends a pawn's record: he died or left the map, so
        // the job he was in stops there instead of running to the end of a
        // wait. Without it the record, which only changes on a job change,
        // would claim work until the span ended (ticket #3311).
        public const string Dead = "dead";
        public const string Gone = "gone";

        public static void RecordDeath(Pawn pawn)
        {
            Write(pawn, Dead, "died");
        }

        // A pawn who dies is despawned too, and the death is the entry a
        // reader wants, so the despawn that follows a kill writes nothing.
        public static void RecordLeave(Pawn pawn)
        {
            if (pawn != null && pawn.Dead)
                return;
            Write(pawn, Gone, "left the map");
        }

        private static void Write(Pawn pawn, string job, string label)
        {
            if (pawn == null)
                return;
            int id = pawn.thingIDNumber;
            int now = Find.TickManager?.TicksGame ?? 0;
            lock (Gate)
            {
                Entry last;
                bool tracked = Current.TryGetValue(id, out last);
                // A pawn the record never saw is only worth an exit when he is
                // a colonist; anyone else is not part of the report anyway.
                if (!tracked && !pawn.IsColonistPlayerControlled)
                    return;
                if (tracked && last.Job == job)
                    return;
                var entry = new Entry
                {
                    Tick = now,
                    PawnId = id,
                    Pawn = pawn.LabelShort ?? pawn.Name?.ToStringShort ?? "?",
                    Job = job,
                    Label = label,
                    Target = "",
                };
                Entries.Add(entry);
                Current[id] = entry;
                Prune(now);
            }
        }

        // Since answers the record from one tick on, plus the entry each pawn
        // was in at that tick: without it the first span of the answer has no
        // start, and a pawn who began a job before the wait would look idle.
        public static List<Entry> Since(int sinceTick)
        {
            lock (Gate)
            {
                var prior = new Dictionary<int, Entry>();
                var outList = new List<Entry>();
                foreach (var e in Entries)
                {
                    if (e.Tick < sinceTick)
                    {
                        prior[e.PawnId] = e;
                        continue;
                    }
                    if (prior.Count > 0)
                    {
                        foreach (var p in prior.Values) outList.Add(p);
                        prior.Clear();
                    }
                    outList.Add(e);
                }
                foreach (var p in prior.Values) outList.Add(p);
                return outList;
            }
        }

        // Forget drops the record, for a new game or a reloaded save whose pawn
        // ids belong to a colony that is gone.
        public static void Forget()
        {
            lock (Gate)
            {
                Entries.Clear();
                Current.Clear();
            }
        }

        private static void Prune(int now)
        {
            int drop = 0;
            while (drop < Entries.Count && Entries[drop].Tick < now - KeepTicks)
                drop++;
            if (Entries.Count - drop > MaxEntries)
                drop = Entries.Count - MaxEntries;
            if (drop > 0)
                Entries.RemoveRange(0, drop);
        }

        // TargetLabel names what a job works on. The game keeps up to three
        // targets per job; a cell target says nothing a reader wants, so only a
        // thing's label is reported.
        private static string TargetLabel(Job job)
        {
            LocalTargetInfo t = job.targetA;
            if (!t.IsValid) t = job.targetB;
            if (!t.IsValid) t = job.targetC;
            if (!t.IsValid) return "";
            try
            {
                if (t.HasThing && t.Thing != null)
                    return t.Thing.LabelShort ?? "";
            }
            catch (Exception)
            {
                // A destroyed or uninitialized thing has no label; the job
                // still belongs in the record.
            }
            return "";
        }
    }
}
