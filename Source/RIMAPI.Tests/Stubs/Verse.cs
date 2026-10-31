// Minimal stand-ins for the RimWorld types the tested mod files touch.
//
// Why stubs and not a reference to RimWorld: Krafs.Rimworld.Ref ships
// reference-only assemblies, so any call into Verse at test time throws
// "Reference assemblies should not be loaded for execution". A test therefore
// links the mod's source file and compiles it against these types instead
// (ticket #3227).
//
// Keep this file small: it carries only what the linked sources need, and every
// member mirrors the signature of the real type so the same source compiles in
// both worlds.

using System;
using System.Collections.Generic;

namespace Verse
{
    /// <summary>
    /// Stands in for Verse.Log and keeps what was written, so a test can assert
    /// which call a mod file made and with which text.
    /// </summary>
    public static class Log
    {
        public static readonly List<LogEntry> Written = new List<LogEntry>();

        public static void Message(string text) => Written.Add(new LogEntry("Message", text));

        public static void Warning(string text) => Written.Add(new LogEntry("Warning", text));

        public static void Error(string text) => Written.Add(new LogEntry("Error", text));

        public static void Clear() => Written.Clear();
    }

    /// <summary>One recorded Verse.Log call: the method used and the text.</summary>
    public readonly struct LogEntry
    {
        public LogEntry(string method, string text)
        {
            Method = method;
            Text = text;
        }

        public string Method { get; }

        public string Text { get; }

        public override string ToString() => Method + ": " + Text;
    }

    /// <summary>
    /// RimWorld runs the static constructor of a class carrying this attribute
    /// at startup; for a test it is only a marker on the type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class StaticConstructorOnStartup : Attribute
    {
    }
}
