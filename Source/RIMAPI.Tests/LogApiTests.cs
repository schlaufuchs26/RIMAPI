using System;
using RIMAPI.Core;
using Verse;
using Xunit;

namespace RIMAPI.Tests
{
    /// <summary>
    /// The logging gate of ticket #3205: the chatty levels stay quiet unless the
    /// mod setting turns them on, and a failure is written in every case.
    /// </summary>
    public class LogApiTests : IDisposable
    {
        // LogApi holds its settings in statics, so each test starts from the
        // shipped defaults (quiet, INFO) and leaves them behind again; the
        // assembly disables xunit's parallel runs for the same reason.
        public LogApiTests() => Reset();

        public void Dispose() => Reset();

        private static void Reset()
        {
            Log.Clear();
            LogApi.IsLogging = false;
            LogApi.LoggingLevel = LoggingLevels.INFO;
        }

        [Fact]
        public void Info_writes_nothing_while_logging_is_off()
        {
            LogApi.Info("hello");

            Assert.Empty(Log.Written);
        }

        [Fact]
        public void Warning_writes_nothing_while_logging_is_off()
        {
            LogApi.Warning("careful");

            Assert.Empty(Log.Written);
        }

        [Fact]
        public void Error_is_written_while_logging_is_off()
        {
            LogApi.Error("boom");

            LogEntry entry = Assert.Single(Log.Written);
            Assert.Equal("Error", entry.Method);
            Assert.Equal("[RIMAPI | ERROR] boom", entry.Text);
        }

        [Fact]
        public void Critical_is_written_while_logging_is_off()
        {
            LogApi.Message("dead", LoggingLevels.CRITICAL);

            LogEntry entry = Assert.Single(Log.Written);
            Assert.Equal("Error", entry.Method);
            Assert.Equal("[RIMAPI | CRITICAL] dead", entry.Text);
        }

        [Fact]
        public void Info_is_written_once_logging_is_on()
        {
            LogApi.IsLogging = true;

            LogApi.Info("hello");

            LogEntry entry = Assert.Single(Log.Written);
            Assert.Equal("Message", entry.Method);
            Assert.Equal("[RIMAPI | INFO] hello", entry.Text);
        }

        [Fact]
        public void Debug_is_written_only_when_the_level_allows_it()
        {
            LogApi.IsLogging = true;

            LogApi.Message("detail", LoggingLevels.DEBUG);
            Assert.Empty(Log.Written);

            LogApi.LoggingLevel = LoggingLevels.DEBUG;
            LogApi.Message("detail", LoggingLevels.DEBUG);

            LogEntry entry = Assert.Single(Log.Written);
            Assert.Equal("[RIMAPI | DEBUG] detail", entry.Text);
        }

        [Fact]
        public void Warning_goes_to_Log_Warning_with_its_own_prefix()
        {
            LogApi.IsLogging = true;
            LogApi.LoggingLevel = LoggingLevels.WARNING;

            LogApi.Warning("careful");

            LogEntry entry = Assert.Single(Log.Written);
            Assert.Equal("Warning", entry.Method);
            Assert.Equal("[RIMAPI | WARNING] careful", entry.Text);
        }

        [Fact]
        public void The_level_drops_info_but_keeps_warnings()
        {
            LogApi.IsLogging = true;
            LogApi.LoggingLevel = LoggingLevels.WARNING;

            LogApi.Info("chatter");
            LogApi.Warning("careful");

            LogEntry entry = Assert.Single(Log.Written);
            Assert.Equal("[RIMAPI | WARNING] careful", entry.Text);
        }

        [Fact]
        public void The_level_never_silences_an_error()
        {
            LogApi.IsLogging = true;
            LogApi.LoggingLevel = LoggingLevels.CRITICAL;

            LogApi.Error("boom");

            LogEntry entry = Assert.Single(Log.Written);
            Assert.Equal("[RIMAPI | ERROR] boom", entry.Text);
        }

        [Fact]
        public void The_object_overload_writes_an_exceptions_type_and_stack()
        {
            LogApi.Error(Caught("boom"));

            LogEntry entry = Assert.Single(Log.Written);
            Assert.Contains("System.InvalidOperationException", entry.Text);
            Assert.Contains("boom", entry.Text);
            // ToString() of an exception carries the stack trace; a message-only
            // write would leave a later session without the call site.
            Assert.Contains("at RIMAPI.Tests.LogApiTests", entry.Text);
        }

        [Fact]
        public void The_object_overload_names_a_null_argument()
        {
            LogApi.Error((object)null);

            LogEntry entry = Assert.Single(Log.Written);
            Assert.Equal("[RIMAPI | ERROR] null", entry.Text);
        }

        /// <summary>
        /// An exception carries its stack trace only once it was thrown, and the
        /// error sites log a caught one.
        /// </summary>
        private static Exception Caught(string message)
        {
            try
            {
                throw new InvalidOperationException(message);
            }
            catch (InvalidOperationException caught)
            {
                return caught;
            }
        }
    }
}
