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

    /// <summary>
    /// The game's gas kinds. The values are the game's own (each gas sits 8
    /// apart, so the type doubles as a mask bit); only the names matter here.
    /// </summary>
    public enum GasType
    {
        BlindSmoke = 0,
        ToxGas = 8,
        RotStink = 16,
        DeadlifeDust = 24,
    }

    /// <summary>
    /// The game's work tags, the mask a pawn's backstory and traits disable
    /// (ticket #3360). The values are the game's own from Verse.WorkTags 1.6,
    /// so a test that composes a mask composes the one the game would.
    /// </summary>
    [Flags]
    public enum WorkTags
    {
        None = 0,
        ManualDumb = 2,
        ManualSkilled = 4,
        Violent = 8,
        Caring = 16,
        Social = 32,
        Commoner = 64,
        Intellectual = 128,
        Animals = 256,
        Artistic = 512,
        Crafting = 1024,
        Cooking = 2048,
        Firefighting = 4096,
        Cleaning = 8192,
        Hauling = 16384,
        PlantWork = 32768,
        Mining = 65536,
        Hunting = 131072,
        Constructing = 262144,
        Shooting = 524288,
        AllWork = 1048576,
    }

    /// <summary>
    /// The game's danger scale an order's max_danger uses; the walk scan puts
    /// each hazard on it, so a test can read the level a gas costs.
    /// </summary>
    public enum Danger
    {
        None = 0,
        Some = 1,
        Deadly = 2,
    }

    /// <summary>
    /// Stands in for Verse.Prefs, the game's options (fox ticket #4212). Only
    /// the one switch the dev boundary reads is here: DevMode is the same
    /// field the game's own dev tools check, and a test sets it to drive the
    /// boundary's two answers.
    /// </summary>
    public static class Prefs
    {
        public static bool DevMode;
    }
}
