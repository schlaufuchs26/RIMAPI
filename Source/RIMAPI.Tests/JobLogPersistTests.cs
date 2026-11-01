using System.Collections.Generic;
using RIMAPI.Helpers;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The job record survives a save and a load (ticket #3614). Before this it
    /// lived in memory only, so a reload wiped it and an activity report that
    /// straddled the reload started empty. These tests pin the two halves that
    /// are game-free: what a load keeps, and the save format.
    /// </summary>
    public class JobLogPersistTests
    {
        private static JobLogEntry Entry(int tick, int pawnId, string job, string target = "", bool idle = false)
        {
            return new JobLogEntry
            {
                Tick = tick,
                PawnId = pawnId,
                Pawn = "Lane",
                Job = job,
                Label = job,
                Target = target,
                Idle = idle,
            };
        }

        [Fact]
        public void ALoadDropsTheTimelineItThrewAway()
        {
            // A load rewinds the game clock: an entry with a later tick was
            // written after the save the game went back to, so it never
            // happened in the world now running.
            var record = new JobLogRecord();
            record.Append(Entry(900, 1, "HaulToCell", "steel"), 900);
            record.Append(Entry(1100, 1, "DoBill", "stove"), 1100);

            record.AfterLoad(1000);

            Assert.Equal(1, record.Count);
            Assert.Equal("HaulToCell", record.At(0).Job);
        }

        [Fact]
        public void ANewGameStartsWithAnEmptyRecord()
        {
            // A fresh colony starts at tick 0; every entry of the colony before
            // it lies in the future of that world.
            var record = new JobLogRecord();
            record.Append(Entry(40000, 7, "DoBill", "stove"), 40000);

            record.AfterLoad(0);

            Assert.Equal(0, record.Count);
            Assert.False(record.Tracks(7));
        }

        [Fact]
        public void TheJobAPawnWasInComesBackAfterALoad()
        {
            // The report needs to know what each pawn was doing at the load
            // moment, or its first span starts with nobody working.
            var record = new JobLogRecord();
            record.Append(Entry(500, 3, "HaulToCell", "steel"), 500);
            record.Append(Entry(900, 3, "DoBill", "stove"), 900);

            record.AfterLoad(1000);

            Assert.True(record.Tracks(3));
            Assert.Equal("DoBill", record.Last(3).Job);
            // And the rebuilt state still suppresses a repeat of that job.
            Assert.False(record.Append(Entry(1010, 3, "DoBill", "stove"), 1010));
            Assert.True(record.Append(Entry(1020, 3, "GotoWander"), 1020));
        }

        [Fact]
        public void TheReportCoversTheSpanAcrossTheLoad()
        {
            // A span that starts before the load has to carry the state each
            // pawn was in at its start, not only the changes inside the span.
            var record = new JobLogRecord();
            record.Append(Entry(100, 1, "HaulToCell", "steel"), 100);
            record.Append(Entry(900, 1, "DoBill", "stove"), 900);

            var span = record.Since(500);

            Assert.Equal(2, span.Count);
            Assert.Equal(100, span[0].Tick);
            Assert.Equal(900, span[1].Tick);
        }

        [Fact]
        public void AnEntrySurvivesTheSaveFormat()
        {
            // Labels come from the game and mods, so a separator inside one
            // must not split the line.
            var entry = Entry(123456, 42, "HaulToCell", "steel | pipe \\ backslash", true);

            JobLogEntry back;
            Assert.True(JobLogEntry.TryParse(entry.ToLine(), out back));

            Assert.Equal(entry.Tick, back.Tick);
            Assert.Equal(entry.PawnId, back.PawnId);
            Assert.Equal(entry.Pawn, back.Pawn);
            Assert.Equal(entry.Job, back.Job);
            Assert.Equal("steel | pipe \\ backslash", back.Target);
            Assert.True(back.Idle);
        }

        [Fact]
        public void ALineAnotherBuildWroteIsDroppedNotFatal()
        {
            // The record lives in the save, so a save written by another build
            // must still load: an unreadable line is skipped, the rest stays.
            var record = new JobLogRecord();
            var lines = new List<string> { "not an entry", Entry(10, 2, "DoBill").ToLine() };

            int dropped = record.LoadLines(lines);

            Assert.Equal(1, dropped);
            Assert.Equal(1, record.Count);
            Assert.Equal("DoBill", record.At(0).Job);
        }

        [Fact]
        public void LoadingASaveReplacesTheRecordItFound()
        {
            // The record of the world before the load belongs to nobody, even
            // when its ticks would still fit the loaded clock.
            var record = new JobLogRecord();
            record.Append(Entry(100, 1, "HaulToCell", "steel"), 100);

            record.LoadLines(new List<string> { Entry(80, 5, "DoBill", "stove").ToLine() });

            Assert.Equal(1, record.Count);
            Assert.Equal(5, record.At(0).PawnId);
            Assert.False(record.Tracks(1));
        }

        [Fact]
        public void AnEmptySaveNodeLeavesNoRecord()
        {
            // A save written before the record was persisted carries no lines;
            // the record of the colony before it must not leak through.
            var record = new JobLogRecord();
            record.Append(Entry(100, 1, "HaulToCell", "steel"), 100);

            record.LoadLines(null);

            Assert.Equal(0, record.Count);
        }

        [Fact]
        public void TheSavedLinesAreTheRecordInOrder()
        {
            var record = new JobLogRecord();
            record.Append(Entry(100, 1, "HaulToCell", "steel"), 100);
            record.Append(Entry(200, 2, "DoBill", "stove"), 200);

            var lines = record.SaveLines();

            Assert.Equal(2, lines.Count);
            JobLogEntry first, second;
            Assert.True(JobLogEntry.TryParse(lines[0], out first));
            Assert.True(JobLogEntry.TryParse(lines[1], out second));
            Assert.Equal(100, first.Tick);
            Assert.Equal(200, second.Tick);
        }
    }
}
