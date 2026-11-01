using System;
using System.Collections.Generic;
using System.Globalization;

namespace RIMAPI.Helpers
{
    // One job change in the record (ticket #3300): what a colonist started
    // doing, when, and the state the report needs to tell work from idling.
    // Plain data, because the save carries it and the unit tests build one
    // (ticket #3614).
    public class JobLogEntry
    {
        public int Tick;
        public int PawnId;
        // Pawn is the label the report prints, so a reloaded record still
        // names the pawn it was written for.
        public string Pawn;
        // Job is the def's defName, empty when the pawn has no job at all.
        public string Job;
        // Label is the def's own label ("harvest"); "idle" without a job.
        public string Label;
        // Target is the thing the job works on, empty when it works on a cell
        // or on nothing.
        public string Target;
        // Idle is the game's own answer to "does this pawn have something to
        // do" (Pawn_MindState.IsIdle, the state the colonists-idle alert
        // shows), not a list of job names (ticket #3392).
        public bool Idle;

        // The save format (ticket #3614): one line per entry, seven fields
        // separated by a pipe. A pipe or a backslash inside a label is
        // escaped, because labels come from the game and from mods. The format
        // is deliberately tolerant on read: a line this build cannot parse is
        // dropped by the record instead of failing the load.
        public string ToLine()
        {
            return string.Join("|", new[]
            {
                Tick.ToString(CultureInfo.InvariantCulture),
                PawnId.ToString(CultureInfo.InvariantCulture),
                Escape(Pawn),
                Escape(Job),
                Escape(Label),
                Escape(Target),
                Idle ? "1" : "0",
            });
        }

        public static bool TryParse(string line, out JobLogEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(line))
                return false;
            string[] fields = Split(line);
            if (fields.Length != 7)
                return false;
            int tick;
            int pawnId;
            if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out tick)
                || !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out pawnId))
                return false;
            if (fields[6] != "0" && fields[6] != "1")
                return false;
            entry = new JobLogEntry
            {
                Tick = tick,
                PawnId = pawnId,
                Pawn = fields[2],
                Job = fields[3],
                Label = fields[4],
                Target = fields[5],
                Idle = fields[6] == "1",
            };
            return true;
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("|", "\\|");
        }

        // Split cuts the line at every pipe that is not escaped, then unescapes
        // each field.
        private static string[] Split(string line)
        {
            var fields = new List<string>();
            var field = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\\' && i + 1 < line.Length)
                {
                    char next = line[i + 1];
                    // Only the two characters the writer escapes are unescaped;
                    // any other backslash is a literal character of the label,
                    // so a line from another build still reads as written.
                    if (next == '\\' || next == '|')
                    {
                        field.Append(next);
                        i++;
                        continue;
                    }
                }
                if (c == '|')
                {
                    fields.Add(field.ToString());
                    field.Length = 0;
                    continue;
                }
                field.Append(c);
            }
            fields.Add(field.ToString());
            return fields.ToArray();
        }
    }

    // The job record itself: the list of job changes plus the state each pawn
    // is in right now, so a repeated hook writes nothing (ticket #3300).
    // Game-free, so the unit tests pin the change rule and the restore after a
    // load; JobLog reads the pawns and owns the save side (ticket #3614).
    // Not thread-safe: JobLog holds the lock around every call.
    public class JobLogRecord
    {
        // KeepTicks bounds the record to two in-game days, which is longer than
        // any wait asks for, and MaxEntries keeps a long-running colony from
        // growing it without bound.
        public const int KeepTicks = 2 * 60000;
        public const int MaxEntries = 20000;

        private readonly List<JobLogEntry> entries = new List<JobLogEntry>();
        // current is the entry each pawn is in right now, so a repeated hook
        // (StartJob and EndCurrentJob both fire on one change) writes nothing.
        private readonly Dictionary<int, JobLogEntry> current = new Dictionary<int, JobLogEntry>();

        public int Count => entries.Count;

        public JobLogEntry At(int index) => entries[index];

        // Append writes the entry unless it repeats the state already recorded
        // for that pawn, and prunes the tail. Returns whether a line was
        // written.
        public bool Append(JobLogEntry entry, int now)
        {
            if (entry == null)
                return false;
            JobLogEntry last;
            if (current.TryGetValue(entry.PawnId, out last)
                && JobLogState.Same(last.Job, last.Target, last.Idle, entry.Job, entry.Target, entry.Idle))
                return false;
            entries.Add(entry);
            current[entry.PawnId] = entry;
            Prune(now);
            return true;
        }

        public bool Tracks(int pawnId)
        {
            return current.ContainsKey(pawnId);
        }

        // Last is the entry a pawn is in right now, or null when the record
        // never saw him.
        public JobLogEntry Last(int pawnId)
        {
            JobLogEntry last;
            return current.TryGetValue(pawnId, out last) ? last : null;
        }

        // Since answers the record from one tick on, plus the entry each pawn
        // was in at that tick: without it the first span of the answer has no
        // start, and a pawn who began a job before the wait would look idle.
        public List<JobLogEntry> Since(int sinceTick)
        {
            var prior = new Dictionary<int, JobLogEntry>();
            var outList = new List<JobLogEntry>();
            foreach (var e in entries)
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

        public void Clear()
        {
            entries.Clear();
            current.Clear();
        }

        // AfterLoad keeps what the loaded world has already lived and drops the
        // rest: a load rewinds the game clock, so an entry with a later tick was
        // written in the timeline the load threw away. A fresh colony starts at
        // tick 0, where that rule empties the record on its own. The state each
        // pawn is in comes back from the tail of what is left, which is what
        // lets a report that straddles the load start with the job each pawn
        // was in (ticket #3614).
        public void AfterLoad(int now)
        {
            entries.RemoveAll(e => e.Tick > now);
            RebuildCurrent();
        }

        public List<string> SaveLines()
        {
            var lines = new List<string>(entries.Count);
            foreach (var e in entries)
                lines.Add(e.ToLine());
            return lines;
        }

        // LoadLines replaces the record with what the save carried. A line this
        // build cannot read is dropped and counted, never fatal: the record
        // sits in the save, so a save written by another build has to load.
        public int LoadLines(List<string> lines)
        {
            entries.Clear();
            current.Clear();
            int dropped = 0;
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    JobLogEntry entry;
                    if (JobLogEntry.TryParse(line, out entry))
                        entries.Add(entry);
                    else
                        dropped++;
                }
            }
            RebuildCurrent();
            return dropped;
        }

        // RebuildCurrent reads the state each pawn is in from the tail: the
        // last entry per pawn is exactly the state the record compared against
        // before the save.
        private void RebuildCurrent()
        {
            current.Clear();
            foreach (var e in entries)
                current[e.PawnId] = e;
        }

        private void Prune(int now)
        {
            int drop = 0;
            while (drop < entries.Count && entries[drop].Tick < now - KeepTicks)
                drop++;
            if (entries.Count - drop > MaxEntries)
                drop = entries.Count - MaxEntries;
            if (drop > 0)
                entries.RemoveRange(0, drop);
        }
    }
}
