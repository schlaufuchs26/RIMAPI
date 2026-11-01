using System;
using System.Collections.Generic;
using RIMAPI.Core;
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
    //
    // The record itself is JobLogRecord, which is game-free so the unit tests
    // pin it (ticket #3614); this class reads the pawns and carries the record
    // in the save.
    public static class JobLog
    {
        private static readonly object Gate = new object();
        private static readonly JobLogRecord Store = new JobLogRecord();

        // Record looks at one pawn and appends an entry when its job changed.
        // Both hooks call it after the game has set the new state, so it reads
        // the pawn instead of taking the job as an argument.
        public static void Record(Pawn pawn)
        {
            if (pawn == null || pawn.jobs == null || pawn.Dead || pawn.Destroyed)
                return;
            // Not IsColonistPlayerControlled: that flag is false for a colonist
            // in a mental state, so the record dropped him during a tantrum or
            // a berserk (ticket #3505).
            if (!JobLogState.Tracked(pawn.IsColonist, pawn.Faction == Faction.OfPlayer))
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
            var entry = new JobLogEntry
            {
                Tick = now,
                PawnId = pawn.thingIDNumber,
                Pawn = pawn.LabelShort ?? pawn.Name?.ToStringShort ?? "?",
                Job = job,
                Label = label,
                Target = target,
                Idle = IdleState(pawn),
            };
            lock (Gate)
            {
                Store.Append(entry, now);
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
                // A pawn the record never saw is only worth an exit when he is
                // a colonist; anyone else is not part of the report anyway.
                // Same rule as Record, so a colonist in a mental state gets his
                // exit line too (ticket #3505).
                bool tracked = Store.Tracks(id);
                if (!tracked && !JobLogState.Tracked(pawn.IsColonist, pawn.Faction == Faction.OfPlayer))
                    return;
                JobLogEntry last = Store.Last(id);
                if (last != null && last.Job == job)
                    return;
                Store.Append(new JobLogEntry
                {
                    Tick = now,
                    PawnId = id,
                    Pawn = pawn.LabelShort ?? pawn.Name?.ToStringShort ?? "?",
                    Job = job,
                    Label = label,
                    Target = "",
                }, now);
            }
        }

        // Since answers the record from one tick on, plus the entry each pawn
        // was in at that tick: without it the first span of the answer has no
        // start, and a pawn who began a job before the wait would look idle.
        public static List<JobLogEntry> Since(int sinceTick)
        {
            lock (Gate)
            {
                return Store.Since(sinceTick);
            }
        }

        // AfterLoad runs when a game finished loading. It keeps what the loaded
        // world has already lived and drops the rest, and it rebuilds the state
        // each pawn is in, so a report that straddles the reload still starts
        // with the job each pawn was in (ticket #3614).
        public static void AfterLoad()
        {
            int now = Find.TickManager?.TicksGame ?? 0;
            lock (Gate)
            {
                Store.AfterLoad(now);
            }
        }

        // Forget drops the record, for a new colony whose pawns share nothing
        // with the one before it (ticket #3614).
        public static void Forget()
        {
            lock (Gate)
            {
                Store.Clear();
            }
        }

        // ExposeData carries the record in the save, so a reload brings it back
        // instead of starting empty (ticket #3614). RIMAPI_GameComponent calls
        // it; the format is one line per entry (JobLogEntry.ToLine), and a line
        // this build cannot read is dropped rather than failing the load.
        public static void ExposeData()
        {
            if (Scribe.mode != LoadSaveMode.Saving && Scribe.mode != LoadSaveMode.LoadingVars)
                return;
            List<string> lines = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                lock (Gate)
                {
                    lines = Store.SaveLines();
                }
            }
            Scribe_Collections.Look(ref lines, "jobLog", LookMode.Value);
            if (Scribe.mode != LoadSaveMode.LoadingVars)
                return;
            lock (Gate)
            {
                int dropped = Store.LoadLines(lines);
                if (dropped > 0)
                    LogApi.Warning($"[RimworldRestApi] job record: dropped {dropped} line(s) of a save this build cannot read");
            }
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
