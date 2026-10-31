using Verse;

namespace RIMAPI.Core
{
    /// <summary>
    /// Supported logging levels for the Rimworld REST API logging helper.
    /// Levels act as a minimum severity threshold when filtering log output.
    /// </summary>
    public enum LoggingLevels
    {
        DEBUG,
        INFO,
        WARNING,
        ERROR,
        CRITICAL,
    }

    /// <summary>
    /// A lightweight configurable logging utility for the RimworldRestApi project.
    /// This class wraps Verse.Log and provides severity-based filtering,
    /// standardized prefixes, and a single toggle to globally enable/disable logging.
    ///
    /// Usage:
    ///     DebugLogging.IsLogging = true;
    ///     DebugLogging.LoggingLevel = LoggingLevels.DEBUG;
    ///     DebugLogging.Info("Hello world");
    ///
    /// Note:
    /// IsLogging gates the chatty levels only (DEBUG, INFO, WARNING); it defaults
    /// to false because those lines would flood Player.log. ERROR and CRITICAL are
    /// always written: a swallowed exception leaves a failure nobody can diagnose
    /// (ticket #3205, a NullReferenceException in a service left no trace at all).
    /// LoggingLevel acts as the minimum required severity to log.
    ///
    /// Example:
    ///     LoggingLevel = WARNING means DEBUG and INFO messages are ignored.
    /// </summary>
    [StaticConstructorOnStartup]
    public class LogApi
    {
        /// <summary>
        /// Global flag for the chatty log levels (DEBUG, INFO, WARNING).
        /// <para>ERROR and CRITICAL are written regardless of this flag.</para>
        /// </summary>
        public static bool IsLogging = false;

        /// <summary>
        /// Minimum severity level required for a message to be logged.
        /// Messages below this level are ignored.
        /// Default = INFO.
        /// </summary>
        public static LoggingLevels LoggingLevel = LoggingLevels.INFO;

        /// <summary>
        /// Logs an INFO-level message.
        /// </summary>
        public static void Info(string text)
        {
            Message(text, LoggingLevels.INFO);
        }

        /// <summary>
        /// Logs an INFO-level message (object version).
        /// </summary>
        public static void Info(object text)
        {
            Message(text, LoggingLevels.INFO);
        }

        /// <summary>
        /// Logs an ERROR-level message.
        /// </summary>
        public static void Error(object text)
        {
            Message(text, LoggingLevels.ERROR);
        }

        /// <summary>
        /// Logs a WARNING-level message.
        /// </summary>
        public static void Warning(object text)
        {
            Message(text, LoggingLevels.WARNING);
        }

        /// <summary>
        /// Whether a message of this level reaches Verse.Log. An ERROR or CRITICAL
        /// always goes out, whatever the settings say: that is the line a later
        /// session reads to find the cause of a failure.
        /// </summary>
        private static bool ShouldWrite(LoggingLevels messageLevel)
        {
            if (messageLevel >= LoggingLevels.ERROR)
                return true;

            if (!IsLogging)
                return false;

            return messageLevel >= LoggingLevel;
        }

        /// <summary>
        /// Core logging method for string input.
        /// <para>Applies filtering through ShouldWrite (errors always pass),
        /// then formats and routes the message to the appropriate Verse log call.</para>
        ///
        /// Example formatting:
        /// <para>[RIMAPI | INFO] Message text</para>
        /// </summary>
        public static void Message(string text, LoggingLevels messageLevel = LoggingLevels.INFO)
        {
            if (!ShouldWrite(messageLevel))
                return;

            string message = "[RIMAPI | ";

            // Build standardized prefix and dispatch based on severity
            switch (messageLevel)
            {
                case LoggingLevels.DEBUG:
                    message += "DEBUG] " + text;
                    Log.Message(message);
                    break;

                case LoggingLevels.INFO:
                    message += "INFO] " + text;
                    Log.Message(message);
                    break;

                case LoggingLevels.WARNING:
                    message += "WARNING] " + text;
                    Log.Warning(message);
                    break;

                case LoggingLevels.ERROR:
                    message += "ERROR] " + text;
                    Log.Error(message);
                    break;

                case LoggingLevels.CRITICAL:
                    // Critical errors get the same Log.Error routing
                    message += "CRITICAL] " + text;
                    Log.Error(message);
                    break;
            }
        }

        /// <summary>
        /// Core logging method for object input.
        /// Converts object to string and applies the same severity filtering as the string overload.
        /// </summary>
        public static void Message(object text, LoggingLevels messageLevel = LoggingLevels.INFO)
        {
            if (!ShouldWrite(messageLevel))
                return;

            string message = "[RIMAPI] " + text.ToString();

            // Route to appropriate Verse.Log method
            switch (messageLevel)
            {
                case LoggingLevels.DEBUG:
                case LoggingLevels.INFO:
                    Log.Message(message);
                    break;

                case LoggingLevels.WARNING:
                    Log.Warning(message);
                    break;

                case LoggingLevels.ERROR:
                case LoggingLevels.CRITICAL:
                    Log.Error(message);
                    break;
            }
        }
    }
}
